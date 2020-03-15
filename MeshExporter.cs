﻿using System;
using System.Collections.Generic;
using System.Text;
using SoulsFormats;
using Autodesk.Fbx;
using System.Numerics;

using System.Linq;
namespace Ds3FbxSharp
{
    public static class FbxConversions
    {
        public static FbxVector4 ToFbx(this Vector3 vector)
        {
            return new FbxVector4(vector.X, vector.Y, vector.Z);
        }
        public static FbxVector2 ToFbx(this Vector2 vector)
        {
            return new FbxVector2(vector.X, vector.Y);
        }
    }

    public class FbxExportData<TSoulsData, TFbxData>
    {
        public FbxExportData(TSoulsData soulsData, TFbxData fbxData, FbxNode fbxNode)
        {
            this.SoulsData = soulsData;
            this.FbxData = fbxData;
            FbxNode = fbxNode;
        }

        public TSoulsData SoulsData { get; }
        public TFbxData FbxData { get; }
        public FbxNode FbxNode { get; }
    }

    public static class FbxExtensions
    {
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

        public static FbxExportData<TSoulsData, TFbxData> CreateExportData<TSoulsData, TFbxData>(this TFbxData fbxData, TSoulsData soulsData) where TFbxData: FbxNodeAttribute
        {
            return new FbxExportData<TSoulsData, TFbxData>(soulsData, fbxData, fbxData.CreateNode());
        }
    }

    public struct MeshExportData
    {
        public FLVER2.Mesh mesh { get; set; }
        public FLVER.Bone meshRoot { get; set; }
    }

    public class MeshExporter : Exporter<MeshExportData, FbxMesh>
    {
        public MeshExporter(FbxScene scene, FLVER2.Mesh mesh) : base(scene, new MeshExportData { mesh = mesh })
        {
        }

        public MeshExporter(FbxScene scene, MeshExportData exportData) : base(scene, exportData)
        {
        }

        protected override FbxMesh GenerateFbx()
        {
            string meshName = (Souls.meshRoot != null ? Souls.meshRoot.Name : "") + "_Mesh";

            FbxMesh mesh = FbxMesh.Create(Scene, meshName);

            mesh.InitControlPoints(Souls.mesh.Vertices.Count);

            int layerIndex = mesh.CreateLayer();

            FbxLayer layer = mesh.GetLayer(layerIndex);

            FbxLayerContainer layerContainer = FbxLayerContainer.Create(Scene, meshName + "_LayerContainer");

            FbxLayerElementUV uv = FbxLayerElementUV.Create(layerContainer, "Diffuse");
            layer.SetUVs(uv);

            FbxLayerElementNormal normal = FbxLayerElementNormal.Create(layerContainer, "Normal");
            layer.SetNormals(normal);
            normal.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            normal.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);

            FbxLayerElementBinormal binormal = FbxLayerElementBinormal.Create(layerContainer, "Binormal");
            layer.SetBinormals(binormal);

            FbxLayerElementTangent tangent = FbxLayerElementTangent.Create(layerContainer, "Tangent");
            layer.SetTangents(tangent);
            tangent.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
            tangent.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);

            for (int vertexIndex = 0; vertexIndex < Souls.mesh.Vertices.Count; ++vertexIndex)
            {
                FLVER.Vertex vertex = Souls.mesh.Vertices[vertexIndex];

                Vector3 position = vertex.Position;
                
                normal.GetDirectArray().Add(vertex.Normal.ToFbx());

                tangent.GetDirectArray().Add(new FbxVector4(vertex.Tangents[0].X, vertex.Tangents[0].Y, vertex.Tangents[0].Z));

                Vector2 uvValue = new Vector2(0);

                if (vertex.UVs.Count > 0)
                {
                    uvValue.X = vertex.UVs[0].X;
                    uvValue.Y = vertex.UVs[0].Y;
                }

                uv.GetDirectArray().Add(uvValue.ToFbx());

                mesh.SetControlPointAt(position.ToFbx(), vertexIndex);
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
                    mesh.AddCompletePolygon(faceSet.Indices[faceStartIndex], faceSet.Indices[faceStartIndex + 1], faceSet.Indices[faceStartIndex + 2]);
                }
            }

            mesh.BuildMeshEdgeArray();

            FbxGeometryConverter converter = new FbxGeometryConverter(Scene.GetFbxManager());
            converter.ComputeEdgeSmoothingFromNormals(mesh);
            converter.ComputePolygonSmoothingFromEdgeSmoothing(mesh);

            return mesh;
        }
    }
}