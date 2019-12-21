#pragma once

#include "json/json.hpp"
#include "Flver.h"
#include "Types.h"

using nlohmann::json;
namespace gmtl
{
	inline void from_json(const json& j, Vector3& val)
	{
		double x, y, z;

		j.at("X").get_to(x);
		j.at("Y").get_to(y);
		j.at("Z").get_to(z);

		val = Vector3(x, y, z);
	}

	inline void from_json(const json& j, Vector4& val)
	{
		double x, y, z, w;
		j.at("X").get_to(x);
		j.at("Y").get_to(y);
		j.at("Z").get_to(z);
		j.at("W").get_to(w);

		val = Vector4(x, y, z, w);
	}
}

namespace Flver
{
	
#define JSON_FROM(PropName) j.at( #PropName ).get_to(val.PropName)

	inline void from_json(const json& j, FaceSet& val)
	{
		JSON_FROM(TriangleStrip);
		JSON_FROM(CullBackfaces);
		JSON_FROM(Indices);
	}
	inline void from_json(const json& j, VertexBoneWeights& val)
	{
		JSON_FROM(A);
		JSON_FROM(B);
		JSON_FROM(C);
		JSON_FROM(D);
	}

	inline void from_json(const json& j, VertexBoneIndices& val)
	{
		JSON_FROM(A);
		JSON_FROM(B);
		JSON_FROM(C);
		JSON_FROM(D);
	}

	inline void from_json(const json& j, VertexColor& val)
	{
		JSON_FROM(R);
		JSON_FROM(G);
		JSON_FROM(B);
		JSON_FROM(A);
	}
	inline void from_json(const json& j, Vertex& val)
	{
		JSON_FROM(Position);
		JSON_FROM(BoneWeights);
		JSON_FROM(UsesBoneWeights);
		JSON_FROM(BoneIndices);
		JSON_FROM(UsesBoneIndices);
		JSON_FROM(Normal);
		JSON_FROM(NormalW);
		JSON_FROM(UVs);
		JSON_FROM(Tangents);
		JSON_FROM(Bitangent);
		JSON_FROM(Colors);
	}



	inline void from_json(const json& j, Mesh& val)
	{
		JSON_FROM(Dynamic);
		JSON_FROM(MaterialIndex);
		JSON_FROM(DefaultBoneIndex);
		JSON_FROM(BoneIndices);
		JSON_FROM(FaceSets);
		JSON_FROM(Vertices);
	}

	inline void from_json(const json& j, Bone& val)
	{
		JSON_FROM(Name);
		JSON_FROM(ParentIndex);
		JSON_FROM(ChildIndex);
		JSON_FROM(PreviousSiblingIndex);
		JSON_FROM(NextSiblingIndex);
		JSON_FROM(Translation);
		JSON_FROM(Rotation);
		JSON_FROM(Scale);
	}


	inline void from_json(const json& j, Flver& val)
	{
		JSON_FROM(Meshes);
		JSON_FROM(Bones);
	}
#undef JSON_FROM
}
