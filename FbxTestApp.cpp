// FbxTestApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "pch.h"

#pragma warning(push, 0)
#include <iostream>
#include <fstream>
#include <algorithm>
#include <cmath>
#include <optional>

#include <args/args.hxx>
#include "json/json.hpp"
#include <fbxsdk.h>
#pragma warning(pop)

#include "Flver.h"

#include "JsonParser.h"

FbxVector4 Convert(const Vector3& vector)
{
	return FbxVector4(vector[0], vector[1], vector[2]);
}

FbxVector4 Convert(const Vector4& vector)
{
	return FbxVector4(vector[0], vector[1], vector[2], vector[3]);
}


template <class TFbx, class TRaw>
struct TFbxNodeHelper
{
	TFbxNodeHelper() : TFbxNodeHelper(nullptr, nullptr, TRaw())
	{
	}

	TFbxNodeHelper(TFbx* inObject, FbxNode* correspondingNode, const TRaw& inRaw) : object(inObject), node(correspondingNode), raw(inRaw)
	{
	}

	FbxNode* node;
	TFbx* object;
	TRaw raw;
};

template <class TFbx, class TRaw>
static TFbxNodeHelper<TFbx, TRaw> Create(FbxScene* scene, TFbx* object, const TRaw& raw)
{
	FbxNode* const node = FbxNode::Create(scene, object->GetName());
	node->SetNodeAttribute(object);
	return TFbxNodeHelper<TFbx, TRaw>(object, node, raw);
}

typedef TFbxNodeHelper<FbxSkeleton, Flver::Bone> ParseBone;
typedef TFbxNodeHelper<FbxMesh, Flver::Mesh> ParseMesh;


ParseMesh exportMesh(FbxScene* scene, const Flver::Mesh& mesh)
{
	FbxMesh* fbx = FbxMesh::Create(scene, "");

	//fbx->SetControlPointCount(mesh.Vertices.size());
	fbx->InitControlPoints(mesh.Vertices.size());
	fbx->InitNormals();
	
	FbxGeometryElementUV* uv = fbx->CreateElementUV("Diffuse");
	FbxGeometryElementTangent* tangents = fbx->CreateElementTangent();
	FbxGeometryElementBinormal* binormal = fbx->CreateElementBinormal();

	for (int vertexIndex = 0; vertexIndex < mesh.Vertices.size(); ++vertexIndex)
	{
		const Flver::Vertex& vertex = mesh.Vertices[vertexIndex];
		
		// models appear with flipped normals if input vector is used
		// swapping X and Z seems to do the trick
		Vector3 position = vertex.Position;
		position[2] = -position[2];

		fbx->SetControlPointAt(Convert(position), Convert(vertex.Normal), vertexIndex);
		
		tangents->GetDirectArray().Add(FbxVector4(vertex.Tangents[0][0], vertex.Tangents[0][1], vertex.Tangents[0][2], vertex.Tangents[0][3]));

		FbxVector4 vertexNormal = Convert(vertex.Normal);
		vertexNormal.Normalize();

		FbxVector4 vertexBitangent = Convert(vertex.Bitangent);
		vertexBitangent[3] = 0;
		vertexBitangent.Normalize();
		
		FbxVector4 vertexBinormal = vertexNormal.CrossProduct(vertexBitangent) * vertex.Bitangent[3];

		binormal->GetDirectArray().Add(vertexBinormal);

		if (vertex.UVs.size() > 0)
		{
			uv->GetDirectArray().Add(FbxVector2(vertex.UVs[0][0], vertex.UVs[0][1]));
		}
		else
		{
			uv->GetDirectArray().Add(FbxVector2(0, 0));
		}
	}

	FbxNode* node = FbxNode::Create(scene, "");
	node->SetNodeAttribute(fbx);

	for (int faceSetIndex = 0; faceSetIndex < mesh.FaceSets.size(); ++faceSetIndex)
	{
		const Flver::FaceSet& faceSet = mesh.FaceSets[faceSetIndex];

		if (faceSet.Flags != (int)Flver::FaceSet::FaceSetFlags::None)
		{
			// skip all LODs
			continue;
		}

		node->mCullingType = faceSet.CullBackfaces ? FbxNode::eCullingOnCCW : FbxNode::eCullingOff;

		assert(faceSet.Indices.size() % 3 == 0);

		for (int triangleStartIndex = 0; triangleStartIndex < faceSet.Indices.size(); triangleStartIndex+=3)
		{
			fbx->BeginPolygon();

			fbx->AddPolygon(faceSet.Indices[triangleStartIndex]);
			fbx->AddPolygon(faceSet.Indices[triangleStartIndex + 1]);
			fbx->AddPolygon(faceSet.Indices[triangleStartIndex + 2]);

			fbx->EndPolygon();
		}
	}

	fbx->BuildMeshEdgeArray();

	FbxGeometryConverter Converter(scene->GetFbxManager());

	Converter.ComputeEdgeSmoothingFromNormals(fbx);
	Converter.ComputePolygonSmoothingFromEdgeSmoothing(fbx);

	return ParseMesh(fbx, node, mesh);
}

static FbxSkeleton* exportBone(FbxScene* scene, const Flver::Bone& bone)
{
	FbxSkeleton* fbx = FbxSkeleton::Create(scene, (bone.Name + "_Bone").c_str());
/*
	if (bone.ParentIndex < 0)
	{
		fbx->SetSkeletonType(FbxSkeleton::EType::eRoot);
	}
	else */if (bone.ChildIndex < 0)
	{
		fbx->SetSkeletonType(FbxSkeleton::EType::eEffector);
	}
	else
	{
		fbx->SetSkeletonType(FbxSkeleton::EType::eLimb);
	}

	return fbx;
}

struct HkxBone
{
	int flverIndex;
	int ParentIndex;
	FbxNode* node;
	Hkx::Bone bone;
	Hkx::Transform transform;
};

void RecurseDeep(ParseBone& currentBone, std::map<int, ParseBone>& allBones);

