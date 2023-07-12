using System;
using System.Collections.Generic;
using System.Text;

using SoulsFormats;
using SFAnimExtensions;
//using Autodesk.Fbx;

using System.Linq;
using System.Numerics;
using SharpGLTF.Scenes;

using SFAnimExtensions.Havok;

namespace Ds3FbxSharp
{
    using FbxNode = SharpGLTF.Scenes.NodeBuilder;
    using NodeBuilder = SharpGLTF.Scenes.NodeBuilder;

    public abstract class ExporterSkeletonRoot
    {
        public abstract void AddNode(NodeBuilder fbxData);
    }

    internal class GltfExporterSkeletonRoot : ExporterSkeletonRoot
    {
        public GltfExporterSkeletonRoot(SceneBuilder sceneBuilder, string rootBoneName)
        {
            rootNode = new NodeBuilder(rootBoneName);
            sceneBuilder.AddNode(rootNode);
        }

        public override void AddNode(NodeBuilder childNode)
        {
            rootNode.AddNode(childNode);
        }

        FbxNode rootNode;
    }

    class DsSkeleton
    {
        public readonly List<DsBoneData> boneDatas;

        public IEnumerable<DsBoneData> roots => boneDatas.Where(boneData => boneData.parent == null);

        public FbxNode SkeletonRootNode { get; }

        public DsSkeleton(FbxNode skeletonRootNode, List<DsBoneData> boneDatas)
        {
            SkeletonRootNode = skeletonRootNode;
            this.boneDatas = boneDatas;
        }

        private IEnumerable<string> GetChildren(IEnumerable<DsBoneData> baseDatas)
        {
            Console.WriteLine("Recursing");

            return baseDatas.SelectMany(baseData => new[] { baseData.exportData.SoulsData.Name }.Concat(GetChildren(boneDatas.Where(boneData => boneData.parent == baseData)).Select(child => "--" + child)));
        }

        public override string ToString()
        {
            return GetChildren(roots.ToList()).Aggregate((head, tail) => head + Environment.NewLine + tail);
        }
    }

    class DsBoneData
    {
        public FbxExportData<DsBone, SharpGLTF.Scenes.NodeBuilder> exportData { get; }

        public DsBoneData parent;
        public readonly FLVER.Bone flverBone;

        public DsBoneData(DsBone bone, FLVER.Bone flverBone, MyExporter exporter)
        {
            var gltfBone = new SharpGLTF.Scenes.NodeBuilder(bone.Name + "_Bone");

            //fbxBone.SetSkeletonType(flverBone.ChildIndex >= 0 ? FbxSkeleton.EType.eLimbNode : FbxSkeleton.EType.eEffector);

            //fbxBone.Size.Set(flverBone.Translation.Length());

            exportData = gltfBone.CreateExportDataWithScene(bone, exporter);

            this.flverBone = flverBone;
        }

        internal void SetParent(DsBoneData parentBoneData)
        {
            parent = parentBoneData;

            if (parentBoneData != null)
            {
                parentBoneData.exportData.FbxData.AddNode(exportData.FbxData);
                //parentBoneData.exportData.FbxNode.AddChild(exportData.FbxNode);
            }
        }
    }

    class SkeletonExporter
    {
        private List<DsBone> bones;

        private readonly HKX.HKASkeleton hkaSkeleton;

        public SkeletonExporter(MyExporter exporter, FLVER2 flver, HKX.HKASkeleton hKASkeleton, List<DsBone> bones)
        {
            Exporter = exporter;
            this.Flver = flver;
            this.bones = bones;
            this.hkaSkeleton = hKASkeleton;
        }

        private static Matrix4x4 CalculateGlobalTransformFlver(FLVER.Bone bone, FLVER2 flver)
        {
            Matrix4x4 localTransform = bone.ComputeLocalTransform();

            //Matrix4x4 localTransform = Matrix4x4.CreateTranslation(bone.Translation)
            //    * Matrix4x4.CreateRotationY(bone.Rotation.Y)
            //    * Matrix4x4.CreateRotationY(bone.Rotation.Z)
            //    * Matrix4x4.CreateRotationY(bone.Rotation.X)
            //    * Matrix4x4.CreateScale(bone.Scale);

            if (bone.ParentIndex >= 0)
            {
                localTransform *= CalculateGlobalTransformFlver(flver.Bones[bone.ParentIndex], flver);
            }

            return localTransform;
        }

