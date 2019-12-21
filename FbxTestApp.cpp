// FbxTestApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "pch.h"
#include <iostream>

#include "Flver.h"

#include "json/json.hpp"

#include "JsonParser.h"

#include <fstream>

#include <algorithm>
#include <cmath>
#include <fbxsdk.h>

FbxVector4 Convert(const Vector3& vector)
{
	return FbxVector4(vector[0], vector[1], vector[2]);
}

FbxVector4 Convert(const Vector4& vector)
{
	return FbxVector4(vector[0], vector[1], vector[2], vector[3]);
}

FbxMesh* exportMesh(FbxScene* scene, const Flver::Mesh& mesh)
{
	FbxMesh* fbx = FbxMesh::Create(scene, "");

	//fbx->SetControlPointCount(mesh.Vertices.size());
	fbx->InitControlPoints(mesh.Vertices.size());
	fbx->InitNormals();
	for (int vertexIndex = 0; vertexIndex < mesh.Vertices.size(); ++vertexIndex)
	{
		const Flver::Vertex& vertex = mesh.Vertices[vertexIndex];
		
		fbx->SetControlPointAt(Convert(vertex.Position), Convert(vertex.Normal), vertexIndex);
	}

	for (int faceSetIndex = 0; faceSetIndex < mesh.FaceSets.size(); ++faceSetIndex)
	{
		const Flver::FaceSet& faceSet = mesh.FaceSets[faceSetIndex];

		if (faceSet.Indices.size() % 3 != 0)
		{
			abort();
			return nullptr;
		}

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

	return fbx;
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


//FbxSkeleton* exportSkeleton(const Flver::Skeleton& skeleton)
//{
//
//}

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
#pragma optimize ("",off)
FbxMatrix GetWorldTransform(const ParseBone& bone, const std::map<int, ParseBone>& allBones)
{
	ParseBone currentBone = bone;

	Matrix4x4 Result;

	//Result.SetIdentity();

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


		//const FbxMatrix matrices[] = { 
		//	FbxMatrix(Empty, Empty, scale),
		//	FbxMatrix(Empty, FbxVector4(euler[0], 0, 0), NoneScale),//.Transpose() ,
		//	FbxMatrix(Empty, FbxVector4(0, 0, euler[2]), NoneScale),//.Transpose() ,
		//	FbxMatrix(Empty, FbxVector4(0, euler[1], 0), NoneScale),//.Transpose() ,
		//	FbxMatrix(translation, Empty, NoneScale),
		//};

		//FbxMatrix localTransformation;
		//localTransformation.SetIdentity();

		

		//Matrix4x4 scaleRes = Result * scaleM;
		//Matrix4x4 rotXRes = scaleRes * rotXM;
		//Matrix4x4 rotZRes = rotXRes * rotZM;

		Matrix4x4 scaleRes = scaleM * Result;
		Matrix4x4 rotXRes = rotXM* scaleRes;
		Matrix4x4 rotZRes = rotZM* rotXRes;


		if (bone.raw.Name == "Spine1" && currentBone.raw.Name == "Spine")
		{
			//Matrix4x4 transposedZ = rotZRes;

			//gmtl::transpose(transposedZ);

			//Matrix4x4 transposedRotZRes = rotXRes * transposedZ;


			std::cout << "row 0 col 3 " << rotZRes(0, 3) << " before Z ROT:  " << rotXRes[0][3] << std::endl;
			std::cout << "row 3 col 0 " << rotZRes(3, 0) << " before Z ROT:  " << rotXRes[3][0] << std::endl;
			
			std::cout << "row 1 col 3 " << rotZRes(1, 3) << " before Z ROT:  " << rotXRes[1][3] << std::endl;
			std::cout << "row 3 col 1 " << rotZRes(3, 1) << " before Z ROT:  " << rotXRes[3][1] << std::endl;
		}

		//Matrix4x4 rotYRes = rotZRes * rotYM;
		//Matrix4x4 transRes = rotYRes * translateM;
		Matrix4x4 rotYRes = rotYM* rotZRes ;
		Matrix4x4 transRes = translateM* rotYRes ;

		Result = transRes;


		//for (const Matrix4x4 matrix : matrices)
		//{
		//	//printf("Busy working on matrix... %f %f %f %f\r\n", Result[3][0], Result[3][1], Result[3][2], Result[3][3]);
		//	Matrix4x4 temp = Result * matrix;
		//	Result = temp;
		//	//localTransformation *= matrix;
		//}

		//Result = localTransformation * Result;

		if (currentBone.raw.ParentIndex < 0)
		{
			break;
		}


		currentBone = allBones.at(currentBone.raw.ParentIndex);
	} while (true);

	auto& boneMatrix = Result;
	//const FbxDouble4 LastRow = boneMatrix[3];

	//boneMatrix[0][3] = LastRow[0];
	//boneMatrix[1][3] = LastRow[1];
	//boneMatrix[2][3] = LastRow[2];

	using std::swap;

	//swap(boneMatrix[0][3], boneMatrix[3][0]);
	//swap(boneMatrix[1][3], boneMatrix[3][1]);
	//swap(boneMatrix[2][3], boneMatrix[3][2]);

	//boneMatrix[3][0] = 0;
	//boneMatrix[3][1] = 0;
	//boneMatrix[3][2] = 0;

	return FbxMatrix(
		Result[0][0], Result[0][1], Result[0][2], Result[0][3],
		Result[1][0], Result[1][1], Result[1][2], Result[1][3],
		Result[2][0], Result[2][1], Result[2][2], Result[2][3],
		Result[3][0], Result[3][1], Result[3][2], Result[3][3]
	).Transpose();

	//return Result;
}
#pragma optimize ("", on)
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

void ProcessBindPose(FbxScene* scene, std::map<int, ParseBone>& skeletonBones, std::map<int, ParseMesh>& meshes)
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


int main()
{
	Matrix4x4 m1, m2;


	m1(1, 1) = -0.99999999999545919;
	m1(1, 2) = -3.0135897933838376e-06;
	m1(2, 1) = 3.0135897933838376e-06;
	m1(2, 2) = -0.99999999999545919;
	m1(3, 0) = 0.18;
	m1(3, 1) = 1.5560814899999998e-08;
	m1(3, 2) = -0.00020003691299999999;


	m1.setState(Matrix4x4::FULL);

	const double ZRotation = 1.5707962499999999;


	Matrix4x4 rotZM;
	gmtl::setRot(rotZM, gmtl::EulerAngleXYZd(0, 0, ZRotation));

	gmtl::zero(m2);
	m2(1, 0) = -1;
	m2(0, 1) = 1;
	m2(2, 2) = 1;
	m2(3, 3) = 1;

	m2.setState(Matrix4x4::FULL);

	Matrix4x4 testm = m1;
	testm *= rotZM;

	std::cout << "row 0 col 3 " << testm(0, 3) << " via braces " << testm[0][3] << std::endl;
	std::cout << "row 3 col 0 " << testm(3, 0) << " via braces " << testm[3][0] << std::endl;

	std::cout << "row 1 col 3 " << testm(1, 3) << " via braces " << testm[1][3] << std::endl;
	std::cout << "row 3 col 1 " << testm(3, 1) << " via braces " << testm[3][1] << std::endl;

	std::string file_contents;
	nlohmann::json parsed;
	{
		std::ifstream stream = std::ifstream("flver.json");
		stream >> parsed;
		//do
		//{

		//	stream >> file_contents;
		//} while (!stream.eof());
	}

	std::cout << "Read input" << std::endl;
	
	Flver::Flver f;
	parsed.get_to(f);

	std::cout << "Input converted to Flver struct" << std::endl;

	FbxManager* manager = FbxManager::Create();

	//FbxMatrix testm(FbxVector4(10, 100, 1000), FbxVector4(), FbxVector4(1, 1, 1));

	FbxScene* scene = FbxScene::Create(manager, "");

	FbxNode* sceneRoot = scene->GetRootNode();

	std::map<int, ParseBone> skeletonBones;

	for (int boneIndex = 0; boneIndex < f.Bones.size(); ++boneIndex)
	{
		const Flver::Bone& bone = f.Bones[boneIndex];

		FbxSkeleton* generatedBone = exportBone(scene, bone);

		skeletonBones.emplace(boneIndex, Create(scene, generatedBone, bone));

		std::cout << "Generated bone #" << boneIndex << " " << bone.Name << std::endl;
	}

	FbxNode* skeletonRoot = ProcessBoneHierarchy(scene, skeletonBones);

	sceneRoot->AddChild(skeletonRoot);


	std::map<int, ParseMesh> meshes;

	for (int meshIndex = 0; meshIndex < f.Meshes.size(); ++meshIndex)
	{
		const Flver::Mesh& mesh = f.Meshes[meshIndex];

		FbxMesh* generatedMesh = exportMesh(scene, mesh);

		ParseMesh meshStruct = Create(scene, generatedMesh, mesh);

		std::string meshName = skeletonBones[meshStruct.raw.DefaultBoneIndex].raw.Name;

		meshStruct.node->SetName(meshName.c_str());

		meshes.emplace(meshIndex, meshStruct);

		sceneRoot->AddChild(meshes[meshIndex].node);

		std::cout << "Generated mesh " << meshIndex << std::endl;
	}

	
	ProcessSkin(scene, skeletonBones, meshes);

	ProcessBindPose(scene, skeletonBones, meshes);


	FbxExporter* ex = FbxExporter::Create(manager, "");

	ex->Initialize("out.fbx");
	
	const bool bDidExport = ex->Export(scene);

	if (bDidExport)
	{
		std::cout << "Export complete" << std::endl;
	}
	else
	{
		std::cout << "Export failed! " << ex->GetStatus().GetErrorString() << std::endl;
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