std::vector<int> GetSiblingIndices(const int nodeIndex, const std::map<int, ParseBone>& allBones)
{
	std::vector<int> siblingIndices;

	for (int currentSiblingIndex = nodeIndex; currentSiblingIndex >= 0; currentSiblingIndex = allBones.at(currentSiblingIndex).raw.NextSiblingIndex)
	{
		siblingIndices.push_back(currentSiblingIndex);
	}

	return siblingIndices;
}

void RecurseWide(ParseBone& currentBone, std::map<int, ParseBone>& allBones)
{
	RecurseDeep(currentBone, allBones);

	if (currentBone.raw.NextSiblingIndex < 0)
	{
		return;
	}


	const short NextSiblingIndex = currentBone.raw.NextSiblingIndex;
	ParseBone& SiblingBone = allBones[NextSiblingIndex];

	std::cout << "Now on " << currentBone.raw.Name << ". Going wide to " << SiblingBone.raw.Name << std::endl;

	RecurseWide(SiblingBone, allBones);
}

void RecurseDeep(ParseBone& currentBone, std::map<int, ParseBone>& allBones)
{
	if (currentBone.raw.ChildIndex < 0)
	{
		return;
	}

	const short nextChildIndex = currentBone.raw.ChildIndex;
	
	for (const int childIndex : GetSiblingIndices(nextChildIndex, allBones))
	{
		ParseBone& childBone = allBones[childIndex];

		std::cout << "Visited " << currentBone.raw.Name << ". It is parent to " << childBone.raw.Name << std::endl;

		//constexpr float RadianToDegree = 180.0f / 3.14f;

		const Vector3 rotationRadian = childBone.raw.Rotation;

		//const Vector3 rotationDegrees(rotationRadian[0] * RadianToDegree, rotationRadian[1] * RadianToDegree, rotationRadian[2] * RadianToDegree);

		//const Vector3& rotationToSet = Vector3(-rotationRadian[0], -rotationRadian[1], -rotationRadian[2]);

		currentBone.node->AddChild(childBone.node);

		RecurseDeep(childBone, allBones);
	}
}

FbxMatrix GetWorldTransform(const ParseBone& bone, const std::map<int, ParseBone>& allBones)
{
	ParseBone currentBone = bone;

	Matrix4x4 Result;

	do
	{
		constexpr double Pi = 3.14159265358979323846;

		FbxVector4 translation = Convert(currentBone.raw.Translation);
		FbxVector4 euler = Convert(currentBone.raw.Rotation) * 180.0 / Pi;
		FbxVector4 scale = Convert(currentBone.raw.Scale);

		const static FbxVector4 Empty(0, 0, 0);
		const static FbxVector4 NoneScale(1,1,1);

		Matrix4x4 scaleM;
		gmtl::setScale(scaleM, currentBone.raw.Scale);

		Matrix4x4 rotXM;
		gmtl::setRot(rotXM, gmtl::EulerAngleXYZd(currentBone.raw.Rotation[0], 0, 0));
		Matrix4x4 rotYM;
		gmtl::setRot(rotYM, gmtl::EulerAngleXYZd(0, currentBone.raw.Rotation[1], 0));
		Matrix4x4 rotZM;
		gmtl::setRot(rotZM, gmtl::EulerAngleXYZd(0, 0, currentBone.raw.Rotation[2]));

		Matrix4x4 translateM;
		gmtl::setTrans(translateM, currentBone.raw.Translation);

		const Matrix4x4 matrices[] = {
			scaleM,
			rotXM,
			rotZM,
			rotYM,
			translateM,
		};

		Matrix4x4 scaleRes = scaleM * Result;
		Matrix4x4 rotXRes = rotXM* scaleRes;
		Matrix4x4 rotZRes = rotZM* rotXRes;

		Matrix4x4 rotYRes = rotYM* rotZRes ;
		Matrix4x4 transRes = translateM* rotYRes ;

		Result = transRes;

		if (currentBone.raw.ParentIndex < 0)
		{
			break;
		}


		currentBone = allBones.at(currentBone.raw.ParentIndex);
	} while (true);

	return FbxMatrix(
		Result[0][0], Result[0][1], Result[0][2], Result[0][3],
		Result[1][0], Result[1][1], Result[1][2], Result[1][3],
		Result[2][0], Result[2][1], Result[2][2], Result[2][3],
		Result[3][0], Result[3][1], Result[3][2], Result[3][3]
	).Transpose();

	//return Result;
}

FbxNode* ProcessBoneHierarchy(FbxScene* scene, std::map<int, ParseBone>& skeletonBones)
{
	FbxNode* SkeletonRootNode = FbxNode::Create(scene, "ActualRoot_Node");

	FbxSkeleton* skel = FbxSkeleton::Create(scene, "ActualRoot_Skel");

	skel->SetSkeletonType(FbxSkeleton::EType::eRoot);

	SkeletonRootNode->SetNodeAttribute(skel);

	for (std::pair<const int, ParseBone>& bone : skeletonBones)
	{
		if (bone.second.raw.ParentIndex < 0)
		{
			std::cout << "Entering bone " << bone.second.raw.Name << std::endl;

			RecurseDeep(bone.second, skeletonBones);

			SkeletonRootNode->AddChild(bone.second.node);
		}
	}

	for (std::pair<const int, ParseBone>& bonePair : skeletonBones)
	{
		ParseBone& bone = bonePair.second;

		FbxMatrix boneMatrix = GetWorldTransform(bone, skeletonBones);

		if (bone.raw.ParentIndex >= 0)
		{
			const FbxMatrix parentMatrix = GetWorldTransform(skeletonBones[bone.raw.ParentIndex], skeletonBones);
			boneMatrix = parentMatrix.Inverse() * boneMatrix;
		}

		FbxVector4 Location, Rotation, Scale, ShearingDummy;
		double signDummy;
		
		boneMatrix.GetElements(Location, Rotation, ShearingDummy, Scale, signDummy);
		
		Location[0] = Location[0];

		bone.node->LclTranslation.Set(Location);
		bone.node->LclRotation.Set(Rotation);
		bone.node->LclScaling.Set(Scale);
	}


	
	return SkeletonRootNode;
}

