using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

using SoulsFormats;
//using Autodesk.Fbx;
namespace Ds3FbxSharp
{
#if false
    class SkinExportData
    {
        public SkinExportData(int activeMeshIndex, DsSkeleton skeleton, FLVER2 flver)
        {
            FLVER2.Mesh mesh = flver.Meshes[activeMeshIndex]; 
            meshData = new MeshExportData { mesh = mesh, meshRoot = flver.Bones[mesh.DefaultBoneIndex] };
            this.flver = flver;
            this.skeleton = skeleton;
        }

        public SkinExportData(MeshExportData meshData, DsSkeleton skeleton, FLVER2 flver)
        {
            this.meshData = meshData;
            this.skeleton = skeleton;
            this.flver = flver;
        }

        public MeshExportData meshData { get; }

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
            MeshExportData meshData = Souls.meshData;

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

            foreach (var deformerData in rawBoneDeformerData.ToLookup(boneDeformerData => boneDeformerData.flverBoneIndex))
            {
                FLVER2 flver = Souls.flver;

                FLVER.Bone flverBone = flver.Bones[deformerData.Key];

                DsBoneData boneData = Souls.skeleton.boneDatas.Single(boneData => boneData.flverBone == flverBone);

                FbxCluster boneCluster = FbxCluster.Create(skin, meshData.meshRoot.Name + "_" + boneData.exportData.SoulsData.Name + "_Cluster");

                boneCluster.SetLink(boneData.exportData.FbxNode);

                foreach (BoneIndexToWeightPair boneWeightPair in deformerData)
                {
                    boneCluster.AddControlPointIndex(boneWeightPair.vertexIndex, boneWeightPair.boneWeight);

                    //Console.WriteLine("Bone {0} has vertex {1} with weight {2}", flverBone.Name, boneWeightPair.vertexIndex, boneWeightPair.boneWeight);
                }

                skin.AddCluster(boneCluster);
            }

            return skin;
        }
    }
#endif
}