        private static Matrix4x4 CalculateGlobalTransformHka(int hkxBoneIndex, HKX.HKASkeleton skeleton)
        {
            var hkxTransform = skeleton.Transforms.GetArrayData().Elements[hkxBoneIndex];

            Matrix4x4 transformMatrix =
                Matrix4x4.CreateScale(hkxTransform.Scale.Vector.X, hkxTransform.Scale.Vector.Y, hkxTransform.Scale.Vector.Z)
                * Matrix4x4.CreateFromQuaternion(new Quaternion(hkxTransform.Rotation.Vector.X, hkxTransform.Rotation.Vector.Y, hkxTransform.Rotation.Vector.Z, hkxTransform.Rotation.Vector.W))
                * Matrix4x4.CreateTranslation(hkxTransform.Position.Vector.X, hkxTransform.Position.Vector.Y, hkxTransform.Position.Vector.Z);

            short parentIndex = skeleton.ParentIndices[hkxBoneIndex].data;

            if (parentIndex >= 0)
            {
                transformMatrix *= CalculateGlobalTransformHka(parentIndex, skeleton);
            }
            else
            {
                //transformMatrix *= Matrix4x4.CreateRotationX((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2));
            }

            return transformMatrix;
        }

        private static Matrix4x4 CalculateGlobalTransform(DsBoneData boneData, FLVER2 flver, HKX.HKASkeleton hkaSkeleton)
        {
            if (boneData.exportData.SoulsData.HkxBoneIndex >= 0)
            {
                System.Diagnostics.Debug.Assert(hkaSkeleton != null);

                System.Console.WriteLine("Using HKA transform");

                return CalculateGlobalTransformHka(boneData.exportData.SoulsData.HkxBoneIndex, hkaSkeleton);
            }

            if (flver != null)
            {
                System.Console.WriteLine("Using FLVER transform");

                return CalculateGlobalTransformFlver(boneData.flverBone, flver);
            }

            throw new System.ArgumentException("Can't calculate transform " + nameof(boneData));
        }