void ProcessSkin(FbxScene* scene, std::map<int, ParseBone>& skeletonBones, std::map<int, ParseMesh>& meshes)
{
	for (std::pair<const int, ParseMesh>& meshPair : meshes)
	{
		const int meshIndex = meshPair.first;
		ParseMesh& mesh = meshPair.second;
		FbxSkin* skin = FbxSkin::Create(mesh.object, "");

		std::cout << "Gathering bound vertices for mesh " << meshIndex << std::endl;

		std::map<int, int> vertexBoneIndicesCount;

		for (std::pair<const int, ParseBone>& bonePair : skeletonBones)
		{
			const int currentBoneIndex = bonePair.first;
			ParseBone& bone = bonePair.second;

			FbxCluster* cluster = FbxCluster::Create(bone.object, "");
			
			cluster->SetLink(bone.node);
			
			struct VertexWeightPair
			{
				int vertexIndex;
				float weight;
			};

			std::vector<VertexWeightPair> vertexWeights;

			for (int vertexIndex = 0; vertexIndex < mesh.raw.Vertices.size(); ++vertexIndex)
			{
				const Flver::Vertex& vertex = mesh.raw.Vertices[vertexIndex];

				if (vertex.UsesBoneIndices && vertex.UsesBoneWeights)
				{
					struct VertexWeightIndexBind
					{
						int Flver::VertexBoneIndices::* indexPtr;
						float Flver::VertexBoneWeights::* weightPtr;
					};

					static constexpr VertexWeightIndexBind Bind[] 
					{
						{ &Flver::VertexBoneIndices::A, &Flver::VertexBoneWeights::A},
						{ &Flver::VertexBoneIndices::B, &Flver::VertexBoneWeights::B},
						{ &Flver::VertexBoneIndices::C, &Flver::VertexBoneWeights::C},
						{ &Flver::VertexBoneIndices::D, &Flver::VertexBoneWeights::D},
					};

					for (int vertexBindIndex = 0; vertexBindIndex < 4; ++vertexBindIndex)
					{
						const VertexWeightIndexBind& CurrentBind = Bind[vertexBindIndex];

						int boneIndex = vertex.BoneIndices.*CurrentBind.indexPtr;
						float weight = vertex.BoneWeights.*CurrentBind.weightPtr;

						if (weight < 0.0001 || boneIndex != currentBoneIndex)
						{
							continue;
						}

						vertexBoneIndicesCount[vertexIndex]++;

						vertexWeights.push_back({ vertexIndex, weight });
					}
				}
			}
		
			// std::cout << "Gathered " << vertexWeights.size() << " bound vertices for bone " << bone.raw.Name << std::endl;

			cluster->SetControlPointIWCount(vertexWeights.size());
			
			for (const VertexWeightPair& vertexWeight : vertexWeights)
			{
				cluster->AddControlPointIndex(vertexWeight.vertexIndex, vertexWeight.weight);
			}

			skin->AddCluster(cluster);
		}

		for (const std::pair<const int, int>& vertexBoneIndicesC : vertexBoneIndicesCount)
		{
			if (vertexBoneIndicesC.second == 0)
			{
				std::cout << "No bones assigned to vertex " << vertexBoneIndicesC.first << std::endl;
			}
			else if (vertexBoneIndicesC.second > 4)
			{
				std::cout << "Too much bone indices for vertex " << vertexBoneIndicesC.first << "(" << vertexBoneIndicesC.second << ")" << std::endl;
			}
		}

		mesh.object->AddDeformer(skin);
	}
}

void ProcessBindPose(FbxScene* scene, std::map<int, ParseBone>& skeletonBones)
{
	std::cout << "Generating bind poses..." << std::endl;

	//for (std::pair<const int, ParseMesh>& meshPair : meshes)
	{
		//std::cout << "Generating bind poses for mesh " << meshPair.first << std::endl;
		std::cout << "Generating bind pose..." << std::endl;

		//ParseMesh& mesh = meshPair.second;
		
		FbxPose* pose = FbxPose::Create(scene, "");

		pose->SetIsBindPose(true);

		for (std::pair<const int, ParseBone>& bonePair : skeletonBones)
		{
			ParseBone& bone = bonePair.second;

			const FbxMatrix bindMatrix = bone.node->EvaluateGlobalTransform();
			pose->Add(bone.node, bindMatrix);

			std::cout << "Some values for bone " << bone.raw.Name << " are " << bindMatrix.Get(0, 0) << " " << bindMatrix.Get(0, 1) << " " << bindMatrix.Get(0, 2) << " " << bindMatrix.Get(0, 3) << " " << std::endl;
		}

		scene->AddPose(pose);
	}
}

std::map<int, HkxBone> PrepareAnimBones(std::map<int, ParseBone>& bones)
{
	std::cout << "Reading hkx skel..." << std::endl;

	nlohmann::json parsed;
	{
		std::ifstream stream = std::ifstream("hkx_skel.json");
		stream >> parsed;
	}

	std::cout << "Read hkx skel." << std::endl;

	Hkx::HkaSkeleton skel;

	parsed.get_to(skel);

	std::cout << "Parsed skel" << std::endl;

	std::map<int, HkxBone> hkxBones;

	for (int hkxBoneIndex = 0; hkxBoneIndex < skel.Bones.size(); ++hkxBoneIndex)
	{
		const Hkx::Bone& bone = skel.Bones[hkxBoneIndex];

		auto flverBone = std::find_if(bones.begin(), bones.end(), [hkxBoneName = bone.Name](const std::pair<const int, ParseBone>& iter) -> bool { return iter.second.raw.Name == hkxBoneName; });

		assert(flverBone != bones.end());

		const short boneParent = skel.ParentIndices[hkxBoneIndex];
		const Hkx::Transform& transform = skel.Transforms[hkxBoneIndex];

		HkxBone hkxBone;
	
		hkxBone.bone = bone;

		hkxBone.ParentIndex = boneParent;
		hkxBone.transform = transform;

		hkxBone.flverIndex = flverBone->first;
		hkxBone.node = flverBone->second.node;

		hkxBones.emplace(hkxBoneIndex, hkxBone);

		std::cout << "Parsed HKX bone " << hkxBone.bone.Name << ", found its FLVER index " << hkxBone.flverIndex << std::endl;
	}

	return hkxBones;
}


