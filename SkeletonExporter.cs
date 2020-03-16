using System;
using System.Collections.Generic;
using System.Text;

using SoulsFormats;
using Autodesk.Fbx;

using System.Linq;
using System.Numerics;

namespace Ds3FbxSharp
{
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

            return baseDatas.SelectMany(baseData => new[] { baseData.exportData.SoulsData.Name }.Concat( GetChildren(boneDatas.Where(boneData => boneData.parent == baseData)).Select(child => "--" + child)));
        }

        public override string ToString()
        {
            return GetChildren(roots.ToList()).Aggregate((head, tail)=> head + Environment.NewLine + tail);
        }
    }

    class DsBoneData
    {
        public FbxExportData<DsBone, FbxSkeleton> exportData { get; }

        public DsBoneData parent;
        public readonly FLVER.Bone flverBone;

        public DsBoneData(DsBone bone, FLVER.Bone flverBone, FbxScene scene)
        {
            FbxSkeleton fbxBone = FbxSkeleton.Create(scene, bone.Name + "Bone");

            fbxBone.SetSkeletonType(flverBone.ChildIndex >= 0 ? FbxSkeleton.EType.eLimb : FbxSkeleton.EType.eEffector);

            fbxBone.Size.Set(flverBone.Translation.Length());

            exportData = fbxBone.CreateExportDataWithScene(bone, scene);

            this.flverBone = flverBone;
        }

        internal void SetParent(DsBoneData parentBoneData)
        {
            parent = parentBoneData;

            if (parentBoneData != null)
            {
                parentBoneData.exportData.FbxNode.AddChild(exportData.FbxNode);
            }
        }
    }

    class SkeletonExporter
    {
        private List<DsBone> bones;

        public SkeletonExporter(FbxScene scene, FLVER2 flver, List<DsBone> bones)
        {
            Scene = scene;
            this.Flver = flver;
            this.bones = bones;
        }

        private static Matrix4x4 CalculateGlobalTransform(FLVER.Bone bone, FLVER2 flver)
        {
            Matrix4x4 localTransform = bone.ComputeLocalTransform();

            //Matrix4x4 localTransform = Matrix4x4.CreateTranslation(bone.Translation)
            //    * Matrix4x4.CreateRotationY(bone.Rotation.Y)
            //    * Matrix4x4.CreateRotationY(bone.Rotation.Z)
            //    * Matrix4x4.CreateRotationY(bone.Rotation.X)
            //    * Matrix4x4.CreateScale(bone.Scale);

            if (bone.ParentIndex >= 0)
            {
                localTransform *= CalculateGlobalTransform(flver.Bones[bone.ParentIndex], flver);
            }

            return localTransform;
        }


        private static Vector3 QuaternionToEuler(Quaternion q)
        {
            Vector3 angles = new Vector3();


            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float) Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = (float) Math.CopySign(Math.PI / 2, sinp); // use 90 degrees if out of range
            else
                angles.Y = (float) Math.Asin(sinp);

            // yaw (z-axis rotation)
            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles * 180 / (float) Math.PI;
        }

        public DsSkeleton ParseSkeleton()
        {
            List<DsBoneData> boneDatas = bones.Select(
                bone =>
                {
                    FLVER.Bone flverBone = Flver.Bones.Single(flverBone => flverBone.Name == bone.Name);

                    return new DsBoneData(bone, flverBone, Scene);
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

            foreach (var boneData in boneDatas)
            {
                Matrix4x4 globalTransform = CalculateGlobalTransform(boneData.flverBone, Flver);

                if (boneData.parent != null)
                {
                    Matrix4x4 globalParentTransform = CalculateGlobalTransform(boneData.parent.flverBone, Flver);
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

                Vector3 scale;
                Quaternion rotation;
                Vector3 translation;

                if (Matrix4x4.Decompose(globalTransform, out scale, out rotation, out translation))
                {
                    boneData.exportData.FbxNode.LclTranslation.Set(translation.ToFbxDouble3());

                    Vector3 euler = QuaternionToEuler(rotation);

                    boneData.exportData.FbxNode.LclRotation.Set(euler.ToFbxDouble3());

                    boneData.exportData.FbxNode.LclScaling.Set(scale.ToFbxDouble3());
                }
                else
                {
                    throw new Exception();
                }
                
            }

            FbxSkeleton skeletonRoot = FbxSkeleton.Create(Scene, "ActualRoot");

            FbxNode skeletonRootNode = skeletonRoot.CreateNode();

            foreach (var root in boneDatas.Where(bone => bone.parent == null))
            {
                skeletonRootNode.AddChild(root.exportData.FbxNode);
            }

            return new DsSkeleton(skeletonRootNode, boneDatas);
        }

        public FbxScene Scene { get; }
        public FLVER2 Flver { get; }
    }
}