        public DsSkeleton ParseSkeleton()
        {
            List<DsBoneData> boneDatas = bones.Select(
                bone =>
                {
                    FLVER.Bone flverBone = Flver.Bones.Single(flverBone => flverBone.Name == bone.Name);

                    return new DsBoneData(bone, flverBone, Exporter);
                }
                ).ToList();



            //exportData.FbxNode.LclTranslation.Set(flverBone.Translation.ToFbxDouble3());

            //exportData.FbxNode.LclRotation.Set(flverBone.Rotation.ToFbxDouble3());

            //exportData.FbxNode.LclScaling.Set(flverBone.Scale.ToFbxDouble3());

            for (int boneIndex = 0; boneIndex < bones.Count; ++boneIndex)
            {
                DsBoneData boneData = boneDatas[boneIndex];

                DsBoneData parentBoneData = boneDatas.Find(parentBoneData => parentBoneData.exportData.SoulsData.Name == boneData.exportData.SoulsData.ParentName);

                boneData.SetParent(parentBoneData);
            }

            Func<DsBoneData, Matrix4x4> calculateTransform = (boneData) =>
            {
                Matrix4x4 rawGlobalTransform = CalculateGlobalTransform(boneData, Flver, hkaSkeleton);

                var preFixupMatrix = Matrix4x4.CreateRotationZ((float)(-Math.PI / 2)); // Matrix4x4.CreateScale(new Vector3(1, 1, 1)); // * Matrix4x4.CreateRotationX((float)(-Math.PI)) * Matrix4x4.CreateRotationY((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2));

                var postFixupMatrix = Matrix4x4.CreateScale(1,1,1); // Matrix4x4.CreateRotationZ((float)(Math.PI / 2)); // Matrix4x4.CreateScale(new Vector3(1, 1, 1));// * Matrix4x4.CreateRotationX((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2));

                if (boneData.parent == null)
                {
                    Matrix4x4 preFixupParent = Matrix4x4.Identity;
                    Matrix4x4 postFixupParent = Matrix4x4.Identity; // * Matrix4x4.CreateRotationX((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2)); ; //* Matrix4x4.CreateScale(1,1,-1);

                    preFixupMatrix *= preFixupParent;
                    postFixupMatrix *= postFixupParent;
                }
                else
                {
                }


                //var t = rawGlobalTransform.Translation;
                //t.Z = -t.Z;
                //rawGlobalTransform.Translation = t;

                var fixedTransform = preFixupMatrix * rawGlobalTransform * postFixupMatrix;

                var btr = new NewBlendableTransform(fixedTransform);

                btr.Translation.Z = -btr.Translation.Z;
                btr.Rotation.X = -btr.Rotation.X;
                btr.Rotation.Y = -btr.Rotation.Y;

                return btr.GetMatrix();
            };

            //Func<DsBoneData, Matrix4x4> calculateParentTransform = (boneData) =>
            //{
            //    Matrix4x4 rawGlobalTransform = CalculateGlobalTransform(boneData, Flver, hkaSkeleton);

            //    var preFixupMatrix = Matrix4x4.CreateScale(new Vector3(1, 1, 1)); // * Matrix4x4.CreateRotationX((float)(-Math.PI)) * Matrix4x4.CreateRotationY((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2));

            //    var postFixupMatrix = Matrix4x4.CreateScale(new Vector3(1, 1, 1)); // * Matrix4x4.CreateRotationX((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2));

            //    if (boneData.parent == null)
            //    {
            //        postFixupMatrix *= Matrix4x4.CreateRotationX((float)(-Math.PI / 2)) * Matrix4x4.CreateRotationZ((float)(Math.PI / 2));
            //    }
            //    else
            //    {
            //        //postFixupMatrix *= Matrix4x4.CreateScale(1, 1, -1);
            //    }

            //    return preFixupMatrix * rawGlobalTransform * postFixupMatrix;
            //};

            foreach (var boneData in boneDatas)
            {
                var globalTransform = calculateTransform(boneData);

                if (boneData.parent != null)
                {
                    var globalParentTransform = calculateTransform(boneData.parent);

                    Matrix4x4 invertedGlobalParentTransform;
                    if (Matrix4x4.Invert(globalParentTransform, out invertedGlobalParentTransform))
                    {
                        globalTransform *= invertedGlobalParentTransform;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                }

                Vector3 scale;
                Quaternion rotation;
                Vector3 translation;

                if (Matrix4x4.Decompose(globalTransform, out scale, out rotation, out translation))
                {
                    boneData.exportData.FbxData.SetLocalTransform(new SharpGLTF.Transforms.AffineTransform(scale, rotation, translation), false);
                    //boneData.exportData.FbxNode.LclTranslation.Set(translation.ToFbxDouble3());

                    //Vector3 euler = rotation.QuaternionToEuler();

                    //boneData.exportData.FbxNode.LclRotation.Set(euler.ToFbxDouble3());

                    //boneData.exportData.FbxNode.LclScaling.Set(scale.ToFbxDouble3());
                }
                else
                {
                    throw new Exception();
                }

            }

            //FbxSkeleton skeletonRoot = FbxSkeleton.Create(Scene, "ActualRoot");

            //FbxNode skeletonRootNode = skeletonRoot.CreateNode();
            //var skeletonRootNode = Exporter.CreateSkeletonRoot("ActualRoot");

            foreach (var root in boneDatas.Where(bone => bone.parent == null))
            {
                var extraRootNode = Exporter.CreateSkeletonRoot(root.exportData.SoulsData.Name);
                extraRootNode.AddNode(root.exportData.FbxData);
                //skeletonRootNode.AddNode(extraRootNode);
                // Scene.GetRootNode().AddChild(root.exportData.FbxNode);
            }



            return new DsSkeleton(null, boneDatas);
        }

        public MyExporter Exporter { get; }
        public FLVER2 Flver { get; }
    }
}
