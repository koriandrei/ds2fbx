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
        public DSAnimStudio.NewHavokAnimation_SplineCompressed dsAnimation;

        public HKX.HKASplineCompressedAnimation hkaAnimation;

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

        protected override FbxAnimStack GenerateFbx()
        {
            FbxAnimStack animStack = FbxAnimStack.Create(Scene, Souls.name + "_AnimStack");

            FbxAnimLayer animLayer = FbxAnimLayer.Create(animStack, "Layer0");

            animStack.AddMember(animLayer);

            DSAnimStudio.NewHavokAnimation_SplineCompressed anim = Souls.dsAnimation;

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

            for (int frameIndex = 0; frameIndex < Souls.hkaAnimation.FrameCount; ++frameIndex)
            {
                FbxTime time = FbxTime.FromFrame(frameIndex);

                int frameInBlockIndex = frameIndex % anim.NumFramesPerBlock;

                int blockIndex = (int)(Math.Floor(((double)frameInBlockIndex) / anim.NumFramesPerBlock));

                anim.CurrentTime = frameIndex * anim.FrameDuration;

                foreach (var bone in Souls.skeleton.boneDatas)
                {
                    int hkxBoneIndex = bone.exportData.SoulsData.HkxBoneIndex;

                    DSAnimStudio.NewBlendableTransform newBlendableTransform = anim.GetBlendableTransformOnCurrentFrame(hkxBoneIndex);

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

            return animStack;
        }
    }
}
