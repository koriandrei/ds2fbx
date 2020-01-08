#pragma once
#include "Types.h"

#pragma warning(push, 0)
#include <vector>
#include <string>
#pragma warning(pop)

namespace Flver
{

	struct Texture
	{
		/// <summary>
		/// The type of texture this is, corresponding to the entries in the MTD.
		/// </summary>
		std::string Type;

			/// <summary>
			/// Network path to the texture file to use.
			/// </summary>
		std::string Path;

			/// <summary>
			/// Unknown.
			/// </summary>
		Vector2 Scale;

			/// <summary>
			/// Unknown.
			/// </summary>
		uint8 Unk10;

			/// <summary>
			/// Unknown.
			/// </summary>
		bool Unk11;

			/// <summary>
			/// Unknown.
			/// </summary>
		float Unk14;

			/// <summary>
			/// Unknown.
			/// </summary>
		float Unk18;

			/// <summary>
			/// Unknown.
			/// </summary>
		float Unk1C;
	};

	/// <summary>
	/// A reference to an MTD file, specifying textures to use.
	/// </summary>
	struct Material
	{
		/// <summary>
		/// Identifies the mesh that uses this material, may include keywords that determine hideable parts.
		/// </summary>
		std::string Name;

			/// <summary>
			/// Virtual path to an MTD file.
			/// </summary>
		std::string MTD;

			/// <summary>
			/// Unknown.
			/// </summary>
		int Flags;

			/// <summary>
			/// Textures used by this material.
			/// </summary>
		std::vector<Texture> Textures;

			/// <summary>
			/// Index to the flver's list of GX lists.
			/// </summary>
		int GXIndex;

			/// <summary>
			/// Unknown; only used in Sekiro.
			/// </summary>
		int Unk18;
	};

	struct FaceSet
	{
		enum class FaceSetFlags : unsigned int
		{
			/// <summary>
			/// Just your average everyday face set.
			/// </summary>
			None = 0,

			/// <summary>
			/// Low detail mesh.
			/// </summary>
			LodLevel1 = 0x01000000,

			/// <summary>
			/// Really low detail mesh.
			/// </summary>
			LodLevel2 = 0x02000000,

			/// <summary>
			/// Many meshes have a copy of each faceset with and without this flag. If you remove them, motion blur stops working.
			/// </summary>
			MotionBlur = 0x80000000,
		};

		int Flags;
		bool TriangleStrip;
		bool CullBackfaces;
		std::vector<int> Indices;
	};

	struct VertexBoneWeights
	{
		float A, B, C, D;
	};

	struct VertexBoneIndices
	{
		int A, B, C, D;
	};

	struct VertexColor
	{
		float A, R, G, B;
	};

	struct Vertex
	{
		Vector3 Position;
		VertexBoneWeights BoneWeights;
		bool UsesBoneWeights;
		VertexBoneIndices BoneIndices;
		bool UsesBoneIndices;
		Vector3 Normal;
		int NormalW;
		std::vector<Vector3> UVs;
		std::vector<Vector4> Tangents;
		Vector4 Bitangent;
		std::vector<VertexColor> Colors;
	};

	struct Mesh
	{
		bool Dynamic;
		int MaterialIndex;
		int DefaultBoneIndex;
		std::vector<int> BoneIndices;
		std::vector<FaceSet> FaceSets;
		std::vector<Vertex> Vertices;
	};

	struct Bone
	{
		std::string Name;
		short ParentIndex;
		short ChildIndex;
		short PreviousSiblingIndex;
		short NextSiblingIndex;
		Vector3 Translation;
		// euler radians
		Vector3 Rotation;
		Vector3 Scale;
	};

	struct Flver
	{
		std::vector<Mesh> Meshes;
		std::vector<Bone> Bones;
		std::vector<Material> Materials;
	};
}

