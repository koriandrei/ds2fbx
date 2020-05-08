using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

using SoulsFormats;
using Autodesk.Fbx;
namespace Ds3FbxSharp
{
    class SkinExportData
    {
        public SkinExportData(FbxExportData<MeshExportData, FbxMesh> meshData, DsSkeleton skeleton, FLVER2 flver)
        {
            this.meshData = meshData;
            this.skeleton = skeleton;
            this.flver = flver;
        }

        public FbxExportData<MeshExportData, FbxMesh> meshData { get; }

        public readonly FLVER2 flver;

        public readonly DsSkeleton skeleton;
    }

    class SkinExporter : Exporter<SkinExportData, FbxSkin>
    {
        public SkinExporter(FbxMesh owner, SkinExportData soulsType) : base(owner, soulsType)
        {
        }

        struct BoneIndexToWeightPair
        {
            public int vertexIndex;
            public int flverBoneIndex;
            public float boneWeight;
        }

        protected override FbxSkin GenerateFbx()
        {
            MeshExportData meshData = Souls.meshData.SoulsData;

            ICollection<BoneIndexToWeightPair> rawBoneDeformerData = new List<BoneIndexToWeightPair>();

            for (int vertexIndex = 0; vertexIndex < meshData.mesh.Vertices.Count; ++vertexIndex)
            {
                FLVER.Vertex vertex = meshData.mesh.Vertices[vertexIndex];

                const int maxVertexDeformations = 4;
                for (int vertexDeformationIndex = 0; vertexDeformationIndex < maxVertexDeformations; ++vertexDeformationIndex)
                {
                    BoneIndexToWeightPair weightData = new BoneIndexToWeightPair() {
                        flverBoneIndex = vertex.BoneIndices[vertexDeformationIndex], 
                        boneWeight = vertex.BoneWeights[vertexDeformationIndex], 
                        vertexIndex = vertexIndex 
                    };

                    if (weightData.flverBoneIndex > 0 && weightData.boneWeight > 0)
                    {
                        rawBoneDeformerData.Add(weightData);
                    }
                }
            }
            
            FbxSkin skin = FbxSkin.Create(Owner, meshData.meshRoot.Name + "_Skin");

            System.Console.WriteLine($"Generating {meshData.meshRoot.Name}");

            foreach (var deformerData in rawBoneDeformerData.ToLookup(boneDeformerData => boneDeformerData.flverBoneIndex))
            {
                FLVER2 flver = Souls.flver;

                FLVER.Bone flverBone = flver.Bones[deformerData.Key];

                DsBoneData boneData = Souls.skeleton.boneDatas.Single(boneData => boneData.flverBone == flverBone);

                //System.Console.WriteLine($"Exporting {deformerData.Key} : {flverBone.Name} with {deformerData.Count(weight=>weight.boneWeight > 0)} vertices");

                FbxCluster boneCluster = FbxCluster.Create(skin, meshData.meshRoot.Name + "_" + boneData.exportData.SoulsData.Name + "_Cluster");

                boneCluster.SetLink(boneData.exportData.FbxNode);
                boneCluster.SetLinkMode(FbxCluster.ELinkMode.eTotalOne);
                boneCluster.SetControlPointIWCount(deformerData.Count());
                boneCluster.SetTransformMatrix(Souls.meshData.FbxNode.EvaluateGlobalTransform());
                boneCluster.SetTransformLinkMatrix(boneData.exportData.FbxNode.EvaluateGlobalTransform());
                foreach (BoneIndexToWeightPair boneWeightPair in deformerData)
                {
                    boneCluster.AddControlPointIndex(boneWeightPair.vertexIndex, boneWeightPair.boneWeight);

                    //Console.WriteLine("Bone {0} has vertex {1} with weight {2}", flverBone.Name, boneWeightPair.vertexIndex, boneWeightPair.boneWeight);
                }

                skin.AddCluster(boneCluster);
            }

            //foreach (var dd in rawBoneDeformerData.GroupBy(biwp => biwp.vertexIndex).Where(group => group.Count() > 2))
            //{
                //System.Console.WriteLine($"Vertex {dd.Key} : {dd.Count()}");
            //}

            //var set = new HashSet<int>();
            //for (int vertexIndex = 0; vertexIndex < meshData.mesh.Vertices.Count; ++vertexIndex)
            //{
            //    if (!rawBoneDeformerData.Any(x=>x.vertexIndex == vertexIndex))
            //    {
            //        set.Add(vertexIndex);
            //    }
            //}

            //System.Console.WriteLine($"Total {set.Count} unweighted nodes");

            return skin;
        }
    }
}
