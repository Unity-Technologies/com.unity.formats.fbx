//#define DEBUG_UNITTEST
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Fbx;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Fbx.Exporter;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests {

	public class BlendshapeTests : ModelExporterTest {
		[TearDown]
		public override void Term () {
#if (!DEBUG_UNITTEST)
			base.Term ();
#endif
		}

		private void ExportSkinnedMesh (string fileToExport, out SkinnedMeshRenderer originalSkinnedMesh, out SkinnedMeshRenderer exportedSkinnedMesh) {
			// add fbx to scene
			GameObject originalFbxObj = AssetDatabase.LoadMainAssetAtPath (fileToExport) as GameObject;
			Assert.IsNotNull (originalFbxObj);
			GameObject originalGO = GameObject.Instantiate (originalFbxObj);
			Assert.IsTrue (originalGO);

			// export fbx
			// get GameObject
			string filename = GetRandomFbxFilePath ();
			ModelExporter.ExportObject (filename, originalGO);
			GameObject fbxObj = AssetDatabase.LoadMainAssetAtPath (filename) as GameObject;
			Assert.IsTrue (fbxObj);

			originalSkinnedMesh = originalGO.GetComponentInChildren<SkinnedMeshRenderer> ();
			Assert.IsNotNull (originalSkinnedMesh);

			exportedSkinnedMesh = fbxObj.GetComponentInChildren<SkinnedMeshRenderer> ();
			Assert.IsNotNull (exportedSkinnedMesh);
		}

		[Test, TestCaseSource (typeof (AnimationTestDataClass), "BlendShapeTestCases")]
		public void TestBlendShapeExport (string fbxPath) {
			fbxPath = FindPathInUnitTests (fbxPath);
			Assert.That (fbxPath, Is.Not.Null);

			SkinnedMeshRenderer originalSMR, exportedSMR;
			ExportSkinnedMesh (fbxPath, out originalSMR, out exportedSMR);

			var originalMesh = originalSMR.sharedMesh;
			var exportedMesh = exportedSMR.sharedMesh;
			Assert.IsNotNull (originalMesh);
			Assert.IsNotNull (exportedMesh);

			// compare blend shape data
			Assert.AreNotEqual (originalMesh.blendShapeCount, 0);
			Assert.AreEqual (originalMesh.blendShapeCount, exportedMesh.blendShapeCount); {
				var deltaVertices = new Vector3[originalMesh.vertexCount];
				var deltaNormals = new Vector3[originalMesh.vertexCount];
				var deltaTangents = new Vector3[originalMesh.vertexCount];
				var fbxDeltaVertices = new Vector3[originalMesh.vertexCount];
				var fbxDeltaNormals = new Vector3[originalMesh.vertexCount];
				var fbxDeltaTangents = new Vector3[originalMesh.vertexCount];

				for (int bi = 0; bi < originalMesh.blendShapeCount; ++bi) {
					Assert.AreEqual (originalMesh.GetBlendShapeName (bi), exportedMesh.GetBlendShapeName (bi));
					Assert.AreEqual (originalMesh.GetBlendShapeFrameCount (bi), exportedMesh.GetBlendShapeFrameCount (bi));

					int frameCount = originalMesh.GetBlendShapeFrameCount (bi);
					for (int fi = 0; fi < frameCount; ++fi) {
						Assert.AreEqual (originalMesh.GetBlendShapeFrameWeight (bi, fi), exportedMesh.GetBlendShapeFrameWeight (bi, fi));

						originalMesh.GetBlendShapeFrameVertices (bi, fi, deltaVertices, deltaNormals, deltaTangents);
						exportedMesh.GetBlendShapeFrameVertices (bi, fi, fbxDeltaVertices, fbxDeltaNormals, fbxDeltaTangents);

						var v3comparer = new Vector3Comparer ();
						Assert.That (deltaVertices, Is.EqualTo (fbxDeltaVertices).Using<Vector3> (v3comparer), string.Format ("delta vertices don't match"));
						Assert.That (deltaNormals, Is.EqualTo (fbxDeltaNormals).Using<Vector3> (v3comparer), string.Format ("delta normals don't match"));
						Assert.That (deltaTangents, Is.EqualTo (fbxDeltaTangents).Using<Vector3> (v3comparer), string.Format ("delta tangents don't match"));

					}
				}
			}
		}

		// Load the case blendshape fbx into the scene and turn it's blend value to 100
		public GameObject InitBlendShape (string fbxPath) {
			fbxPath = FindPathInUnitTests (fbxPath);
			Assert.That (fbxPath, Is.Not.Null);
			var blendInstance = (GameObject) Object.Instantiate (AssetDatabase.LoadAssetAtPath<GameObject> (fbxPath));
			Assert.That (blendInstance, Is.Not.Null);
			blendInstance.GetComponent<SkinnedMeshRenderer> ().SetBlendShapeWeight (0, 100f);
			return blendInstance;
		}

		// Load the comparative fbx that we can compare our blendshape to
		public GameObject InitComparativeInstance (string instancePath) {
			var comparativeFbxPath = FindPathInUnitTests (instancePath);
			var comparativeInstance = (GameObject) Object.Instantiate (AssetDatabase.LoadAssetAtPath<GameObject> (comparativeFbxPath));
			Assert.That (comparativeInstance, Is.Not.Null);
			return comparativeInstance;
		}

		// Bakes the 100% blend blendshape into a mesh so that we can compare it to our comparative fbx cylinders, otherwise it only compares basic sharedMeshes.
		public Mesh BakeBlendshape (GameObject blendinstance) {
			Mesh bakedShape = new Mesh ();
			blendinstance.GetComponent<SkinnedMeshRenderer> ().BakeMesh (bakedShape);
			return bakedShape;
		}

		[Test, TestCaseSource (typeof (BlendshapesDataClass), "ColorBlendShapeCases")]
		public void TestGetSetCompareBlendshapeColors (string fbxPath, string comparativeFbxPath) {
			var blendinstance = InitBlendShape (fbxPath);
			var comparativeInstance = InitComparativeInstance(comparativeFbxPath);
			var blendVertices = BakeBlendshape (blendinstance);

			// Compare vertex colors from the blendshape mesh to the comparative static fbx - The test fails here, it wont when full blendshape support will be implemented
			Vector3[] comparativeVertices = comparativeInstance.GetComponent<MeshFilter> ().sharedMesh.vertices;
			Assert.AreEqual (blendVertices.vertices.Length, comparativeVertices.Length);
			Color[] blendColors = blendVertices.colors;
			Color[] comparativeColors = comparativeInstance.GetComponent<MeshFilter> ().sharedMesh.colors;
			Assert.AreEqual (blendColors.Length, comparativeColors.Length);
			for (int colorIndex = 0; colorIndex <= blendColors.Length; colorIndex++) {
				Assert.AreEqual (blendColors[colorIndex], comparativeColors[colorIndex]);
			}
			// TODO - Uni-55429 - If unity can at this point import blendshape vertex colors, can the FBXExporter now roundtrip an asset of the sort? Could be a separate test.
		}

		[Test, TestCaseSource (typeof (BlendshapesDataClass), "VertexNormalBlendShapeCases")]
		public void TestGetSetCompareBlendshapeNormals (string fbxPath, string comparativeFbxPath) {
			var blendinstance = InitBlendShape (fbxPath);
			var comparativeInstance = InitComparativeInstance (comparativeFbxPath);
			var blendVertices = BakeBlendshape (blendinstance);

			// Compare vertex normal from the blendshape mesh to the comparative fbx - The test fails here, it wont when full blendshape support will be implemented
			Vector3[] comparativeVertices = comparativeInstance.GetComponent<MeshFilter> ().sharedMesh.vertices;
			Assert.AreEqual (blendVertices.vertices.Length, comparativeVertices.Length);
			Vector3[] blendNormals = blendVertices.normals;
			Vector3[] comparativeNormals = comparativeInstance.GetComponent<MeshFilter> ().sharedMesh.normals;
			Assert.AreEqual (blendNormals.Length, comparativeNormals.Length);
			for (int normalIndex = 0; normalIndex <= blendNormals.Length; normalIndex++) {
				Assert.AreEqual (blendNormals[normalIndex], comparativeNormals[normalIndex]);
			}
			// TODO - Uni-55429 - If unity can at this point import blendshape vertex normals, can the FBXExporter now roundtrip an asset of the sort? Could be a separate test. 
		}

		[Test, TestCaseSource (typeof (BlendshapesDataClass), "VertexNormalBlendShapeCases")]
		public void TestGetSetCompareBlendshapeTangents (string fbxPath, string comparativeFbxPath) {
			var blendinstance = InitBlendShape (fbxPath);
			var comparativeInstance = InitComparativeInstance (comparativeFbxPath);
			var blendVertices = BakeBlendshape (blendinstance);

			// Compare vertex normal from the blendshape mesh to the comparative fbx - The test fails here, it wont when full blendshape support will be implemented
			Vector3[] comparativeVertices = comparativeInstance.GetComponent<MeshFilter> ().sharedMesh.vertices;
			Assert.AreEqual (blendVertices.vertices.Length, comparativeVertices.Length);

			Vector4[] blendTangents = blendVertices.tangents;
			Vector4[] comparativeTangents = comparativeInstance.GetComponent<MeshFilter> ().sharedMesh.tangents;
			Assert.AreEqual (blendTangents.Length, comparativeTangents.Length);
			for (int tangentIndex = 0; tangentIndex <= blendTangents.Length; tangentIndex++) {
				Assert.AreEqual (blendTangents[tangentIndex], comparativeTangents[tangentIndex]);
			}
			// TODO - Uni-55429 - If unity can at this point import blendshape vertex normals, can the FBXExporter now roundtrip an asset of the sort? Could be a separate test. 
		}

	}

	public class BlendshapesDataClass {
		public static IEnumerable BlendShapeTestCases {
			get {
				yield return "Models/Blendshapes/blendshape.fbx";
				yield return "Models/Blendshapes/blendshape_with_skinning.fbx";
			}
		}

		public static IEnumerable ColorBlendShapeCases {
			get {
				yield return new TestCaseData ("Models/Blendshapes/BlendShape_Color.fbx","Models/BlendShapes/Comparative_VertexColorCylinder.fbx");
			}
		}

		public static IEnumerable VertexNormalBlendShapeCases {
			get {
				yield return new TestCaseData ("Models/Blendshapes/Blendshape_VertexNormal.fbx","Models/BlendShapes/Comparative_NormalCylinder.fbx");
			}
		}
	}

}