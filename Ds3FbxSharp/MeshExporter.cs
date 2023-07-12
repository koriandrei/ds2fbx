using System;
using System.Collections.Generic;
using System.Text;
using SoulsFormats;
//using Autodesk.Fbx;
using System.Numerics;

using System.Linq;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;

namespace Ds3FbxSharp
{
    using MeshBuilder = SharpGLTF.Geometry.MeshBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormal, SharpGLTF.Geometry.VertexTypes.VertexColor1Texture1, SharpGLTF.Geometry.VertexTypes.VertexEmpty>;
    using PrimitiveBuilder = SharpGLTF.Geometry.PrimitiveBuilder<SharpGLTF.Materials.MaterialBuilder, SharpGLTF.Geometry.VertexTypes.VertexPositionNormal, SharpGLTF.Geometry.VertexTypes.VertexColor1Texture1, SharpGLTF.Geometry.VertexTypes.VertexEmpty>;
    using VertexBuilder = SharpGLTF.Geometry.VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexEmpty>;

    public static class FbxConversions
    {
        //public static FbxVector4 ToFbxVector4(this Vector3 vector)
        //{
        //    return new FbxVector4(vector.X, vector.Y, vector.Z);
        //}

        //public static FbxDouble3 ToFbxDouble3(this Vector3 vector)
        //{
        //    return new FbxDouble3(vector.X, vector.Y, vector.Z);
        //}

        //public static FbxVector2 ToFbxVector2(this Vector2 vector)
        //{
        //    return new FbxVector2(vector.X, vector.Y);
        //}

        public static Vector3 ToVector3(this Vector4 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }


        public static Vector3 QuaternionToEuler(this Quaternion q)
        {
            Vector3 angles = new Vector3();


            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp); // use 90 degrees if out of range
            else
                angles.Y = (float)Math.Asin(sinp);

            // yaw (z-axis rotation)
            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            if ((Math.Abs(Math.Abs(angles.X) - Math.Abs(angles.Z)) < 0.001) && (Math.Abs(Math.Abs(angles.X) - Math.PI) < 0.001))
            {
                angles.X = angles.Z = 0;
            }

