using System;
using System.Collections.Generic;
using System.Text;

using SoulsFormats;

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

        public DsBone(FLVER.Bone flverBone, FLVER2 flver, HKX.Bone hkxBone, HKX.HKASkeleton hkaSkeleton) : this(flverBone, flver)
        {
            HkxBoneIndex = hkaSkeleton.Bones.GetArrayData().Elements.IndexOf(hkxBone);

            short hkxParentBoneIndex = hkaSkeleton.ParentIndices.GetArrayData().Elements[HkxBoneIndex].data;

            if (hkxParentBoneIndex >= 0)
            {
                HKX.Bone hkxParentBone = hkaSkeleton.Bones.GetArrayData().Elements[hkxParentBoneIndex];

                ParentName = hkxParentBone.Name.GetString();
            }
        }

        public string Name { get; }

        public string ParentName { get; }

        public int HkxBoneIndex { get; }
    }



    static class SkeletonFixup
    {
        public static IEnumerable<DsBone> FixupDsBones(FLVER2 flver)
        {
            return flver.Bones.Select(flverBone => new DsBone(flverBone, flver));
        }

        public static IEnumerable<DsBone> FixupDsBones(FLVER2 flver, HKX.HKASkeleton hkx)
        {
            return flver.Bones.Join(hkx.Bones.GetArrayData().Elements, flverBone => flverBone.Name, hkxBone => hkxBone.Name.GetString(), (flverBone, hkxBone) =>
                    new DsBone(flverBone, flver, hkxBone, hkx)
            );

        }
    }
}
