using System;
using System.Collections.Generic;
using Autodesk.Fbx;
using SoulsFormats;
using SFAnimExtensions;
using System.Linq;
using CommandLine;
namespace Ds3FbxSharp
{
    class Program
    {
        public class Options
        {
            [Option]
            public string BasePath { get; set; }

            [Option(Separator =',')]
            public IEnumerable<string> Paths { get; set; }

            [Option]
            public string ModelId { get; set; }

            public enum Export
            {
                Mesh,
                Skeleton,
                Skin,
                Animation,
            }

            public static IEnumerable<Export> ExpandExports(Export export)
            {
                switch (export)
                {
                    case Export.Skin:
                        return new[] { Export.Mesh, Export.Skeleton };
                    case Export.Animation:
                        return new[] { Export.Skeleton };
                }

                return Array.Empty<Export>();
            }

            [Option(Separator =',', Required = true)]
            public IEnumerable<Export> Exports { get; set; }
        }

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

        static IEnumerable<string> GetPathsToLoad(Options options)
        {
            return options.Paths
                .Select(path => System.IO.Path.IsPathRooted(path) ? path : System.IO.Path.Combine(options.BasePath, path))
                .SelectMany(path => System.IO.File.Exists(path) ? new[] { path } : System.IO.Directory.GetFiles(path, "*.dcx"));
        }

        static bool ShouldReadFile(BinderFileHeader header, Options options)
        {
            return true;
        }

