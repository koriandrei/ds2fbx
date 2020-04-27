using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

using SoulsFormats;
using Autodesk.Fbx;
using System.Numerics;

namespace Ds3FbxSharp
{
    class AnimationExportData
    {
        public DSAnimStudio.NewHavokAnimation_SplineCompressedData dsAnimation;

        public HKX.HKADefaultAnimatedReferenceFrame animRefFrame;

        //public HKX.HKASplineCompressedAnimation hkaAnimation;

        public HKX.HKAAnimationBinding hkaAnimationBinding;

        public DsSkeleton skeleton;

        public string name;
    }

    class AnimationExporter : Exporter<AnimationExportData, FbxAnimStack>
    {
        private readonly RootMotionCache rootMotionCache;

        public AnimationExporter(FbxScene scene, AnimationExportData soulsType) : base(scene, soulsType)
        {
            rootMotionCache = new RootMotionCache(soulsType);
        }

        class AnimExportHelper
        {
            public class AnimExportHelperHelper
            {
                public AnimExportHelperHelper(FbxAnimLayer animLayer, FbxPropertyDouble3 node)
                {
                    curveX = node.GetCurve(animLayer, Globals.FBXSDK_CURVENODE_COMPONENT_X, /*pCreate =*/ true);
                    curveY = node.GetCurve(animLayer, Globals.FBXSDK_CURVENODE_COMPONENT_Y, /*pCreate =*/ true);
                    curveZ = node.GetCurve(animLayer, Globals.FBXSDK_CURVENODE_COMPONENT_Z, /*pCreate =*/ true);

                    curveX.KeyModifyBegin();
                    curveY.KeyModifyBegin();
                    curveZ.KeyModifyBegin();
                }

                public void AddPoint(FbxTime time, Vector3 value)
                {
                    int xCurveAddedKey = curveX.KeyAdd(time);
                    curveX.KeySet(xCurveAddedKey, time, value.X);

                    int yCurveAddedKey = curveY.KeyAdd(time);
                    curveY.KeySet(yCurveAddedKey, time, value.Y);

                    int zCurveAddedKey = curveZ.KeyAdd(time);
                    curveZ.KeySet(zCurveAddedKey, time, value.Z);

                    //FbxAnimCurveKey xKey = new FbxAnimCurveKey(time, value.X);
                    //curveX.KeyAdd(time, value.X);
                    //curveY.KeyAdd(time, value.Y);
                    //curveZ.KeyAdd(time, value.Z);
                    //FbxAnimCurveKey xKey = new FbxAnimCurveKey(time, value.Y);
                    //FbxAnimCurveKey xKey = new FbxAnimCurveKey(time, value.Z);
                }

                public void Finish()
                {
                    curveX.KeyModifyEnd();
                    curveY.KeyModifyEnd();
                    curveZ.KeyModifyEnd();
                }

                private FbxAnimCurve curveX;
                private FbxAnimCurve curveY;
                private FbxAnimCurve curveZ;

            }

            public AnimExportHelper(FbxAnimLayer animLayer, FbxPropertyDouble3 translation, FbxPropertyDouble3 rotation, FbxPropertyDouble3 scale)
            {
                this.translation = new AnimExportHelperHelper(animLayer, translation);
                this.rotation = new AnimExportHelperHelper(animLayer, rotation);
                this.scale = new AnimExportHelperHelper(animLayer, scale);
            }

            public AnimExportHelperHelper translation;
            public AnimExportHelperHelper rotation;
            public AnimExportHelperHelper scale;
        }