struct FbxAnimNodes
{
	struct FbxAnimNodeCurve
	{
		FbxAnimNodeCurve(FbxAnimLayer* animLayer, FbxPropertyT<FbxDouble3>& in_node) : node(in_node.GetCurveNode(animLayer))
		{
			curveX = in_node.GetCurve(animLayer, FBXSDK_CURVENODE_COMPONENT_X, /*pCreate =*/ true);
			assert(curveX != nullptr);
			curveY = in_node.GetCurve(animLayer, FBXSDK_CURVENODE_COMPONENT_Y, /*pCreate =*/ true);
			assert(curveY != nullptr);
			curveZ = in_node.GetCurve(animLayer, FBXSDK_CURVENODE_COMPONENT_Z, /*pCreate =*/ true);
			assert(curveZ != nullptr);

			curveX->KeyModifyBegin();
			curveY->KeyModifyBegin();
			curveZ->KeyModifyBegin();
		}

		void AddPoint(const FbxTime& time, const FbxVector4& value)
		{
			FbxAnimCurveKey xKey(time, value[0]);
			curveX->KeyAdd(time, xKey);
			FbxAnimCurveKey yKey(time, value[1]);
			curveY->KeyAdd(time, yKey);
			FbxAnimCurveKey zKey(time, value[2]);
			curveZ->KeyAdd(time, zKey);
		}

		void Finish()
		{
			if (curveX != nullptr)
			{
				curveX->KeyModifyEnd();
				curveX = nullptr;
			}

			if (curveY != nullptr)
			{
				curveY->KeyModifyEnd();
				curveY = nullptr;
			}

			if (curveZ != nullptr)
			{
				curveZ->KeyModifyEnd();
				curveZ = nullptr;
			}
		}

		FbxAnimCurveNode* node = nullptr;

		FbxAnimCurve* curveX = nullptr;
		FbxAnimCurve* curveY = nullptr;
		FbxAnimCurve* curveZ = nullptr;
	};

	FbxAnimNodes(FbxAnimLayer* animLayer, FbxPropertyT<FbxDouble3>& translationNode, FbxPropertyT<FbxDouble3>&  rotationNode, FbxPropertyT<FbxDouble3>&  scaleNode) : translation(animLayer, translationNode), rotation(animLayer, rotationNode), scale(animLayer, scaleNode) {}

	FbxAnimNodeCurve translation;
	FbxAnimNodeCurve rotation;
	FbxAnimNodeCurve scale;
};


enum class VectorPart { X, Y, Z, };
enum class TransformPart { Position, Scale, };

template <VectorPart part>
struct VectorPartHelper
{
};

template <>
struct VectorPartHelper<VectorPart::X>
{
	static constexpr Hkx::FlagOffset StaticOffsetFlag = Hkx::FlagOffset::StaticX;

	static constexpr FbxAnimCurve* FbxAnimNodes::FbxAnimNodeCurve::* AnimNodeCurvePtr = &FbxAnimNodes::FbxAnimNodeCurve::curveX;

	static float GetValue(const Vector3& Value) 
	{
		return Value[0];
	}

	static const Hkx::SplineChannel<float>& GetChannel(const Hkx::SplineTrackVector3& Spline)
	{
		return Spline.ChannelX;
	}
};

template <>
struct VectorPartHelper<VectorPart::Y>
{
	static constexpr Hkx::FlagOffset StaticOffsetFlag = Hkx::FlagOffset::StaticY;
	static constexpr FbxAnimCurve* FbxAnimNodes::FbxAnimNodeCurve::* AnimNodeCurvePtr = &FbxAnimNodes::FbxAnimNodeCurve::curveY;

	static float GetValue(const Vector3& Value)
	{
		return Value[1];
	}

	static const Hkx::SplineChannel<float>& GetChannel(const Hkx::SplineTrackVector3& Spline)
	{
		return Spline.ChannelY;
	}
};

template <>
struct VectorPartHelper<VectorPart::Z>
{
	static constexpr Hkx::FlagOffset StaticOffsetFlag = Hkx::FlagOffset::StaticZ;
	static constexpr FbxAnimCurve* FbxAnimNodes::FbxAnimNodeCurve::* AnimNodeCurvePtr = &FbxAnimNodes::FbxAnimNodeCurve::curveZ;

	static float GetValue(const Vector3& Value)
	{
		return Value[2];
	}

	static const Hkx::SplineChannel<float>& GetChannel(const Hkx::SplineTrackVector3& Spline)
	{
		return Spline.ChannelZ;
	}
};

template <TransformPart part>
struct TransformPartHelper
{
};

template <>
struct TransformPartHelper<TransformPart::Position>
{
	static constexpr FbxAnimNodes::FbxAnimNodeCurve FbxAnimNodes::* AnimNodeCurvePtr = &FbxAnimNodes::translation;

	static Vector3 GetStaticOffset(const Hkx::HkxTrack& track)
	{
		return track.StaticPosition;
	}

	static Vector3 GetTransform(const Hkx::Transform& transform)
	{
		return Vector3(transform.Position[0], transform.Position[1], transform.Position[2]);
	}

	static const Hkx::SplineTrackVector3& GetSpline(const Hkx::HkxTrack& Track)
	{
		return Track.SplinePosition;
	}

	static bool HasSpline(const Hkx::HkxTrack& Track)
	{
		return Track.HasSplinePosition;
	}

	static const std::vector<Hkx::FlagOffset>& GetFlags(const Hkx::HkxTrack& Track)
	{
		return Track.Mask.PositionTypes;
	}
};

