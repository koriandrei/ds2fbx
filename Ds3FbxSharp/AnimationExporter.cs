using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

using SoulsFormats;
using Autodesk.Fbx;
using System.Numerics;

using SFAnimExtensions.Havok;
using SFAnimExtensions;

namespace Ds3FbxSharp
{
    class AnimationExportData
    {
        public HavokAnimationData dsAnimation;

        public HKX.HKADefaultAnimatedReferenceFrame animRefFrame;

        //public HKX.HKASplineCompressedAnimation hkaAnimation;

        public HKX.HKAAnimationBinding hkaAnimationBinding;

        public DsSkeleton skeleton;

        public string name;
    }

    class AnimationExporter : Exporter<AnimationExportData, FbxAnimStack>
    {
        public AnimationExporter(FbxScene scene, AnimationExportData soulsType) : base(scene, soulsType)
        {
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

        private Matrix4x4 GetMatrix(int hkxBoneIndex, float frameIndex)
        {
            HavokAnimationData anim = Souls.dsAnimation;

            NewBlendableTransform newBlendableTransform = anim.GetBlendableTransformOnFrame(hkxBoneIndex, frameIndex);

            var currentTransformMatrix = newBlendableTransform.GetMatrix();

            int parentIndex = Souls.dsAnimation.hkaSkeleton.ParentIndices[hkxBoneIndex].data;

            if (parentIndex >= 0)
            {
                currentTransformMatrix *= GetMatrix(parentIndex, frameIndex);
            }
            else
            {
                //var rootMotion = rootMotionCache.GetRootMotionOnFrame((int)frameIndex);

                //currentTransformMatrix *= Microsoft.Xna.Framework.Matrix.CreateRotationY(rootMotion.W);
                //currentTransformMatrix *= Microsoft.Xna.Framework.Matrix.CreateTranslation(rootMotion.X, rootMotion.Y, rootMotion.Z);
            }

            return currentTransformMatrix;
        }

        protected override FbxAnimStack GenerateFbx()
        {
            HavokAnimationData anim = Souls.dsAnimation;
            var animStack = FbxAnimStack.Create(Scene, anim.Name + "_AnimStack");

            animStack.SetLocalTimeSpan(new FbxTimeSpan(FbxTime.FromFrame(0), FbxTime.FromFrame(anim.FrameCount)));

            FbxAnimLayer animLayer = FbxAnimLayer.Create(animStack, "Layer0");

            animStack.AddMember(animLayer);

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

                Func<DsBoneData, Matrix4x4> calculateTransform = bone =>
                {
                    var calculatedMatrix = GetMatrix(bone.exportData.SoulsData.HkxBoneIndex, frameIndex);

                    var hackPreMatrix = Matrix4x4.Identity; // * Matrix4x4.CreateRotationY((float)(-Math.PI / 2)); ; // Microsoft.Xna.Framework.Matrix.CreateScale(-1, 1, 1);
                    var hackPostMatrix = Matrix4x4.Identity; // * Matrix4x4.CreateRotationY((float)(Math.PI)); // Matrix4x4.CreateScale(1, 1, -1);

                    if (bone.parent == null)
                    {
                        //var unrotateRoot = Microsoft.Xna.Framework.Matrix.CreateRotationZ((float)(Math.PI / 2)) * Microsoft.Xna.Framework.Matrix.CreateRotationY(-(float)(Math.PI / 2));
                        if (Matrix4x4.Invert(calculatedMatrix, out var inverted))
                        {
                            //hackPostMatrix =inverted *Matrix4x4.CreateRotationX((float)(-Math.PI / 2)) * hackPostMatrix;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        var translation = calculatedMatrix.Translation;

                        translation.Z = -translation.Z;

                        calculatedMatrix.Translation = translation;
                    }

                    return hackPreMatrix * calculatedMatrix * hackPostMatrix;
                };

                foreach (var bone in Souls.skeleton.boneDatas)
                {
                    var calculatedMatrix = calculateTransform(bone);

                    var hackNewBlendableMatrix = calculatedMatrix;



                    if (bone.parent != null)
                    {
                        var calculatedParentMatrix = calculateTransform(bone.parent);

                        var hackParentBlendableMatrix = calculatedParentMatrix;

                        if (Matrix4x4.Invert(hackParentBlendableMatrix, out var inverted))
                        {
                            hackNewBlendableMatrix *= inverted;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    //else
                    //{
                    //    if (anim.RootMotion != null)
                    //    {
                    //        var rootMotion = anim.RootMotion.ExtractRootMotion(0, frameIndex / anim.Duration);

                    //        var rootMotionMatrix = Matrix4x4.CreateRotationY(rootMotion.directionChange);
                    //        rootMotionMatrix *= Matrix4x4.CreateTranslation(rootMotion.positionChange);

                    //        hackNewBlendableMatrix *= rootMotionMatrix;
                    //    }
                    //}

                    var newBlendableTransform = new NewBlendableTransform(hackNewBlendableMatrix);

                    int hkxBoneIndex = bone.exportData.SoulsData.HkxBoneIndex;

                    AnimExportHelper animExportHelper = boneHelpers[hkxBoneIndex];

                    var euler = new Quaternion(newBlendableTransform.Rotation.X, newBlendableTransform.Rotation.Y, newBlendableTransform.Rotation.Z, newBlendableTransform.Rotation.W).QuaternionToEuler();

                    animExportHelper.translation.AddPoint(time, new Vector3(newBlendableTransform.Translation.X, newBlendableTransform.Translation.Y, newBlendableTransform.Translation.Z));
                    animExportHelper.rotation.AddPoint(time, euler);
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
