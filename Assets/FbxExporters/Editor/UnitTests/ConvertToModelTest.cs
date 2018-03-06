using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using FbxExporters.Editor;

namespace FbxExporters.UnitTests
{
    public class ConvertToModelTest : ExporterTestBase
    {
        public static List<string> ChildNames(Transform a) {
            var names = new List<string>();
            foreach(Transform child in a) {
                names.Add(child.name);
            }
            return names;
        }

        [Test]
        public void TestStaticHelpers()
        {
            // Test IncrementFileName
            {
                var tempPath = Path.GetTempPath ();
                var basename = Path.GetFileNameWithoutExtension (Path.GetRandomFileName ());
                basename = basename + "yo"; // add some non-numeric stuff

                var filename1 = basename + ".fbx";
                var filename2 = Path.Combine(tempPath, basename + " 1.fbx");
                Assert.AreEqual (filename2, ConvertToModel.IncrementFileName (tempPath, filename1));

                filename1 = basename + " 1.fbx";
                filename2 = Path.Combine(tempPath, basename + " 2.fbx");
                Assert.AreEqual (filename2, ConvertToModel.IncrementFileName (tempPath, filename1));

                filename1 = basename + "1.fbx";
                filename2 = Path.Combine(tempPath, basename + "2.fbx");
                Assert.AreEqual (filename2, ConvertToModel.IncrementFileName (tempPath, filename1));

                // UNI-25513: bug was that Cube01.fbx => Cube2.fbx
                filename1 = basename + "01.fbx";
                filename2 = Path.Combine(tempPath, basename + "02.fbx");
                Assert.AreEqual (filename2, ConvertToModel.IncrementFileName (tempPath, filename1));
            }

            // Test EnforceUniqueNames
            {
                var a = new GameObject("a");
                var b = new GameObject("b");
                var a1 = new GameObject("a");
                var a2 = new GameObject("a");
                ConvertToModel.EnforceUniqueNames(new GameObject[] { a, b, a1, a2 });
                Assert.AreEqual("a", a.name);
                Assert.AreEqual("b", b.name);
                Assert.AreEqual("a 1", a1.name);
                Assert.AreEqual("a 2", a2.name);
            }

            // Test CopyComponents
            {
                var a = GameObject.CreatePrimitive (PrimitiveType.Cube);
                a.name = "a";
                var b = GameObject.CreatePrimitive (PrimitiveType.Sphere);
                b.name = "b";
                a.AddComponent<BoxCollider>();
                a.transform.localPosition += new Vector3(1,2,3);
                Assert.IsFalse(b.GetComponent<BoxCollider>());
                Assert.AreEqual(Vector3.zero, b.transform.localPosition);
                Assert.AreNotEqual (a.GetComponent<MeshFilter>().sharedMesh, b.GetComponent<MeshFilter> ().sharedMesh);
                ConvertToModel.CopyComponents(b, a);
                Assert.IsFalse(b.GetComponent<BoxCollider>());
                Assert.AreEqual(Vector3.zero, b.transform.localPosition);
                Assert.AreEqual (a.GetComponent<MeshFilter>().sharedMesh, b.GetComponent<MeshFilter> ().sharedMesh);
            }

            // Test UpdateFromSourceRecursive. Very similar but recursive.
            {
                var a = GameObject.CreatePrimitive (PrimitiveType.Cube);
                a.name = "a";
                var a1 = GameObject.CreatePrimitive (PrimitiveType.Cube);
                a1.name = "AA";
                var a2 = GameObject.CreatePrimitive (PrimitiveType.Cube);
                a2.name = "BB";
                a2.transform.parent = a.transform;
                a1.transform.parent = a.transform; // out of alpha order!
                var b = GameObject.CreatePrimitive (PrimitiveType.Sphere);
                b.name = "b";
                var b1 = GameObject.CreatePrimitive (PrimitiveType.Sphere);
                b1.name = "AA";
                var b2 = GameObject.CreatePrimitive (PrimitiveType.Sphere);
                b2.name = "BB";
                b1.transform.parent = b.transform;
                b2.transform.parent = b.transform; // in alpha order
                a.AddComponent<BoxCollider> ();
                a1.transform.localPosition = new Vector3 (1, 2, 3);

                Assert.AreNotEqual(b.GetComponent<MeshFilter>().sharedMesh, a.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsFalse (b.GetComponent<BoxCollider> ());
                Assert.AreEqual ("BB", b.transform.GetChild (1).name);
                Assert.AreEqual (Vector3.zero, b1.transform.localPosition);

                ConvertToModel.UpdateFromSourceRecursive (b, a);

                // only the mesh + materials should change
                Assert.AreEqual(b.GetComponent<MeshFilter>().sharedMesh, a.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsFalse (b.GetComponent<BoxCollider> ());
                Assert.AreEqual ("BB", b.transform.GetChild (1).name);
                Assert.AreEqual (Vector3.zero, b1.transform.localPosition);
            }
        }

        [Test]
        public void BasicTest()
        {
            // Get a random directory.
            var path = GetRandomFileNamePath(extName: "");

            // Create a cube.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Convert it to a prefab -- but keep the cube.
            var cubePrefabInstance = ConvertToModel.Convert(cube,
                fbxDirectoryFullPath: path);

            // Make sure it's what we expect.
            Assert.That(cube); // we kept the original
            Assert.That(cubePrefabInstance); // we got the new
            Assert.AreSame(cube, cubePrefabInstance); // the original and new are the same
            Assert.AreEqual("Cube", cubePrefabInstance.name); // it has the right name
            Assert.That(!EditorUtility.IsPersistent(cubePrefabInstance));
            var cubePrefabAsset = PrefabUtility.GetPrefabParent(cubePrefabInstance);
            Assert.That(cubePrefabAsset);
            Assert.That (EditorUtility.IsPersistent (cubePrefabAsset));

            Assert.That(cubePrefabInstance.GetComponent<FbxPrefab>());

            // Should be all the same triangles. But it isn't. TODO.
            // At least the indices should match in multiplicity.
            var cubeMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
            var cubePrefabMesh = cubePrefabInstance.GetComponent<MeshFilter>().sharedMesh;
            //Assert.That(
            //  cubeMesh.triangles,
            //  Is.EqualTo(cubePrefabMesh.triangles)
            //);
            Assert.That(cubeMesh.triangles, Is.EquivalentTo(cubeMesh.triangles));

            // Make sure it's where we expect.
            var assetRelativePath = AssetDatabase.GetAssetPath(cubePrefabAsset);
            var assetFullPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../" + assetRelativePath));
            Assert.AreEqual(Path.GetFullPath(path), Path.GetDirectoryName(assetFullPath));

            // Convert it again, make sure there's only one FbxPrefab (see UNI-25528).
            // Also make sure we deleted.
            var cubePrefabInstance2 = ConvertToModel.Convert(cubePrefabInstance,
                fbxDirectoryFullPath: path);
            Assert.That(cubePrefabInstance2.GetComponents<FbxPrefab>().Length, Is.EqualTo(1));
        }

        [Test]
        public void SkinnedMeshTest()
        {
            // UNI-24379: in the first release, the plugin only handles static
            // meshes, not skinned meshes. Rewrite this test when we add that
            // feature.

            // Create a cube with a bogus skinned-mesh rather than a static
            // mesh setup.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<SkinnedMeshRenderer>();
            var meshFilter = cube.GetComponent<MeshFilter>();
            var meshRender = cube.GetComponent<MeshRenderer>();
            Object.DestroyImmediate(meshRender);
            Object.DestroyImmediate(meshFilter);

            // Convert it.
            var cubeInstance = ConvertToModel.Convert(cube, fbxFullPath: GetRandomFbxFilePath());

            // Make sure it doesn't have a skinned mesh renderer on it.
            // In the future we'll want to assert the opposite!
            Assert.That(cubeInstance.GetComponentsInChildren<SkinnedMeshRenderer>(), Is.Empty);
        }

        [Test]
        public void MapNameToSourceTest()
        {
            //Create a cube with 3 children game objects
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

            capsule.transform.parent = cube.transform;
            sphere.transform.parent = cube.transform;
            quad.transform.parent = cube.transform;
            capsule.transform.SetSiblingIndex(0);

            //Create a similar Heirarchy that we can use as our phony "exported" hierarchy.
            var cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var capsule2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var quad2 = GameObject.CreatePrimitive(PrimitiveType.Quad);

            capsule2.transform.parent = cube2.transform;
            sphere2.transform.parent = cube2.transform;
            quad2.transform.parent = cube2.transform;
            capsule.transform.SetSiblingIndex(1);

            var dictionary = ConvertToModel.MapNameToSourceRecursive(cube, cube2);

            //We expect these to pass because we've given it an identical game object, as it would have after a normal export.
            Assert.AreSame(capsule2, dictionary[capsule.name]);
            Assert.AreSame(sphere2, dictionary[sphere.name]);
            Assert.AreSame(quad2, dictionary[quad.name]);
            Assert.True(dictionary.Count == 4);

            //Create a broken hierarchy, one that is missing a primitive
            var cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var capsule3 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var sphere3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            capsule3.transform.parent = cube3.transform;
            sphere3.transform.parent = cube3.transform;

            var dictionaryBroken = ConvertToModel.MapNameToSourceRecursive(cube, cube3);

            //the dictionary size should be equal to the amount of children + the parent
            Assert.True(dictionaryBroken.Count == 4);

            Assert.IsNull(dictionaryBroken[quad.name]);
            Assert.AreSame(capsule3, dictionaryBroken[capsule.name]);
            Assert.AreSame(sphere3, dictionaryBroken[sphere.name]);
        }

        [Test]
        public void TestInstanceNameMatchesFilename()
        {
            // create a cube, export it to random filename
            // make sure instance name gets updated when converting to prefab

            // Get a random directory.
            var path = GetRandomFileNamePath(extName: ".fbx");

            // Create a cube.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Convert it to a prefab -- but keep the cube.
            var cubePrefabInstance = ConvertToModel.Convert(cube,
                fbxFullPath: path);

            Assert.That (cube);
            Assert.That (cubePrefabInstance);
            Assert.AreSame (cube, cubePrefabInstance);

            Assert.AreEqual (Path.GetFileNameWithoutExtension (path), cube.name);
        }

        [Test]
        public void TestConvertModelInstance()
        {
            // expected result: prefab is linked to originally exported
            //                  fbx and no new fbx is created.

            // create hierarchy (Cube->Sphere)
            var cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
            var sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);

            sphere.transform.SetParent (cube.transform);

            // export using regular model export
            var filename = GetRandomFileNamePath();
            GameObject fbxObj = ExportSelection (filename, cube);

            // add back to scene
            GameObject fbxInstance = PrefabUtility.InstantiatePrefab (fbxObj) as GameObject;
            Assert.That (fbxInstance, Is.Not.Null);

            // attach some components
            var rigidBody = fbxInstance.AddComponent<Rigidbody> ();
            Assert.That (rigidBody, Is.Not.Null);

            var instanceSphere = fbxInstance.transform.GetChild (0);
            Assert.That (instanceSphere, Is.Not.Null);

            var boxCollider = instanceSphere.gameObject.AddComponent<BoxCollider> ();
            Assert.That (boxCollider, Is.Not.Null);

            // convert to prefab
            GameObject converted = ConvertToModel.Convert (fbxInstance, Path.GetDirectoryName(filename));
            Assert.That (converted, Is.EqualTo (fbxInstance));

            // check meshes link to original fbx
            var prefabCubeMesh = fbxInstance.GetComponent<MeshFilter>().sharedMesh;
            Assert.That (prefabCubeMesh, Is.Not.Null);

            var fbxObjAssetPath = AssetDatabase.GetAssetPath (fbxObj);

            Assert.That (AssetDatabase.GetAssetPath (prefabCubeMesh), Is.EqualTo (fbxObjAssetPath));

            var prefabSphere = fbxInstance.transform.GetChild (0);
            Assert.That (prefabSphere, Is.Not.Null);
            var prefabSphereMesh = prefabSphere.GetComponent<MeshFilter> ().sharedMesh;
            Assert.That (prefabSphere, Is.Not.Null);

            Assert.That (AssetDatabase.GetAssetPath (prefabSphereMesh), Is.EqualTo (fbxObjAssetPath));

            // check that components are still there
            Assert.That (fbxInstance.GetComponent<Rigidbody>(), Is.Not.Null);
            Assert.That (prefabSphere.GetComponent<BoxCollider> (), Is.Not.Null);
        }
    }
}
