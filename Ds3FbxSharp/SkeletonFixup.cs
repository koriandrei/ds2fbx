using System;
using System.Collections.Generic;
using System.Text;

using SoulsFormats;
using SFAnimExtensions;
using System.Linq;

namespace Ds3FbxSharp
{
    struct DsBone
    {
        public DsBone(FLVER.Bone flverBone, FLVER2 flver)
        {
            HkxBoneIndex = -1;

            Name = flverBone.Name;

            ParentName = flverBone.ParentIndex > 0 ? flver.Bones[flverBone.ParentIndex].Name : null;
        }

        public DsBone(HKX.Bone hkxBone, HKX.HKASkeleton hkaSkeleton) : this()
        {
            HkxBoneIndex = hkaSkeleton.Bones.GetArrayData().Elements.IndexOf(hkxBone);

            Name = hkxBone.Name.GetString();

            ParentName = InitializeHkxParentName(hkxBone, hkaSkeleton);
        }

        private string InitializeHkxParentName(HKX.Bone hkxBone, HKX.HKASkeleton hkaSkeleton)
        {
            short hkxParentBoneIndex = hkaSkeleton.ParentIndices.GetArrayData().Elements[HkxBoneIndex].data;

            if (hkxParentBoneIndex >= 0)
            {
                HKX.Bone hkxParentBone = hkaSkeleton.Bones.GetArrayData().Elements[hkxParentBoneIndex];

                return hkxParentBone.Name.GetString();
            }

            return null;
        }

        public DsBone(FLVER.Bone flverBone, FLVER2 flver, HKX.Bone hkxBone, HKX.HKASkeleton hkaSkeleton) : this(flverBone, flver)
        {
            HkxBoneIndex = hkaSkeleton.Bones.GetArrayData().Elements.IndexOf(hkxBone);

            ParentName = InitializeHkxParentName(hkxBone, hkaSkeleton);
        }

        public string Name { get; }

        public string ParentName { get; set; }

        public int HkxBoneIndex { get; }
    }



    static class SkeletonFixup
    {
        private static IEnumerable<DsBone> FixupDsBones(FLVER2 flver)
        {
            return flver.Bones.Select(flverBone => new DsBone(flverBone, flver));
        }
        public static IEnumerable<DsBone> FixupDsBones(FLVER2 flver, HKX.HKASkeleton hkx)
        {
            var skel = FixupDsBonesInternal(flver, hkx);

            Func<DsBone, DsBone> boneConversion = bone => {
                //if (bone.Name == "Pelvis")
                //{
                //    bone.ParentName = "RootRotXZ";
                //}

                if (bone.Name == "Spine")
                {
                    bone.ParentName = "Pelvis";
                }

                return bone;
            };

            return skel.Select(boneConversion);
        }

        private static IEnumerable<DsBone> FixupDsBonesInternal(FLVER2 flver, HKX.HKASkeleton hkx)
        {
            if (hkx == null)
            {
                return FixupDsBones(flver);
            }

            if (flver == null)
            {
                return FixupDsBones(hkx);
            }

            return flver.Bones.Join(hkx.Bones.GetArrayData().Elements, flverBone => flverBone.Name, hkxBone => hkxBone.Name.GetString(), (flverBone, hkxBone) =>
                    new DsBone(flverBone, flver, hkxBone, hkx)
            );

        }

        private static IEnumerable<DsBone> FixupDsBones(HKX.HKASkeleton hkx)
        {
            return hkx.Bones.GetArrayData().Elements.Select(hkxBone => new DsBone(hkxBone, hkx));
        }
    }
}