template <>
struct TransformPartHelper<TransformPart::Scale>
{
	static constexpr FbxAnimNodes::FbxAnimNodeCurve FbxAnimNodes::* AnimNodeCurvePtr = &FbxAnimNodes::scale;

	static Vector3 GetStaticOffset(const Hkx::HkxTrack& track)
	{
		return track.StaticScale;
	}

	static Vector3 GetTransform(const Hkx::Transform& transform)
	{
		return Vector3(transform.Scale[0], transform.Scale[1], transform.Scale[2]);
	}

	static const Hkx::SplineTrackVector3& GetSpline(const Hkx::HkxTrack& Track)
	{
		return Track.SplineScale;
	}

	static bool HasSpline(const Hkx::HkxTrack& Track)
	{
		return Track.HasSplineScale;
	}

	static const std::vector<Hkx::FlagOffset>& GetFlags(const Hkx::HkxTrack& Track)
	{
		return Track.Mask.ScaleTypes;
	}
};

template <class T>
struct NurbsDefaultValueHelper
{

};

template<>
struct NurbsDefaultValueHelper<float>
{
	constexpr static float Value = 0.0f;
};

template<>
struct NurbsDefaultValueHelper<Quaternion>
{
	static const Quaternion Value;
};

const Quaternion NurbsDefaultValueHelper<Quaternion>::Value = Quaternion(0, 0, 0, 0);

template <class T, bool CombineByAddition>
struct NurbsHelper
{
	static int FindKnotSpan(int degree, float value, int pointsCount, const std::vector<uint8>& knots)
	{
		if (value >= knots[pointsCount])
		{
			return pointsCount - 1;
		}

		int low = degree;
		int high = pointsCount;
		int mid = (low + high) / 2;

		while (value < knots[mid] || value >= knots[mid + 1])
		{
			if (value < knots[mid])
			{
				high = mid;
			}
			else
			{
				low = mid;
			}

			mid = (low + high) / 2;
		}

		return mid;
	}
	static T Evaluate(int degree, float frame, const std::vector<uint8>& knots, const std::vector<T>& controlPoints)
	{
		return Evaluate(FindKnotSpan(degree, frame, controlPoints.size(), knots), degree, frame, knots, controlPoints);
	}

	static T Evaluate(int knotSpan, int degree, float frame, const std::vector<uint8>& knots, const std::vector<T>& controlPoints)
	{
		const size_t controlPointsCount = controlPoints.size();

		if (controlPointsCount == 1)
		{
			return controlPoints[0];
		}

		const int span = knotSpan;

		double N[] = { 1.0f, 0, 0, 0, 0 };

		for (int i = 1; i <= degree; i++)
		{
			for (int j = i - 1; j >= 0; j--)
			{
				float A = (frame - knots[span - j]) / (knots[span + i - j] - knots[span - j]);
				// without multiplying A, model jitters slightly
				float tmp = N[j] * A;
				// without subtracting tmp, model flies away then resets to origin every few frames
				N[j + 1] += N[j] - tmp;
				// without setting to tmp, model either is moved from origin or grows very long limbs
				// depending on the animation
				N[j] = tmp;
			}
		}

		T retVal = NurbsDefaultValueHelper<T>::Value;

		for (int i = 0; i <= degree; i++)
		{
			const T& combineWith = controlPoints[span - i] * N[i];
			
			if (CombineByAddition)
			{
				retVal += combineWith;
			}
			else
			{
				retVal *= combineWith;
			}
		}

		return retVal;
	}
};

template <TransformPart transformPart, VectorPart vectorPart>
struct VectorSplineHelper
{
	typedef TransformPartHelper<transformPart> TransformHelper;
	typedef VectorPartHelper<vectorPart> VectorHelper;

	static int FindKnotSpan(int degree, float value, int pointsCount, const std::vector<uint8>& knots)
	{
		return NurbsHelper::FindKnotSpan(degree, value, pointsCount, knots);
	}

	static float Evaluate(const Hkx::HkxTrack& track, const Hkx::Transform& transform, int Frame)
	{
		if (TransformHelper::HasSpline(track))
		{
			const Hkx::SplineTrackVector3& spline = TransformHelper::GetSpline(track);

			const Hkx::SplineChannel<float>& channel = VectorHelper::GetChannel(spline);

			if (channel.is_actually_present)
			{
				const std::vector<float> controlPoints = channel.Values;

				return NurbsHelper<float, true>::Evaluate(spline.Degree, Frame, spline.Knots, controlPoints);
			}
		}
		else
		{
			const std::vector<Hkx::FlagOffset>& flags = TransformHelper::GetFlags(track);

			if (std::find(flags.cbegin(), flags.cend(), VectorHelper::StaticOffsetFlag) != flags.cend())
			{
				return static_cast<float>( VectorHelper::GetValue(TransformHelper::GetStaticOffset(track)));
			}
		}

		return VectorHelper::GetValue(TransformHelper::GetTransform(transform));
	}

	static void ProcessFbxBones(FbxAnimNodes& nodes, const FbxTime& time, const Hkx::HkxTrack& track, const Hkx::Transform& transform, int frame)
	{
		FbxAnimNodes::FbxAnimNodeCurve& nodeCurve = nodes.*TransformHelper::AnimNodeCurvePtr;

		FbxAnimCurve* fbxCurve = nodeCurve.*VectorHelper::AnimNodeCurvePtr;

		const float evaluated = Evaluate(track, transform, frame);

		FbxAnimCurveKey key(time, evaluated);
		fbxCurve->KeyAdd(time, key);
	}
};

FbxMatrix Convert(const Matrix4x4& matrix)
{
		FbxMatrix fbxM(
			matrix(0, 0), matrix(0, 1), matrix(0, 2), matrix(0, 3),
			matrix(1, 0), matrix(1, 1), matrix(1, 2), matrix(1, 3),
			matrix(2, 0), matrix(2, 1), matrix(2, 2), matrix(2, 3),
			matrix(3, 0), matrix(3, 1), matrix(3, 2), matrix(3, 3)
		);

		return fbxM;
}