        static int RunAndReturnExitCode(Options options)
        {
            string charToLookFor = "c1100";

            if (options.ModelId != null)
            {
                charToLookFor = options.ModelId;
            }

            var fileLookup = GetPathsToLoad(options)
                .Select(path => new BND4Reader(path))
                .SelectMany(bndReader => bndReader.Files
                    .Where(file => ShouldReadFile(file, options))
                    .Select(file => (header: file, contents: bndReader.ReadFile(file)))
                )
                .ToLookup(t => GetModelDataType(t.contents))
                ;

            Console.WriteLine("Loaded files");

            FLVER2 meshFlver = fileLookup[ModelDataType.Flver].Select(t => FLVER2.Read(t.contents)).Where(flver => flver.Meshes.Count > 0).FirstOrDefault();

            if (meshFlver == null && options.Exports.Contains(Options.Export.Mesh))
            {
                throw new Exception("No flver with at least one mesh could be found, cannot export meshes");
            }

            var regexR1 = new System.Text.RegularExpressions.Regex(@".*a0\d\d_03[04]0[0-2]0.hkx");
            var regexR2 = new System.Text.RegularExpressions.Regex(@".*a0\d\d_03[04]3[24][0-1].hkx");
            var regexRunning = new System.Text.RegularExpressions.Regex(@".*a0\d\d_030[569]00.hkx");
            var regexWA = new System.Text.RegularExpressions.Regex(@".*a0\d\d_036\d\d\d.hkx");

            Func< (BinderFileHeader header, byte[] contents), bool> filterHkxs = (t) => {
                if (t.header.Name.Contains("keleton"))
                {
                    return true;
                }
                return new[] { regexR1, regexR2, regexRunning, /*regexWA,*/ }.Any(r => r.IsMatch(t.header.Name));
            };

            var hkxs = fileLookup[ModelDataType.Hkx].Where(t=>!t.header.Name.EndsWith("2000_c.hkx")).Where(filterHkxs).Select(t => HKX.Read(t.contents)).ToArray();
            var skeletons = GetHkxObjects<HKX.HKASkeleton>(hkxs);

            HKX.HKASkeleton hkaSkeleton = skeletons.FirstOrDefault();


            HKX.HKAAnimationBinding binding = GetHkxObjects<HKX.HKAAnimationBinding>(hkxs).First();

            FbxManager m = FbxManager.Create();

            FbxScene scene = FbxScene.Create(m, "BlackKnight");



            FbxNode sceneRoot = scene.GetRootNode();

            IList<FbxExportData<MeshExportData, FbxMesh>> meshes = null;

            if (options.Exports.Contains(Options.Export.Mesh))
            {
                meshes = meshFlver?.Meshes.Select(mesh => new MeshExportData() { mesh = mesh, meshRoot = meshFlver.Bones[mesh.DefaultBoneIndex] })
                    .Select(meshExportData => new MeshExporter(scene, meshExportData))
                    .Select(exporter => exporter.Fbx.CreateExportData(exporter.Souls))
                    //.Take(1)
                    .ToList();

                if (meshes != null)
                {
                    foreach (var exportedMesh in meshes)
                    {
                        sceneRoot.AddChild(exportedMesh.FbxNode);
                    }
                }
                Console.WriteLine("Exported meshes");
            }

            DsSkeleton skeleton = null;

            if (options.Exports.Contains(Options.Export.Skeleton))
            {
                List<DsBone> bones = SkeletonFixup.FixupDsBones(meshFlver, hkaSkeleton).ToList();

                skeleton = new SkeletonExporter(scene, meshFlver, hkaSkeleton, bones).ParseSkeleton();

                Console.WriteLine("Exported skeleton");


                FbxPose pose = FbxPose.Create(scene, "Pose");

                pose.SetIsBindPose(true);

                foreach (var skeletonBone in skeleton.boneDatas)
                {
                    var boneTransform = skeletonBone.exportData.FbxNode.EvaluateGlobalTransform();
                    pose.Add(skeletonBone.exportData.FbxNode, new FbxMatrix(boneTransform));
                }

                scene.AddPose(pose);

                Console.WriteLine("Exported pose");
            }

            if (options.Exports.Contains(Options.Export.Skin))
            {
                foreach (var meshData in meshes)
                {
                    FbxSkin generatedSkin = new SkinExporter(meshData.FbxData, new SkinExportData(meshData, skeleton, meshFlver)).Fbx;
                    meshData.FbxData.AddDeformer(generatedSkin);
                };

                Console.WriteLine("Exported skin");
            }

            if (options.Exports.Contains(Options.Export.Animation))
            {
                Console.WriteLine("Exporting animations...");

                Func<HKX.HKASplineCompressedAnimation, HKX.HKADefaultAnimatedReferenceFrame, int, bool> b = ((animation, refFrame, animIndex) =>
                {
                    SFAnimExtensions.Havok.HavokAnimationData anim = new SFAnimExtensions.Havok.HavokAnimationData_SplineCompressed(animIndex.ToString("0000"), hkaSkeleton, refFrame, binding, animation);

                    var animExporter = new AnimationExporter(scene, new AnimationExportData() { dsAnimation = anim, animRefFrame = refFrame, hkaAnimationBinding = binding, skeleton = skeleton, name = animIndex.ToString() });

                    try
                    {
                        var dummy = animExporter.Fbx;
                    }
                    catch (AnimationExporter.AnimationExportException)
                    {
                        System.Console.WriteLine("Eh, an animation will be skipped");
                    }
                    return true;
                });

                const int animsToTake = 100;

                int index = 0;
                foreach (var animData in GetHkxObjects<HKX.HKASplineCompressedAnimation, HKX.HKASplineCompressedAnimation, HKX.HKADefaultAnimatedReferenceFrame>(hkxs).ToList().Skip(0).Take(animsToTake))
                {
                    b(animData.Item1, animData.Item3, index++);
                }
            }

            PrintFbxNode(sceneRoot);

            using (FbxExporter ex = FbxExporter.Create(m, "Exporter"))
            {
                ex.Initialize(charToLookFor + "_out.fbx");

                Console.WriteLine(ex.Export(scene));
            }

            m.Destroy();

            Console.WriteLine("All done");

            return 0;
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(options => RunAndReturnExitCode(CookOptions(options)), error=>1);
        }

        private static Options CookOptions(Options options)
        {
            return new Options() { BasePath = options.BasePath, ModelId = options.ModelId, Paths = options.Paths, Exports = CookExports(options.Exports) };
        }

        private static IEnumerable<Options.Export> CookExports(IEnumerable<Options.Export> exports)
        {
            return exports.SelectMany(export => new[] { export }.Concat(Options.ExpandExports(export))).ToHashSet();
        }
    }
}