        private Microsoft.Xna.Framework.Matrix GetMatrix(int hkxBoneIndex, int blockIndex, float frameIndex)
        {
            DSAnimStudio.NewHavokAnimation_SplineCompressedData anim = Souls.dsAnimation;
            int transformTrack = anim.HkxBoneIndexToTransformTrackMap[hkxBoneIndex];

            DSAnimStudio.NewBlendableTransform newBlendableTransform = anim.GetTransformOnSpecificBlockAndFrame(transformTrack, blockIndex, frameIndex);

            var currentTransformMatrix = /*newBlendableTransform.GetMatrixScale() **/ newBlendableTransform.GetMatrix();

            int parentIndex = Souls.dsAnimation.skeleton.ParentIndices[hkxBoneIndex].data;

            if (parentIndex >= 0)
            {
                currentTransformMatrix *= GetMatrix(parentIndex, blockIndex, frameIndex);
            }
            else
            {
                var rootMotion = rootMotionCache.GetRootMotionOnFrame((int)frameIndex);

                currentTransformMatrix *= Microsoft.Xna.Framework.Matrix.CreateRotationY(rootMotion.W);
                currentTransformMatrix *= Microsoft.Xna.Framework.Matrix.CreateTranslation(rootMotion.X, rootMotion.Y, rootMotion.Z);
            }

            return currentTransformMatrix;
        }

        private DSAnimStudio.NewBlendableTransform GetTransform(int hkxBoneIndex, int blockIndex, float frameIndex)
        {
            return new DSAnimStudio.NewBlendableTransform(GetMatrix(hkxBoneIndex, blockIndex, frameIndex));
        }

        private DSAnimStudio.NewBlendableTransform GetTransform(DsBoneData bone, int blockIndex, float frameIndex)
        {
            return GetTransform(bone.exportData.SoulsData.HkxBoneIndex, blockIndex, frameIndex);
        }

        private static void HackFixupMatrix(ref Microsoft.Xna.Framework.Matrix matrix)
        {
            Microsoft.Xna.Framework.Matrix hackFixupMatrix =
                Microsoft.Xna.Framework.Matrix.CreateScale(1, 1, 1)
                // *
                //Microsoft.Xna.Framework.Matrix.CreateRotationY((float)(Math.PI))
                ;
            matrix *= hackFixupMatrix;

            //matrix = hackFixupMatrix * matrix;
        }

        private static void HackFixupTransform(ref DSAnimStudio.NewBlendableTransform transform)
        {
            var currentTransformMatrix = transform.GetMatrixScale() * transform.GetMatrix();

            HackFixupMatrix(ref currentTransformMatrix);

            transform = new DSAnimStudio.NewBlendableTransform(currentTransformMatrix);

            //var fixedUp = transform.GetMatrix() *;

            //transform = new DSAnimStudio.NewBlendableTransform(transform.GetMatrixScale() * fixedUp);

            //var hackFixupTransform = new DSAnimStudio.NewBlendableTransform(hackFixupMatrix);

            //transform *= hackFixupTransform;
            //transform.Translation.Z = -transform.Translation.Z;
        }

        struct RootMotionCache
        {
            public RootMotionCache(AnimationExportData souls)
            {
                Souls = souls;

                var rootMotionFrames = Souls.animRefFrame.ReferenceFrameSamples.GetArrayData().Elements;

                System.Diagnostics.Debug.Assert(rootMotionFrames.Count == souls.dsAnimation.FrameCount);
            }

            public Vector4 GetRootMotion(float frame)
            {
                var rootMotionFrames = Souls.animRefFrame.ReferenceFrameSamples.GetArrayData().Elements;

                var previousFrame = GetRootMotionOnFrame((int)Math.Floor(frame));

                var nextFrame = GetRootMotionOnFrame((int)Math.Ceiling(frame));

                float alpha = (float)(frame - Math.Floor(frame));

                return Vector4.Lerp(previousFrame, nextFrame, alpha);
            }

            public Vector4 GetRootMotionOnFrame(int frame)
            {
                if (Souls.animRefFrame == null)
                {
                    return Vector4.Zero;
                }

                var rootMotionFrames = Souls.animRefFrame.ReferenceFrameSamples.GetArrayData().Elements;

                return rootMotionFrames[frame].Vector;
            }

            public AnimationExportData Souls { get; }
        }

