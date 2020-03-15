using System;
using Autodesk.Fbx;
using SoulsFormats;
using System.Linq;
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
            //var reader = new SoulsFormats.BXF4Reader(@"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\Data1.bhd", @"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\Data1.bdt");
            //FLVER2 flver = reader.Files.Where(file => file.Name.Contains("1300"))
            //    .Select(file => reader.ReadFile(file))
            //    .Select(fileContents => new BND4Reader(fileContents))
            //    .SelectMany(bndReader => bndReader.Files.Select(file => bndReader.ReadFile(file)))
            //    .Where(fileContents => FLVER2.Is(fileContents))
            //    .Select(fileContents => FLVER2.Read(fileContents))
            //    .First();

            FLVER2 flver = System.IO.Directory.GetFiles(@"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\chr\", "*1300*")
                .Select(path => new BND4Reader(path))
                .SelectMany(bndReader => bndReader.Files.Select(file => bndReader.ReadFile(file)))
                .Where(fileContents => FLVER2.Is(fileContents))
                .Select(fileContents => FLVER2.Read(fileContents))
                .First();

            
            FbxManager m = FbxManager.Create();

            FbxScene scene = FbxScene.Create(m, "BlackKnight");

            FbxNode sceneRoot = scene.GetRootNode();

            var meshes = flver.Meshes.Select(mesh => new MeshExportData() { mesh = mesh, meshRoot = flver.Bones[mesh.DefaultBoneIndex] })
                .Select(meshExportData => new MeshExporter(scene, meshExportData))
                .Select(exporter => exporter.Fbx.CreateExportData(exporter.Souls.mesh));

            foreach (var exportedMesh in meshes)
            {
                sceneRoot.AddChild(exportedMesh.FbxNode);
            }

            using (FbxExporter ex = FbxExporter.Create(m, "Exporter"))
            {
                ex.Initialize("out.fbx");

                Console.WriteLine(ex.Export(scene));
            }

            m.Destroy();

            Console.WriteLine("All done");
        }
    }
}