void ParseAnimations(FbxScene* scene, std::map<int, HkxBone>& animBones, const std::vector<std::string>& animationNames, FbxNode* rootBone)
{
	std::cout << "Reading hkx animations..." << std::endl;

	nlohmann::json parsed;
	{
		std::ifstream stream = std::ifstream("hkx_anims.json");
		stream >> parsed;
	}

	std::cout << "Read hkx anims." << std::endl;

	std::map<std::string, Hkx::HkxAnim> animFile;


	if (animationNames.size() > 0)
	{
		for (const std::string& animName : animationNames)
		{
			animFile.emplace(animName, parsed.at(animName).get<Hkx::HkxAnim>());
		}
	}
	else
	{
		parsed.get_to(animFile);
		std::cout << "Will export all " << animFile.size() << " animations" << std::endl;
	}

	Hkx::HkxAnim anim11;

	std::cout << "Parsed hkx anims." << std::endl;

	
	for (const std::pair<std::string, Hkx::HkxAnim>& animPair : animFile)
	{
		const std::string& animName = animPair.first;
		const Hkx::HkxAnim& anim = animPair.second;

		std::cout << "Exporting animation " << animName << std::endl;

		FbxAnimStack* animStack = FbxAnimStack::Create(scene, animName.c_str());

		FbxAnimLayer* animLayer = FbxAnimLayer::Create(animStack, "Layer0");

		animStack->AddMember(animLayer);

		std::map<int, FbxAnimNodes> nodes;

		for (std::pair<const int, HkxBone>& bonePair : animBones)
		{
			const int boneIndex = bonePair.first;
			HkxBone& bone = bonePair.second;

			bone.node->LclTranslation.GetCurveNode(animLayer, /*bCreate =*/ true);
			bone.node->LclRotation.GetCurveNode(animLayer, /*bCreate =*/ true);
			bone.node->LclScaling.GetCurveNode(animLayer, /*bCreate =*/ true);

			nodes.emplace(boneIndex, FbxAnimNodes(animLayer, bone.node->LclTranslation, bone.node->LclRotation, bone.node->LclScaling));
		}

		rootBone->LclTranslation.GetCurveNode(animLayer, true);
		rootBone->LclRotation.GetCurveNode(animLayer, true);
		rootBone->LclScaling.GetCurveNode(animLayer, true);

		FbxAnimNodes rootAnimNode(animLayer, rootBone->LclTranslation, rootBone->LclRotation, rootBone->LclScaling);

		for (int frameIndex = 0; frameIndex < anim.FrameCount; ++frameIndex)
		{
			FbxTime time;
			time.SetFrame(frameIndex);

			const int frameInBlockIndex = frameIndex % anim.NumFramesPerBlock;

			const int blockIndex = static_cast<int>(std::floor(((double)(frameInBlockIndex)) / anim.NumFramesPerBlock));

			//std::cout << "time is " << frameIndex << std::endl;

			std::map<int, Matrix4x4> localBoneTransforms;

			for (std::pair<const int, HkxBone>& bonePair : animBones)
			{
				const int boneIndex = bonePair.first;
				HkxBone& bone = bonePair.second;
				//FbxAnimNodes& node = nodes.at(boneIndex);


				if (frameIndex == 1)
				{
					//std::cout << "frame 1!";
				}

				const int trackIndex = anim.HkxBoneIndexToTransformTrackMap[boneIndex];

				assert(trackIndex >= 0);

				const Hkx::HkxTrack& track = anim.Tracks[blockIndex][trackIndex];

					//VectorSplineHelper<TransformPart::Position, VectorPart::X>::ProcessFbxBones(node, time, track, bone.transform, frameInBlockIndex);
					//VectorSplineHelper<TransformPart::Position, VectorPart::Y>::ProcessFbxBones(node, time, track, bone.transform, frameInBlockIndex);
					//VectorSplineHelper<TransformPart::Position, VectorPart::Z>::ProcessFbxBones(node, time, track, bone.transform, frameInBlockIndex);

					//VectorSplineHelper<TransformPart::Scale, VectorPart::X>::ProcessFbxBones(node, time, track, bone.transform, frameInBlockIndex);
					//VectorSplineHelper<TransformPart::Scale, VectorPart::Y>::ProcessFbxBones(node, time, track, bone.transform, frameInBlockIndex);
					//VectorSplineHelper<TransformPart::Scale, VectorPart::Z>::ProcessFbxBones(node, time, track, bone.transform, frameInBlockIndex);


				auto posX = VectorSplineHelper<TransformPart::Position, VectorPart::X>::Evaluate(track, bone.transform, frameInBlockIndex);
				auto posY = VectorSplineHelper<TransformPart::Position, VectorPart::Y>::Evaluate(track, bone.transform, frameInBlockIndex);
				auto posZ = VectorSplineHelper<TransformPart::Position, VectorPart::Z>::Evaluate(track, bone.transform, frameInBlockIndex);

				auto scaleX = VectorSplineHelper<TransformPart::Scale, VectorPart::X>::Evaluate(track, bone.transform, frameInBlockIndex);
				auto scaleY = VectorSplineHelper<TransformPart::Scale, VectorPart::Y>::Evaluate(track, bone.transform, frameInBlockIndex);
				auto scaleZ = VectorSplineHelper<TransformPart::Scale, VectorPart::Z>::Evaluate(track, bone.transform, frameInBlockIndex);

				Matrix4x4 translate;
				gmtl::setTrans(translate, Vector3(posX, posY, posZ));
				Matrix4x4 scale;
				gmtl::setScale(scale, Vector3(scaleX, scaleY, scaleZ));

				Quaternion rotationToSet;

				if (track.HasSplineRotation)
				{
					const Hkx::SplineTrackQuaternion& rotationTrack = track.SplineRotation;

					rotationToSet = NurbsHelper<Quaternion, true>::Evaluate(rotationTrack.Degree, frameIndex, rotationTrack.Knots, track.SplineRotation.Channel.Values);
					//Quaternion rotY;
					//gmtl::set(rotY, gmtl::EulerAngleXYZd(0, 3.14, 0));
					//rotationToSet *= rotY;
				}
				else if (track.HasStaticRotation)
				{
					rotationToSet = track.StaticRotation;
				}
				else
				{
					rotationToSet = bone.transform.Rotation;
				}


				Matrix4x4 rot;
				gmtl::setRot(rot, rotationToSet);

				Matrix4x4 finalTransform = translate * rot *scale;// * rot;

				gmtl::transpose(finalTransform);
/*
				std::cout << "for " << bone.bone.Name << " transform is " <<
					finalTransform(0, 0) << " " << finalTransform(0, 1) << " " << finalTransform(0, 2) << " " << finalTransform(0, 3) << " " <<
					finalTransform(1, 0)<< " " << finalTransform(1, 1) << " "<< finalTransform(1, 2) << " "<< finalTransform(1, 3) << " " <<
					finalTransform(2, 0)<< " " << finalTransform(2, 1) << " "<< finalTransform(2, 2) << " "<< finalTransform(2, 3) << " " <<
					finalTransform(3, 0)<< " " << finalTransform(3, 1) << " "<< finalTransform(3, 2) << " "<< finalTransform(3, 3) << std::endl;*/

				localBoneTransforms.emplace(boneIndex, finalTransform);

				//FbxMatrix fbxM(
				//	finalTransform(0, 0), finalTransform(0, 1), finalTransform(0, 2), finalTransform(0, 3),
				//	finalTransform(1, 0), finalTransform(1, 1), finalTransform(1, 2), finalTransform(1, 3),
				//	finalTransform(2, 0), finalTransform(2, 1), finalTransform(2, 2), finalTransform(2, 3),
				//	finalTransform(3, 0), finalTransform(3, 1), finalTransform(3, 2), finalTransform(3, 3)
				//);

				//FbxVector4 translateV, scaleV, rotateV, shearingDummyV;
				//double signDummy;
				//fbxM/*.Transpose()*/.GetElements(translateV, rotateV, shearingDummyV, scaleV, signDummy);

				//node.translation.AddPoint(time, translateV);
				//node.rotation.AddPoint(time, rotateV);
				//node.scale.AddPoint(time, scaleV);
			}
		
			std::map<int, Matrix4x4> globalBoneTransforms;


			for (std::pair<const int, HkxBone>& bonePair : animBones)
			{
				const int boneIndex = bonePair.first;

				Matrix4x4 globalBoneTransform;

				for (int parentIndex = boneIndex; parentIndex >= 0; parentIndex = animBones[parentIndex].ParentIndex)
				{
					globalBoneTransform = globalBoneTransform * localBoneTransforms[parentIndex];
				}

				Matrix4x4 rotateY;

				gmtl::set(rotateY, gmtl::EulerAngleXYZd(0, 3.14, 0));

				globalBoneTransform = rotateY * globalBoneTransform;

				Vector3 globalBoneTranslation;
				gmtl::setTrans(globalBoneTranslation, globalBoneTransform);

				globalBoneTranslation[0] = -globalBoneTranslation[0];

				gmtl::setTrans(globalBoneTransform, globalBoneTranslation);

				

				globalBoneTransforms.emplace(boneIndex, globalBoneTransform);


				//if (frameIndex == 0)
				//std::cout << "for " << bone.bone.Name << " transform is " <<
				//	globalBoneTransform(0, 0) << " " << globalBoneTransform(0, 1) << " " << globalBoneTransform(0, 2) << " " << globalBoneTransform(0, 3) << " " <<
				//	globalBoneTransform(1, 0)<< " " << globalBoneTransform(1, 1) << " "<< globalBoneTransform(1, 2) << " "<< globalBoneTransform(1, 3) << " " <<
				//	globalBoneTransform(2, 0)<< " " << globalBoneTransform(2, 1) << " "<< globalBoneTransform(2, 2) << " "<< globalBoneTransform(2, 3) << " " <<
				//	globalBoneTransform(3, 0)<< " " << globalBoneTransform(3, 1) << " "<< globalBoneTransform(3, 2) << " "<< globalBoneTransform(3, 3) << std::endl;
			}

			for (std::pair<const int, HkxBone>& bonePair : animBones)
			{
				const int boneIndex = bonePair.first;
				HkxBone& bone = bonePair.second;

				Matrix4x4 finalBoneTransform = globalBoneTransforms[boneIndex];

				FbxMatrix finalFbxBoneTransform = Convert(finalBoneTransform);// .Transpose();

				if (bone.ParentIndex >= 0)
				{
					FbxMatrix parentFbxBoneTransform = Convert(globalBoneTransforms[bone.ParentIndex]);// .Transpose();

					finalFbxBoneTransform = parentFbxBoneTransform.Inverse() * finalFbxBoneTransform;
				}



				FbxVector4 translateV, scaleV, rotateV, shearingDummyV;
				double signDummy;
				finalFbxBoneTransform/*.Transpose()*/.GetElements(translateV, rotateV, shearingDummyV, scaleV, signDummy);

				FbxAnimNodes& node = nodes.at(boneIndex);

				//FbxVector4 translateVgen(frameIndex,0, 0);

				//node.translation.AddPoint(time, translateVgen);
				//node.rotation.AddPoint(time, FbxVector4(0, 0, 0));

				//node.rotation.AddPoint(time, FbxVector4(0, frameIndex, 0));
				//node.scale.AddPoint(time, FbxVector4(1, 1, 1));

				node.translation.AddPoint(time, translateV);
				node.rotation.AddPoint(time, rotateV);
				node.scale.AddPoint(time, scaleV);

				if (bone.bone.Name == "Root")
				{
					rootAnimNode.translation.AddPoint(time, translateV);
					rootAnimNode.rotation.AddPoint(time, rotateV);
					rootAnimNode.scale.AddPoint(time, scaleV);
				}
			}
		}
		
		for (std::pair<const int, FbxAnimNodes>& nnode : nodes)
		{
			nnode.second.translation.Finish();
			nnode.second.rotation.Finish();
			nnode.second.scale.Finish();
		}

		rootAnimNode.translation.Finish();
		rootAnimNode.rotation.Finish();
		rootAnimNode.scale.Finish();

		std::cout << "Exported!" << std::endl << std::endl;
	}
}

