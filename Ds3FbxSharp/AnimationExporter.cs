using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

using SoulsFormats;
using System.Numerics;

using SFAnimExtensions.Havok;
using SFAnimExtensions;

namespace Ds3FbxSharp
{
#if false
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

        class FrameData
        {
            public class FrameBoneData
            {
                public int hkxBoneIndex;
                public NewBlendableTransform transform;
            }

            public int frameIndex;

            public IEnumerable<FrameBoneData> boneDatas;
        }

        public class AnimationExportException : Exception
        {
            public AnimationExportException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        protected override FbxAnimStack GenerateFbx()
        {
            HavokAnimationData anim = Souls.dsAnimation;

            IDictionary<int, FrameData> frameDatas = ExtractAnimationData(anim);

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

            foreach (var frameData in frameDatas)
            {
                FbxTime time = FbxTime.FromFrame(frameData.Key);

                foreach (var boneData in frameData.Value.boneDatas)
                {
                    int hkxBoneIndex = boneData.hkxBoneIndex;

                    var newBlendableTransform = boneData.transform;

                    AnimExportHelper animExportHelper = boneHelpers[hkxBoneIndex];

                    var euler = new Quaternion(newBlendableTransform.Rotation.X, newBlendableTransform.Rotation.Y, newBlendableTransform.Rotation.Z, newBlendableTransform.Rotation.W).QuaternionToEuler();

                    animExportHelper.translation.AddPoint(time, newBlendableTransform.Translation);
                    animExportHelper.rotation.AddPoint(time, euler);
                    animExportHelper.scale.AddPoint(time, newBlendableTransform.Scale);
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

        private IDictionary<int, FrameData> ExtractAnimationData(HavokAnimationData anim)
        {
            IDictionary<int, FrameData> frameDatas = new Dictionary<int, FrameData>();

            try
            {


                for (int frameIndex = 0; frameIndex < Souls.dsAnimation.FrameCount; ++frameIndex)
                {
                    var frameBoneDatas = new List<FrameData.FrameBoneData>();

                    FrameData data = new FrameData() { boneDatas = frameBoneDatas, frameIndex = frameIndex };



                    Func<DsBoneData, Matrix4x4> calculateTransform = bone =>
                    {
                        if (bone.parent == null)
                        {
                            return Matrix4x4.Identity;
                        }

                        var calculatedMatrix = GetMatrix(bone.exportData.SoulsData.HkxBoneIndex, frameIndex);

                        var hackPreMatrix = Matrix4x4.CreateRotationZ((float)(-Math.PI / 2)); // * Matrix4x4.CreateRotationY((float)(-Math.PI / 2)); ; // Microsoft.Xna.Framework.Matrix.CreateScale(-1, 1, 1);
                    var hackPostMatrix = Matrix4x4.Identity; // Matrix4x4.CreateScale(-1, 1, 1); // Matrix4x4.CreateScale(-1,1,1); // * Matrix4x4.CreateRotationY((float)(Math.PI)); // Matrix4x4.CreateScale(1, 1, -1);

                    var transformedMatrix = hackPreMatrix * calculatedMatrix * hackPostMatrix;

                        var btr = new NewBlendableTransform(transformedMatrix);

                        btr.Translation.Z = -btr.Translation.Z;
                        btr.Rotation.X = -btr.Rotation.X;
                        btr.Rotation.Y = -btr.Rotation.Y;

                        return btr.GetMatrix();
                    };

                    foreach (var bone in Souls.skeleton.boneDatas)
                    {
                        var boneData111 = new FrameData.FrameBoneData() { hkxBoneIndex = bone.exportData.SoulsData.HkxBoneIndex };

                        var calculatedMatrix = calculateTransform(bone);

                        var hackNewBlendableMatrix = calculatedMatrix;

                        if (bone.parent != null)
                        {
                            Matrix4x4 parentMatrix = calculateTransform(bone.parent);

                            if (Matrix4x4.Invert(parentMatrix, out var inverted))
                            {
                                hackNewBlendableMatrix *= inverted;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            Func<Matrix4x4> getRefMatrix = () =>
                            {
                                if (Souls.animRefFrame == null)
                                {
                                    return Matrix4x4.Identity;
                                }

                                return Matrix4x4.CreateWorld(Vector3.Zero, Souls.animRefFrame.Forward.ToVector3(), Souls.animRefFrame.Up.ToVector3());
                            };

                            var refMatrix = Matrix4x4.Identity; // getRefMatrix();

                            Matrix4x4 additionalPostTransformation = refMatrix;

                            // this is a root bone
                            if (anim.RootMotion != null)
                            {
                                var rootMotion = anim.RootMotion.ExtractRootMotion(0, anim.Duration * (((float)frameIndex / anim.FrameCount)));

                                rootMotion.positionChange.Z = -rootMotion.positionChange.Z;

                                Matrix4x4 rootMotionTransformation = Matrix4x4.CreateRotationY(rootMotion.directionChange) * Matrix4x4.CreateTranslation(rootMotion.positionChange);
                                additionalPostTransformation *= rootMotionTransformation;
                            }

                            hackNewBlendableMatrix *= additionalPostTransformation;
                        }

                        var newBlendableTransform = new NewBlendableTransform(hackNewBlendableMatrix);

                        int hkxBoneIndex = bone.exportData.SoulsData.HkxBoneIndex;

                        boneData111.hkxBoneIndex = hkxBoneIndex;
                        boneData111.transform = newBlendableTransform;

                        frameBoneDatas.Add(boneData111);
                    }

                    frameDatas.Add(frameIndex, data);
                }

                return frameDatas;
            }
            catch (Exception ex)
            {
                throw new AnimationExportException("An exception occured while exporting animations!", ex);
            }
        }
    }
#endif
}
