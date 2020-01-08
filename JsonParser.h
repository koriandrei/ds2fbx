#pragma once

#pragma warning(push, 0)
#include "json/json.hpp"
#pragma warning(pop)

#include "Flver.h"
#include "Types.h"

#include "Hkx.h"

#define JSON_FROM(PropName) {auto json_prop = j.at( #PropName ); if (!json_prop.is_null()) { json_prop.get_to(val.PropName); }}

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

	inline void from_json(const json& j, Vector2& val)
	{
		double x, y;

		j.at("X").get_to(x);
		j.at("Y").get_to(y);

		val = Vector2(x, y);
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

	inline void from_json(const json& j, Quaternion& val)
	{
		double x, y, z, w;
		j.at("X").get_to(x);
		j.at("Y").get_to(y);
		j.at("Z").get_to(z);
		j.at("W").get_to(w);

		val = Quaternion(x, y, z, w);
	}
}

namespace Hkx
{
	template <class T>
	inline void from_json(const json& j, SplineChannel<T>& val)
	{
		JSON_FROM(Values);
		JSON_FROM(IsDynamic);

		val.is_actually_present = !j.at("Values").is_null();
	}

	inline void from_json(const json& j, SplineTrackVector3& val)
	{
		JSON_FROM(ChannelX);
		JSON_FROM(ChannelY);
		JSON_FROM(ChannelX);
		JSON_FROM(Knots);
		JSON_FROM(Degree);
	}

	inline void from_json(const json& j, SplineTrackQuaternion& val)
	{
		JSON_FROM(Channel);
		JSON_FROM(Knots);
		JSON_FROM(Degree);
	}
	
	inline Vector3 ParseStringVector(std::string vector_string)
	{
		size_t first_comma = vector_string.find(',');
		size_t second_comma = vector_string.find(',', first_comma + 1);
		std::string v1 = vector_string.substr(0, first_comma);
		std::string v2 = vector_string.substr(first_comma + 1, second_comma - first_comma - 1);
		std::string v3 = vector_string.substr(second_comma + 1);

		Vector3 v;
		
		v[0] = std::stod(v1);
		v[1] = std::stod(v2);
		v[2] = std::stod(v3);

		return v;
	}

	inline Vector4 ParseStringVector4(std::string vector_string)
	{
		size_t first_comma = vector_string.find(',');
		size_t second_comma = vector_string.find(',', first_comma + 1);
		size_t third_comma = vector_string.find(',', first_comma + 1);
		std::string v1 = vector_string.substr(0, first_comma);
		std::string v2 = vector_string.substr(first_comma + 1, second_comma - first_comma - 1);
		std::string v3 = vector_string.substr(second_comma + 1, third_comma - second_comma - 1);
		std::string v4 = vector_string.substr(third_comma + 1);

		Vector4 v;

		v[0] = std::stod(v1);
		v[1] = std::stod(v2);
		v[2] = std::stod(v3);
		v[3] = std::stod(v4);

		return v;
	}

	inline void from_json(const json& j, TransformMask& val)
	{
		JSON_FROM(PositionTypes);
		JSON_FROM(RotationTypes);
		JSON_FROM(ScaleTypes);
	}

	inline void from_json(const json& j, HkxTrack& val)
	{
		JSON_FROM(Mask);

		JSON_FROM(HasSplinePosition);
		JSON_FROM(HasSplineRotation);
		JSON_FROM(HasSplineScale);
		JSON_FROM(HasStaticRotation);

		std::string static_position, static_scale;

		j.at("StaticPosition").get_to(static_position);
		j.at("StaticScale").get_to(static_scale);

		val.StaticPosition = ParseStringVector(static_position);
		val.StaticScale = ParseStringVector(static_scale);

		JSON_FROM(/*Quaternion*/ StaticRotation);
		JSON_FROM(SplinePosition);
		JSON_FROM(SplineRotation);
		JSON_FROM(SplineScale);
	}

	inline void from_json(const json& j, HkxAnim& val)
	{
		assert(j.at("Tracks").is_array());
		// JSON_FROM(Tracks);

		for (const json::value_type& singleTrack : j.at("Tracks"))
		{
			assert(singleTrack.is_array());
			std::vector<HkxTrack> tracksArray;
			singleTrack.get_to(tracksArray);

			val.Tracks.push_back(singleTrack);
		}

		JSON_FROM(Name);
		JSON_FROM(HkxBoneIndexToTransformTrackMap);
		JSON_FROM(TransformTrackIndexToHkxBoneMap);
		const json& rootMotion = j.at("RootMotionFrames");
		if (rootMotion.is_null())
		{
			val.is_root_motion_present = false;
		}
		if (rootMotion.is_array())
		{
			for (const std::string stringRootMotionVector : rootMotion.get<std::vector<std::string>>())
			{
				val.RootMotionFrames.push_back(ParseStringVector4(stringRootMotionVector));
			}
			val.is_root_motion_present = true;
		}
		JSON_FROM(Duration);
		JSON_FROM(BlockCount);
		JSON_FROM(NumFramesPerBlock);
		JSON_FROM(FrameCount);
	}