        protected override FbxAnimStack GenerateFbx()
        {

            DSAnimStudio.NewHavokAnimation_SplineCompressedData anim = Souls.dsAnimation;
            var animStack = FbxAnimStack.Create(Scene, Souls.name + "_AnimStack");

            animStack.SetLocalTimeSpan(new FbxTimeSpan(FbxTime.FromFrame(0), FbxTime.FromFrame(anim.FrameCount)));

            FbxAnimLayer animLayer = FbxAnimLayer.Create(animStack, "Layer0");

            animStack.AddMember(animLayer);


            IDictionary<int, short> hkaTrackToHkaBoneIndex = new Dictionary<int, short>();

            for (int boneMapIndex = 0; boneMapIndex < Souls.hkaAnimationBinding.TransformTrackToBoneIndices.Size; ++boneMapIndex)
            {
                hkaTrackToHkaBoneIndex.Add(boneMapIndex, Souls.hkaAnimationBinding.TransformTrackToBoneIndices.GetArrayData().Elements[boneMapIndex].data);
            }

            IDictionary<int, AnimExportHelper> boneHelpers = new Dictionary<int, AnimExportHelper>();

            foreach (DsBoneData boneData in Souls.skeleton.boneDatas)
            {
                boneData.exportData.FbxNode.LclTranslation.GetCurveNode(animLayer, true);
                boneData.exportData.FbxNode.LclRotation.GetCurveNode(animLayer, true);
                boneData.exportData.FbxNode.LclScaling.GetCurveNode(animLayer, true);

                boneHelpers.Add(boneData.exportData.SoulsData.HkxBoneIndex, new AnimExportHelper(
                    animLayer,
                    boneData.exportData.FbxNode.LclTranslation,
                    boneData.exportData.FbxNode.LclRotation,
                    boneData.exportData.FbxNode.LclScaling
                    )
                );
            }

            for (int frameIndex = 0; frameIndex < Souls.dsAnimation.FrameCount; ++frameIndex)
            {
                FbxTime time = FbxTime.FromFrame(frameIndex);

                int frameInBlockIndex = frameIndex % anim.NumFramesPerBlock;

                int blockIndex = (int)(Math.Floor(((double)frameInBlockIndex) / anim.NumFramesPerBlock));

                foreach (var bone in Souls.skeleton.boneDatas)
                {
                    var hackPreMatrix = Microsoft.Xna.Framework.Matrix.CreateScale(-1, 1, 1);
                    var hackPostMatrix = Microsoft.Xna.Framework.Matrix.CreateScale(1, 1, -1) ;

                    var calculatedMatrix = GetMatrix(bone.exportData.SoulsData.HkxBoneIndex, blockIndex, frameIndex);

                    var hackNewBlendableMatrix = hackPreMatrix * calculatedMatrix * hackPostMatrix;



                    if (bone.parent != null)
                    {
                        var calculatedParentMatrix = GetMatrix(bone.parent.exportData.SoulsData.HkxBoneIndex, blockIndex, frameIndex);

                        var hackParentBlendableMatrix = hackPreMatrix * calculatedParentMatrix * hackPostMatrix;

                        hackNewBlendableMatrix *= Microsoft.Xna.Framework.Matrix.Invert(hackParentBlendableMatrix);
                    }
                    else
                    {
                        HackFixupMatrix(ref hackNewBlendableMatrix);
                    }

                    var newBlendableTransform = new DSAnimStudio.NewBlendableTransform(hackNewBlendableMatrix);



                    int hkxBoneIndex = bone.exportData.SoulsData.HkxBoneIndex;

                    AnimExportHelper animExportHelper = boneHelpers[hkxBoneIndex];

                    animExportHelper.translation.AddPoint(time, new Vector3(newBlendableTransform.Translation.X, newBlendableTransform.Translation.Y, newBlendableTransform.Translation.Z));
                    animExportHelper.rotation.AddPoint(time, new Quaternion(newBlendableTransform.Rotation.X, newBlendableTransform.Rotation.Y, newBlendableTransform.Rotation.Z, newBlendableTransform.Rotation.W).QuaternionToEuler());
                    animExportHelper.scale.AddPoint(time, new Vector3(newBlendableTransform.Scale.X, newBlendableTransform.Scale.Y, newBlendableTransform.Scale.Z));
                }
            }

            foreach (AnimExportHelper helper in boneHelpers.Values)
            {
                helper.translation.Finish();
                helper.rotation.Finish();
                helper.scale.Finish();
            }

            return null;
        }
    }
}