int main(int argc, char* argv[])
{
	args::ArgumentParser parser("This is a DS to FBX model converter");
	args::Flag shouldGenerateSkins(parser, "skin", "Should generate skins", { "skin", 's' });
	args::Flag shouldGenerateMesh(parser, "mesh", "Should generate mesh", { "mesh", 'm' });
	args::Flag shouldGenerateBones(parser, "bones", "Should generate bones", { "bones", 'b' });
	args::Flag shouldExportAnimations(parser, "animations", "Should export animations", {"animations", "anims", 'a'});
	args::ValueFlagList<std::string> animationNames(parser, "animation list", "Which animations to export?", {"animlist", "al"});
	args::Flag shouldGenerateBindPose(parser, "bindpose", "Should calculate bind pose", {"bindpose", "bpose", "bp"});
	args::ValueFlag<std::string> fbxExport(parser, "FBX export", "Is exporting required", { "export", 'e' }, "out.fbx");
	
	parser.Add(shouldGenerateSkins);
	parser.Add(shouldExportAnimations);
	parser.Add(shouldGenerateBindPose);
	parser.Add(shouldGenerateMesh);
	parser.Add(shouldGenerateBones);
	parser.Add(animationNames);
	parser.Add(fbxExport);

	try
	{
		parser.ParseArgs(std::vector<std::string>(argv + 1, argv + argc));
		std::cout << "Input args parsed" << std::endl;
	}
	catch (args::Error e)
	{
		std::cout << "An error occured while parsing args. " << e.what() << " Exiting..." << std::endl;
		
		return 1;
	}

	const std::vector<args::FlagBase*> flags = parser.GetAllFlags();
	if (!std::any_of(flags.cbegin(), flags.cend(), [](const args::FlagBase* flag)-> bool { return flag->Matched(); }))
	{
		std::cout << "No input flags were passed" << std::endl;
		return 1;
	}

	std::cout << "Reading flver..." << std::endl;
	nlohmann::json parsed;
	{
		std::ifstream stream = std::ifstream("flver.json");
		stream >> parsed;
	}

	std::cout << "Read flver." << std::endl;
	
	Flver::Flver f;
	parsed.get_to(f);

	std::cout << "Input converted to Flver struct" << std::endl;

	FbxManager* manager = FbxManager::Create();

	FbxScene* scene = FbxScene::Create(manager, "");


	//// set DkS coordinate system
	//FbxAxisSystem::DirectX.ConvertScene(scene);

	//FbxSystemUnit::m.ConvertScene(scene);

	FbxNode* sceneRoot = scene->GetRootNode();

	std::map<int, ParseBone> skeletonBones;

	if (shouldGenerateBones)
	{
		for (int boneIndex = 0; boneIndex < f.Bones.size(); ++boneIndex)
		{
			const Flver::Bone& bone = f.Bones[boneIndex];

			FbxSkeleton* generatedBone = exportBone(scene, bone);

			skeletonBones.emplace(boneIndex, Create(scene, generatedBone, bone));

			std::cout << "Generated bone #" << boneIndex << " " << bone.Name << std::endl;
		}
	}
	FbxNode* skeletonRoot = nullptr;
	
	if (shouldGenerateBones)
	{
		skeletonRoot = ProcessBoneHierarchy(scene, skeletonBones);

		sceneRoot->AddChild(skeletonRoot);
	}

	if (!shouldGenerateBones)
	{
		std::cout << "Skipped bone generation" << std::endl;
	}


	std::map<int, ParseMesh> meshes;


	if (shouldGenerateMesh)
	{
		for (int meshIndex = 0; meshIndex < f.Meshes.size(); ++meshIndex)
		{
			const Flver::Mesh& mesh = f.Meshes[meshIndex];

			//FbxMesh* generatedMesh = ;

			ParseMesh meshStruct = exportMesh(scene, mesh); // Create(scene, generatedMesh, mesh);

			std::string meshName = skeletonBones[meshStruct.raw.DefaultBoneIndex].raw.Name;

			meshStruct.node->SetName(meshName.c_str());

			meshes.emplace(meshIndex, meshStruct);

			sceneRoot->AddChild(meshes[meshIndex].node);

			std::cout << "Generated mesh " << meshIndex << std::endl;
		}
	}
	else
	{
		std::cout << "Skipped mesh generation" << std::endl;
	}
	

	if (shouldGenerateMesh && shouldGenerateBones && shouldGenerateSkins)
	{
		ProcessSkin(scene, skeletonBones, meshes);
	}
	else
	{
		std::cout << "Skipping skin generation..." << std::endl;
	}
	
	if (shouldGenerateBones && shouldGenerateBindPose)
	{
		ProcessBindPose(scene, skeletonBones);
	}
	else
	{
		std::cout << "Skipping bind pose generation..." << std::endl;
	}

	if (shouldExportAnimations.Get() || animationNames.Matched())
	{
		std::map<int, HkxBone> animBones = PrepareAnimBones(skeletonBones);

		ParseAnimations(scene, animBones, animationNames.Get(), skeletonRoot);
	}
	else
	{
		std::cout << "Animation export skipped." << std::endl;
	}
/*
	FbxAxisSystem axis(FbxAxisSystem::EPreDefinedAxisSystem::eDirectX);
	axis.ConvertScene(scene);*/

	if (fbxExport)
	{
		FbxExporter* ex = FbxExporter::Create(manager, "");

		std::string exportPath = fbxExport.Get();

		ex->Initialize(exportPath.c_str());

		const bool bDidExport = ex->Export(scene);

		if (bDidExport)
		{
			std::cout << "Export complete" << std::endl;
		}
		else
		{
			std::cout << "Export failed! " << ex->GetStatus().GetErrorString() << std::endl;
		}
	}
	else
	{
		std::cout << "Export skipped." << std::endl;
	}

	manager->Destroy();

	return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
