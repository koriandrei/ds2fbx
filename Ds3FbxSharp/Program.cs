using System;
using System.Collections.Generic;
using Autodesk.Fbx;
using SoulsFormats;
using SFAnimExtensions;
using System.Linq;

namespace Ds3FbxSharp
{
    public abstract class MyExporter {
        public abstract ExporterMesh CreateMesh(string meshName);
    };

    class GltfExporter : MyExporter
    {
        public override ExporterMesh CreateMesh(string meshName)
        {
            return new GltfExporterMesh(meshName);
        }
    }

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

        static (T1, T2, T3) GetHkxObjectsFromHkx<T1, T2, T3>(HKX hkx) where T1 : HKX.HKXObject where T2 : HKX.HKXObject
        {
            return (hkx.DataSection.Objects.OfType<T1>().SingleOrDefault(), hkx.DataSection.Objects.OfType<T2>().SingleOrDefault(), hkx.DataSection.Objects.OfType<T3>().SingleOrDefault());
        }

        static (T1, T2) GetHkxObjectsFromHkx<T1, T2>(HKX hkx) where T1 : HKX.HKXObject where T2 : HKX.HKXObject
        {
            var objects = GetHkxObjectsFromHkx<T1, T2, T2>(hkx);
            return (objects.Item1, objects.Item2);
        }

        static T1 GetHkxObjectsFromHkx<T1>(HKX hkx) where T1 : HKX.HKXObject
        {
            var objects = GetHkxObjectsFromHkx<T1, T1, T1>(hkx);
            return objects.Item1;
        }

        static IEnumerable<T> GetHkxObjects<T>(IEnumerable<HKX> hkxs) where T : HKX.HKXObject
        {
            return hkxs.SelectMany(hkx => hkx.DataSection.Objects).Where(hkxObject => hkxObject is T).Select(hkxObject => (T)hkxObject);
        }

        static IEnumerable<(T1, T2, T3)> GetHkxObjects<T1, T2, T3>(IEnumerable<HKX> hkxs) where T1 : HKX.HKXObject where T2 : HKX.HKXObject
where T3 : HKX.HKXObject

        {
            return hkxs.Select(GetHkxObjectsFromHkx<T1, T2, T3>).Where(tuple => tuple.Item1 != null && tuple.Item2 != null && tuple.Item3 != null);
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
                .Concat(System.IO.Directory.GetFiles(@"G:\SteamLibrary\steamapps\common\DARK SOULS III\Game\parts\", "bd_m_*bnd.dcx"))
                .Select(path => new BND4Reader(path))
                .SelectMany(bndReader => bndReader.Files.Where(file =>
                {
                    if (file.Name.EndsWith("hkx"))
                    {
                        bool isAnimHkx = file.Name.Substring(file.Name.LastIndexOf("\\") + 1).StartsWith("a10");

                        return isAnimHkx;
                    }
                    return true;
                }).Select(file => bndReader.ReadFile(file)))
                //.ToList()
                //.GroupBy(fileContents=>GetModelDataType(fileContents))
                //.ToDictionary()
                .ToLookup(GetModelDataType)
                //.Where(fileContents => FLVER2.Is(fileContents))
                //.Select(fileContents => FLVER2.Read(fileContents))
                //.First()
                ;

            Console.WriteLine("Loaded files");

            FLVER2 flver = fileLookup[ModelDataType.Flver].Select(FLVER2.Read).Where(flver => flver.Meshes.Count > 0).ElementAt(7);
            var hkxs = fileLookup[ModelDataType.Hkx].Select(HKX.Read);
            var skeletons = GetHkxObjects<HKX.HKASkeleton>(hkxs);

            HKX.HKASkeleton hkaSkeleton = skeletons.FirstOrDefault();


            HKX.HKAAnimationBinding binding = GetHkxObjects<HKX.HKAAnimationBinding>(hkxs).First();

            MyExporter exporter = new GltfExporter();

            string sceneName = "BlackKnight";
            //FbxScene scene = FbxScene.Create(m, sceneName);
            SharpGLTF.Scenes.SceneBuilder sceneBuilder = new SharpGLTF.Scenes.SceneBuilder(sceneName);

            new SharpGLTF.Geometry.MeshBuilder<SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent>().AddMesh;
            // new SharpGLTF.Materials.MaterialBuilder();

            var meshes = flver?.Meshes.Select(mesh => new MeshExportData() { mesh = mesh, meshRoot = flver.Bones[mesh.DefaultBoneIndex] })
                .Select(meshExportData => new MeshExporter(exporter, meshExportData))
                .Select(exporter => exporter.Fbx.CreateExportData(exporter.Souls))
                .ToList();

            if (meshes != null)
            {
                foreach (var exportedMesh in meshes)
                {
                    sceneRoot.AddChild(exportedMesh.FbxNode);
                }
            }

            Console.WriteLine("Exported meshes");

            List<DsBone> bones = SkeletonFixup.FixupDsBones(flver, hkaSkeleton).ToList();

            DsSkeleton skeleton = new SkeletonExporter(scene, flver, hkaSkeleton, bones).ParseSkeleton();

            Console.WriteLine("Exported skeleton");

            if (meshes != null)
            {
                foreach (var meshData in meshes)
                {
                    FbxSkin generatedSkin = new SkinExporter(meshData.FbxData, new SkinExportData(meshData.SoulsData, skeleton, flver)).Fbx;
                    meshData.FbxData.AddDeformer(generatedSkin);
                };
            }

            Console.WriteLine("Exported skin");

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

            Console.WriteLine("Exporting animations...");

            Func<HKX.HKASplineCompressedAnimation, HKX.HKADefaultAnimatedReferenceFrame, int, bool> b = ((animation, refFrame, animIndex) =>
            {
                SFAnimExtensions.Havok.HavokAnimationData anim = new SFAnimExtensions.Havok.HavokAnimationData_SplineCompressed(animIndex.ToString(), hkaSkeleton, refFrame, binding, animation);

                var animExporter = new AnimationExporter(scene, new AnimationExportData() { dsAnimation = anim, animRefFrame = refFrame, hkaAnimationBinding = binding, skeleton = skeleton, name = animIndex.ToString() });

                var dummy = animExporter.Fbx;

                return dummy == null;
            });

            const int animsToTake = 5;

            int index = 0;
            foreach (var animData in GetHkxObjects<HKX.HKASplineCompressedAnimation, HKX.HKASplineCompressedAnimation, HKX.HKADefaultAnimatedReferenceFrame>(hkxs).ToList().Take(animsToTake))
            {
                b(animData.Item1, animData.Item3, index++);
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
