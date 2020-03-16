using System;
using Autodesk.Fbx;
using SoulsFormats;
using System.Linq;
namespace Ds3FbxSharp
{
    class Program
    {
        enum ModelDataType
        {
            Flver,
            Hkx,
            Unk,
        }

        private static ModelDataType GetModelDataType(byte[] fileContents)
        {
            if (FLVER2.Is(fileContents))
            {
                return ModelDataType.Flver;
            }

            if (HKX.Is(fileContents))
            {
                return ModelDataType.Hkx;
            }

            return ModelDataType.Unk;
        }

        static void PrintFbxNode(FbxNode node, string prefix = "")
        {
            Console.WriteLine(prefix + node.GetName());

            for (int childIndex = 0; childIndex < node.GetChildCount(); ++childIndex)
            {
                PrintFbxNode(node.GetChild(childIndex), prefix + "--");
            }
        }

        static void Main(string[] args)
        {
            //var reader = new SoulsFormats.BXF4Reader(@"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\Data1.bhd", @"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\Data1.bdt");
            //FLVER2 flver = reader.Files.Where(file => file.Name.Contains("1300"))
            //    .Select(file => reader.ReadFile(file))
            //    .Select(fileContents => new BND4Reader(fileContents))
            //    .SelectMany(bndReader => bndReader.Files.Select(file => bndReader.ReadFile(file)))
            //    .Where(fileContents => FLVER2.Is(fileContents))
            //    .Select(fileContents => FLVER2.Read(fileContents))
            //    .First();

            /*FLVER2 flver =*/

            string charToLookFor = "1300";

            if (args.Length > 0)
            {
                charToLookFor = args[0];
            }

            var fileLookup = System.IO.Directory.GetFiles(@"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\chr\", string.Format(System.Globalization.CultureInfo.InvariantCulture, "*{0}*bnd.dcx", charToLookFor))
                .Select(path => new BND4Reader(path))
                .SelectMany(bndReader => bndReader.Files.Select(file => bndReader.ReadFile(file)))
                //.GroupBy(fileContents=>GetModelDataType(fileContents))
                //.ToDictionary()
                .ToLookup(GetModelDataType)
                //.Where(fileContents => FLVER2.Is(fileContents))
                //.Select(fileContents => FLVER2.Read(fileContents))
                //.First()
                ;

            FLVER2 flver = fileLookup[ModelDataType.Flver].Select(FLVER2.Read).Single();

            HKX hkx = fileLookup[ModelDataType.Hkx].Select(HKX.Read).First();

            HKX.HKASkeleton hkaSkeleton = (HKX.HKASkeleton)hkx.DataSection.Objects.Single(hkaObject => hkaObject is HKX.HKASkeleton);


            FbxManager m = FbxManager.Create();

            FbxScene scene = FbxScene.Create(m, "BlackKnight");

            FbxNode sceneRoot = scene.GetRootNode();

            var meshes = flver.Meshes.Select(mesh => new MeshExportData() { mesh = mesh, meshRoot = flver.Bones[mesh.DefaultBoneIndex] })
                .Select(meshExportData => new MeshExporter(scene, meshExportData))
                .Select(exporter => exporter.Fbx.CreateExportData(exporter.Souls))
                .ToList();
            
            foreach (var exportedMesh in meshes)
            {
                sceneRoot.AddChild(exportedMesh.FbxNode);
            }

            var bones = SkeletonFixup.FixupDsBones(flver, hkaSkeleton).ToList();

            DsSkeleton skeleton = new SkeletonExporter(scene, flver, bones).ParseSkeleton();

            sceneRoot.AddChild(skeleton.SkeletonRootNode);

            foreach (var meshData in meshes)
            {
                FbxSkin generatedSkin = new SkinExporter(meshData.FbxData, new SkinExportData(meshData.SoulsData, skeleton, flver)).Fbx;
                meshData.FbxData.AddDeformer(generatedSkin);
            };

            {
                FbxPose pose = FbxPose.Create(scene, "Pose");

                pose.SetIsBindPose(true);

                foreach (var skeletonBone in skeleton.boneDatas)
                {
                    var boneTransform = skeletonBone.exportData.FbxNode.EvaluateGlobalTransform();
                    pose.Add(skeletonBone.exportData.FbxNode, new FbxMatrix(boneTransform));
                }

                scene.AddPose(pose);
            }

            PrintFbxNode(sceneRoot);

            using (FbxExporter ex = FbxExporter.Create(m, "Exporter"))
            {
                ex.Initialize(charToLookFor + "_out.fbx");

                Console.WriteLine(ex.Export(scene));
            }

            m.Destroy();

            Console.WriteLine("All done");
        }
    }
}
