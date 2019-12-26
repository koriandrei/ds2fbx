#pragma once
#include "Types.h"

#pragma warning(push, 0)
#include <vector>
#include <string>
#pragma warning(pop)

namespace Flver
{
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
	};
}

