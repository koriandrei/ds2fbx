#pragma once
#include "Types.h"
#include <vector>
#include <string>

namespace Flver
{
	struct FaceSet
	{
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