            return angles * 180 / (float)Math.PI;
        }
    }
    
    public class FbxExportData<TSoulsData, TFbxData>
    {
        public FbxExportData(TSoulsData soulsData, TFbxData fbxData)
        {
            this.SoulsData = soulsData;
            this.FbxData = fbxData;
        }

        public TSoulsData SoulsData { get; }
        public TFbxData FbxData { get; }
    }

    public static class FbxExtensions
    {
#if false
        public static FbxMesh AddCompletePolygon(this FbxMesh mesh, params int[] vertexIndices)
        {
            mesh.BeginPolygon();

            foreach (int vertexIndex in vertexIndices)
            {
                mesh.AddPolygon(vertexIndex);
            }

            mesh.EndPolygon();

            return mesh;
        }

        public static FbxNode CreateNode(this FbxNodeAttribute nodeAttribute)
        {
            FbxNode node = FbxNode.Create(nodeAttribute, nodeAttribute.GetName() + "_Node");
            
            node.SetNodeAttribute(nodeAttribute);

            return node;
        }
#endif
        public static FbxExportData<TSoulsData, TFbxData> CreateExportData<TSoulsData, TFbxData>(this TFbxData fbxData, TSoulsData soulsData) where TFbxData: SharpGLTF.BaseBuilder
        {
            return new FbxExportData<TSoulsData, TFbxData>(soulsData, fbxData);
        }

        public static FbxExportData<TSoulsData, TFbxData> CreateExportDataWithScene<TSoulsData, TFbxData>(this TFbxData fbxData, TSoulsData soulsData, MyExporter exporter) where TFbxData: SharpGLTF.BaseBuilder
        {
            //FbxNode node = FbxNode.Create(scene, fbxData.GetName() + "_Node");

            //node.SetNodeAttribute(fbxData);
            
            return new FbxExportData<TSoulsData, TFbxData>(soulsData, fbxData);
        }
    }

    public struct MeshExportData
    {
        public FLVER2.Mesh mesh { get; set; }
        public FLVER.Bone meshRoot { get; set; }
    }

    public abstract class ExporterMesh
    {
        public abstract PrimitiveBuilder AddPrimitive(string name, int verticesCount);

        public abstract void InitPoints(int pointsCount);

        public abstract IMeshBuilder<MaterialBuilder> GetMesh();
    }
    class GltfExporterMesh : ExporterMesh
    {
        public GltfExporterMesh(string meshName)
        {
            builder = new MeshBuilder(meshName + "_Mesh");
            materialBuilder = new MaterialBuilder(meshName + "_Material");
        }

        public override PrimitiveBuilder AddPrimitive(string name, int verticesCount)
        {
            return builder.UsePrimitive(materialBuilder);
        }

        public override void InitPoints(int pointsCount)
        {
            //mesh.InitControlPoints(Souls.mesh.Vertices.Count);
            //builder.AddMesh();
        }
        public override IMeshBuilder<MaterialBuilder> GetMesh()
        {
            return builder;
        }

        MeshBuilder builder;
        SharpGLTF.Materials.MaterialBuilder materialBuilder;
    }

    public class MeshExporter : Exporter<MeshExportData, ExporterMesh>
    {
        public MeshExporter(MyExporter exporter, FLVER2.Mesh mesh) : base(exporter, new MeshExportData { mesh = mesh })
        {
        }

        public MeshExporter(MyExporter exporter, MeshExportData exportData) : base(exporter, exportData)
        {
        }

        protected override ExporterMesh GenerateFbx()
        {
            string meshName = (Souls.meshRoot != null ? Souls.meshRoot.Name : "") + "_Mesh";

            ExporterMesh mesh = MyExporter.CreateMesh(meshName);

            var primitive = mesh.AddPrimitive(meshName, Souls.mesh.Vertices.Count);
            //int layerIndex = mesh.CreateLayer();

            //FbxLayer layer = mesh.GetLayer(layerIndex);

            //FbxLayerContainer layerContainer = FbxLayerContainer.Create(Scene, meshName + "_LayerContainer");

            //FbxLayerElementUV uv = FbxLayerElementUV.Create(layerContainer, "Diffuse");
            //layer.SetUVs(uv);

            //FbxLayerElementNormal normal = FbxLayerElementNormal.Create(layerContainer, "Normal");
            //layer.SetNormals(normal);
            //normal.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            //normal.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);

            //FbxLayerElementBinormal binormal = FbxLayerElementBinormal.Create(layerContainer, "Binormal");
            //layer.SetBinormals(binormal);

            //FbxLayerElementTangent tangent = FbxLayerElementTangent.Create(layerContainer, "Tangent");
            //layer.SetTangents(tangent);
            //tangent.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            //tangent.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);

            Dictionary<int, VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4>> vertexIndexToBuilder = new Dictionary<int, VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4>>(Souls.mesh.Vertices.Count);

            for (int vertexIndex = 0; vertexIndex < Souls.mesh.Vertices.Count; ++vertexIndex)
            {
                FLVER.Vertex vertex = Souls.mesh.Vertices[vertexIndex];

                Vector3 position = vertex.Position;

                // this fixes vertex positions since otherwise the model is turned inside out
                // and it appears like it holds weapons in the left hand
                position.Z = -position.Z;

                Vector2 uvValue = new Vector2(0);
                if (vertex.UVs.Count > 0)
                {
                    uvValue.X = vertex.UVs[0].X;
                    uvValue.Y = vertex.UVs[0].Y;
                }

                var vertexPositionNormalTangent = new SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent(position, vertex.Normal, vertex.Tangents[0]);
                var vertexUv = new SharpGLTF.Geometry.VertexTypes.VertexTexture1(uvValue);

                VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4> builder = new VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent, SharpGLTF.Geometry.VertexTypes.VertexTexture1, SharpGLTF.Geometry.VertexTypes.VertexJoints4>(vertexPositionNormalTangent, vertexUv);

                vertexIndexToBuilder[vertexIndex] = builder;

                //normal.GetDirectArray().Add(vertex.Normal.ToFbxVector4());

                //tangent.GetDirectArray().Add(new FbxVector4(vertex.Tangents[0].X, vertex.Tangents[0].Y, vertex.Tangents[0].Z));



                //uv.GetDirectArray().Add(uvValue.ToFbxVector2());

                //mesh.SetControlPointAt(position.ToFbxVector4(), vertexIndex);
            }

            for (int faceSetIndex = 0; faceSetIndex < Souls.mesh.FaceSets.Count; ++faceSetIndex)
            {
                FLVER2.FaceSet faceSet = Souls.mesh.FaceSets[faceSetIndex];

                if (faceSet.Flags != FLVER2.FaceSet.FSFlags.None)
                {
                    continue;
                }

                for (int faceStartIndex = 0; faceStartIndex < faceSet.Indices.Count; faceStartIndex += 3)
                {
                    int[] vertexIndices = new int[3]{faceSet.Indices[faceStartIndex],
                        faceSet.Indices[faceStartIndex + 1],
                        faceSet.Indices[faceStartIndex + 2]};
                    primitive.AddTriangle(vertexIndexToBuilder[vertexIndices[0]], vertexIndexToBuilder[vertexIndices[1]], vertexIndexToBuilder[vertexIndices[2]]);

                    //mesh.AddCompletePolygon(
                    //    vertexIndices[0],
                    //    vertexIndices[1],
                    //    vertexIndices[2]
                    //);
                }
            }

            //mesh.BuildMeshEdgeArray();

            //FbxGeometryConverter converter = new FbxGeometryConverter(Scene.GetFbxManager());
            //converter.ComputeEdgeSmoothingFromNormals(mesh);
            //converter.ComputePolygonSmoothingFromEdgeSmoothing(mesh);

            return mesh;
        }
    }
}
