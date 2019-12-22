#pragma once

#include <map>
#include "Types.h"

#include <string>
#include <vector>

namespace Hkx
{
	template <class T>
	struct SplineChannel
	{
		std::vector<T> Values;
		bool IsDynamic;

		bool is_actually_present = false;
	};

	struct SplineTrackVector3
	{
		SplineChannel<float> ChannelX;
		SplineChannel<float> ChannelY;
		SplineChannel<float> ChannelZ;
		std::vector<uint8> Knots;
		uint8 Degree;
	};

	struct SplineTrackQuaternion
	{
		SplineChannel<Quaternion> Channel;
		std::vector<uint8> Knots;
		uint8 Degree;
	};


	enum class ScalarQuantizationType : uint8
	{
		BITS8 = 0,
		BITS16 = 1,
	};

	enum class RotationQuantizationType : uint8
	{
		POLAR32 = 0, //4 bytes long
		THREECOMP40 = 1, //5 bytes long
		THREECOMP48 = 2, //6 bytes long
		THREECOMP24 = 3, //3 bytes long
		STRAIGHT16 = 4, //2 bytes long
		UNCOMPRESSED = 5, //16 bytes long
	};

	enum class FlagOffset : uint8
	{
		StaticX = 0b00000001,
		StaticY = 0b00000010,
		StaticZ = 0b00000100,
		StaticW = 0b00001000,
		SplineX = 0b00010000,
		SplineY = 0b00100000,
		SplineZ = 0b01000000,
		SplineW = 0b10000000
	};

	struct TransformMask
	{
		//ScalarQuantizationType PositionQuantizationType;
		//RotationQuantizationType RotationQuantizationType;
		//ScalarQuantizationType ScaleQuantizationType;
		std::vector<FlagOffset> PositionTypes;
		std::vector<FlagOffset> RotationTypes;
		std::vector<FlagOffset> ScaleTypes;
	};

	struct HkxTrack
	{
		TransformMask Mask;

		bool HasSplinePosition;
		bool HasSplineRotation;
		bool HasSplineScale;

		bool HasStaticRotation;

		Vector3 StaticPosition;
		Quaternion StaticRotation;
		Vector3 StaticScale;
		SplineTrackVector3 SplinePosition;
		SplineTrackQuaternion SplineRotation;
		SplineTrackVector3 SplineScale;
	};

	struct HkxAnim
	{
		std::vector < std::vector<HkxTrack> > Tracks;
		std::string Name;
		std::vector<int> HkxBoneIndexToTransformTrackMap;
		std::vector<int> TransformTrackIndexToHkxBoneMap;
		int BlockCount;
		int FrameCount;
		int NumFramesPerBlock;
	};

	struct Bone
	{
		std::string Name;
		int LockTranslation;
	};

	struct Transform
	{
		Vector4 Position;
		Quaternion Rotation;
		Vector4 Scale;
	};

	struct HkaSkeleton
	{
		std::string Name;
		std::vector<short> ParentIndices;
		std::vector <Bone> Bones;
		std::vector<Transform> Transforms;
		std::vector<float> ReferenceFloats;
	};

	struct File
	{
		std::map<std::string, HkxAnim> File;
	};
}