	struct HkaString
	{
		operator std::string() const
		{
			return StringData;
		}

		std::string StringData;
	};

	inline void from_json(const json& j, HkaString& val)
	{
		JSON_FROM(StringData);
	}

	template <class T>
	struct HkaTypeBind
	{
		static_assert(std::false_type<T>::value, "This bind must be specialized for different types");
	};

	template <>
	struct HkaTypeBind<std::string>
	{
		typedef HkaString HkaType;
	};


	template <class T>
	struct HkaArray
	{
		std::vector<T> Values;

		operator std::vector<T>() const
		{
			return Values;
		}
	};

	template <class T>
	struct HkaTypeBind<std::vector<T>>
	{
		typedef HkaArray<T> HkaType;
	};

	template <class T>
	inline void from_json(const json& j, HkaArray<T>& val)
	{
		std::vector<typename HkaTypeBind<T>::HkaType> HkaValues;

		j.at("Values").get_to(HkaValues);

		for (const auto& HkaValue: HkaValues)
		{
			val.Values.push_back(HkaValue);
		}
	}

	struct HkaShort
	{
		short data;

		operator short() const
		{
			return data;
		}
	};

	template <>
	struct HkaTypeBind<short>
	{
		typedef HkaShort HkaType;
	};

	inline void from_json(const json& j, HkaShort& val)
	{
		JSON_FROM(data);
	}

	struct HkaFloat
	{
		operator float() const
		{
			return data;
		}

		float data;
	};

	template <>
	struct HkaTypeBind<float>
	{
		typedef HkaFloat HkaType;
	};

	inline void from_json(const json& j, HkaFloat& val)
	{
		JSON_FROM(data);
	}

#define HKA_JSON_FROM(PropName) { val.PropName = j.at(#PropName).get<HkaTypeBind<decltype(val.PropName)>::HkaType>(); }

	struct HkaVector
	{
		Vector4 Vector;

		operator Vector4() const
		{
			return Vector;
		}
	};

	template <>
	struct HkaTypeBind<Vector4>
	{
		typedef HkaVector HkaType;
	};

	struct HkaQuat
	{
		Vector4 Vector;

		operator Quaternion() const
		{
			return Quaternion(Vector[0], Vector[1], Vector[2], Vector[3]);
		}
	};

	template <>
	struct HkaTypeBind<Quaternion>
	{
		typedef HkaQuat HkaType;
	};


	inline void from_json(const json& j, HkaVector& val)
	{
		JSON_FROM(Vector);
	}

	inline void from_json(const json& j, HkaQuat& val)
	{
		JSON_FROM(Vector);
	}

	template <>
	struct HkaTypeBind<Bone>
	{
		typedef Bone HkaType;
	};

	template <>
	struct HkaTypeBind<Transform>
	{
		typedef Transform HkaType;
	};

	inline void from_json(const json& j, Bone& val)
	{
		HKA_JSON_FROM(Name);
		JSON_FROM(LockTranslation);
	}

	inline void from_json(const json& j, Transform& val)
	{
		HKA_JSON_FROM(Position);
		HKA_JSON_FROM(Rotation);
		HKA_JSON_FROM(Scale);
	}

	inline void from_json(const json& j, HkaSkeleton& val)
	{
		HKA_JSON_FROM(Name);
		HKA_JSON_FROM(ParentIndices);
		HKA_JSON_FROM(Bones);
		HKA_JSON_FROM(Transforms);
		HKA_JSON_FROM(ReferenceFloats);
	}

#undef HKA_JSON_FROM

	inline void from_json(const json& j, File& val)
	{
		assert(j.is_object());

		for (json::const_iterator iterator = j.cbegin(); iterator != j.cend(); ++iterator)
		{
			HkxAnim anim;
			iterator.value().get_to(anim);
			val.File.emplace(iterator.key(), anim);
		}
	}
}

namespace Flver
{
	inline void from_json(const json& j, Texture& val)
	{
		JSON_FROM(Type);
		JSON_FROM(Path);
		JSON_FROM(Scale);
		JSON_FROM(Unk10);
		JSON_FROM(Unk11);
		JSON_FROM(Unk14);
		JSON_FROM(Unk18);
		JSON_FROM(Unk1C);
	}

	inline void from_json(const json& j, Material& val)
	{
		JSON_FROM(Name);
		JSON_FROM(MTD);
		JSON_FROM(Flags);
		JSON_FROM(Textures);
		JSON_FROM(GXIndex);
		JSON_FROM(Unk18);
	}


	inline void from_json(const json& j, FaceSet& val)
	{
		JSON_FROM(TriangleStrip);
		JSON_FROM(CullBackfaces);
		JSON_FROM(Indices);
		JSON_FROM(Flags);
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
		JSON_FROM(Materials);
	}
}

#undef JSON_FROM
