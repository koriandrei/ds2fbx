using System;
using Autodesk.Fbx;
namespace Ds3FbxSharp
{
    class Program
    {
        static void AddPoly(FbxMesh mesh, params int[] polyIndices)
            {
            mesh.BeginPolygon();
            foreach (int polyIndex in polyIndices)
            {
                mesh.AddPolygon(polyIndex);
            }
            mesh.EndPolygon();
            }

        static void Main(string[] args)
        {
            FbxManager m = FbxManager.Create();

            FbxScene scene = FbxScene.Create(m, "BlackKnight");

            FbxNode sceneRoot = scene.GetRootNode();

            FbxSkeleton rootBone = FbxSkeleton.Create(scene, "Root_Bone");
            rootBone.SetSkeletonType(FbxSkeleton.EType.eRoot);
            FbxSkeleton limbBone = FbxSkeleton.Create(scene, "Limb_Bone");
            limbBone.SetSkeletonType(FbxSkeleton.EType.eLimb);

            FbxNode rootBoneNode = FbxNode.Create(scene, rootBone.GetName() + "_Node");
            rootBoneNode.SetNodeAttribute(rootBone);
            FbxNode limbBoneNode = FbxNode.Create(scene, limbBone.GetName() + "_Node");
            limbBoneNode.SetNodeAttribute(limbBone);

            rootBoneNode.AddChild(limbBoneNode);

            limbBoneNode.LclTranslation.Set(new FbxDouble3(10, 0, 5));

            FbxPose bindPose = FbxPose.Create(scene, "BindPose");

            bindPose.SetIsBindPose(true);

            bindPose.Add(limbBoneNode, new FbxMatrix( limbBoneNode.EvaluateGlobalTransform()));
            bindPose.Add(rootBoneNode, new FbxMatrix(rootBoneNode.EvaluateGlobalTransform()));

            scene.AddPose(bindPose);

            sceneRoot.AddChild(rootBoneNode);

            FbxMesh mesh = FbxMesh.Create(scene, "Mesh");

            mesh.InitControlPoints(8);
            mesh.SetControlPointAt(new FbxVector4(-1, -1, -1), 0);
            mesh.SetControlPointAt(new FbxVector4(1, -1, -1), 1);
            mesh.SetControlPointAt(new FbxVector4(1, 1, -1), 2);
            mesh.SetControlPointAt(new FbxVector4(-1, 1, -1), 3);

            mesh.SetControlPointAt(new FbxVector4(-1, -1, 1), 4);
            mesh.SetControlPointAt(new FbxVector4(1, -1, 1), 5);
            mesh.SetControlPointAt(new FbxVector4(1, 1, 1), 6);
            mesh.SetControlPointAt(new FbxVector4(-1, 1, 1), 7);

            AddPoly(mesh, 0, 1, 2);
            AddPoly(mesh, 2, 3, 0);

            AddPoly(mesh, 5, 1, 2);
            AddPoly(mesh, 2, 6, 5);

            FbxNode meshNode = FbxNode.Create(scene, mesh.GetName() + "_Node");

            meshNode.SetNodeAttribute(mesh);

            sceneRoot.AddChild(meshNode);

            FbxSkin skin = FbxSkin.Create(scene, "Skin");

            FbxCluster rootCluster = FbxCluster.Create(scene, "RootCluster");

            rootCluster.SetLink(rootBoneNode);

            rootCluster.SetControlPointIWCount(2);

            rootCluster.AddControlPointIndex(0, 1);
            rootCluster.AddControlPointIndex(1, 1);

            FbxCluster limbCluster = FbxCluster.Create(scene, "RootCluster");

            limbCluster.SetLink(limbBoneNode);

            limbCluster.SetControlPointIWCount(2);

            limbCluster.AddControlPointIndex(2, 1);
            limbCluster.AddControlPointIndex(3, 1);

            skin.AddCluster(rootCluster);
            skin.AddCluster(limbCluster);

            mesh.AddDeformer(skin);

            FbxExporter ex = FbxExporter.Create(m, "Exporter");

            ex.Initialize("out.fbx");

            Console.WriteLine(ex.Export(scene));

            m.Destroy();
        }
    }
}
