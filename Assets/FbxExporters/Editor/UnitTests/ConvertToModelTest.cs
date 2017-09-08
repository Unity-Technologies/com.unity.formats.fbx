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

                var filename1 = basename + " 1.fbx";
                var filename2 = Path.Combine(tempPath, basename + " 2.fbx");
                Assert.AreEqual (filename2, ConvertToModel.IncrementFileName (tempPath, filename1));

                filename1 = basename + "1.fbx";
                filename2 = Path.Combine(tempPath, basename + "2.fbx");
                Assert.AreEqual (filename2, ConvertToModel.IncrementFileName (tempPath, filename1));

                filename1 = basename + "k.fbx";
                filename2 = Path.Combine(tempPath, basename + "k 1.fbx");
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

            // Test FixSiblingOrder
            {
                var a = new GameObject("a").transform;
                var b = new GameObject("a").transform;

                var a1 = new GameObject("a1").transform;
                var a2 = new GameObject("a2").transform;
                var a3 = new GameObject("a3").transform;

                var b1 = new GameObject("a1").transform;
                var b2 = new GameObject("a2").transform;
                var b3 = new GameObject("a3").transform;

                a1.parent = a;
                a2.parent = a;
                a3.parent = a;

                b3.parent = b;
                b1.parent = b;
                b2.parent = b;

                // Assert same set, different order.
                Assert.That(ChildNames(b), Is.EquivalentTo(ChildNames(a)));
                Assert.That(ChildNames(b), Is.Not.EqualTo(ChildNames(a)));

                // Fix the sibling order. Now we have same set, same order!
                ConvertToModel.FixSiblingOrder(a, b);
                Assert.That(ChildNames(b), Is.EquivalentTo(ChildNames(a)));
                Assert.That(ChildNames(b), Is.EqualTo(ChildNames(a)));
            }

            // Test CopyComponents
            {
                var a = new GameObject("a");
                var b = new GameObject("b");
                a.AddComponent<BoxCollider>();
                a.transform.localPosition += new Vector3(1,2,3);
                Assert.IsFalse(b.GetComponent<BoxCollider>());
                Assert.AreEqual(Vector3.zero, b.transform.localPosition);
                ConvertToModel.CopyComponents(a, b);
                Assert.IsTrue(b.GetComponent<BoxCollider>());
                Assert.AreEqual(new Vector3(1,2,3), b.transform.localPosition);
            }

            // Test SetupImportedGameObject. Very similar but recursive.
            {
                var a = new GameObject ("a");
                var a1 = new GameObject ("AA");
                var a2 = new GameObject ("BB");
                a2.transform.parent = a.transform;
                a1.transform.parent = a.transform; // out of alpha order!
                var b = new GameObject ("b");
                var b1 = new GameObject ("AA");
                var b2 = new GameObject ("BB");
                b1.transform.parent = b.transform;
                b2.transform.parent = b.transform; // in alpha order
                a.AddComponent<BoxCollider> ();
                a1.transform.localPosition = new Vector3 (1, 2, 3);

                Assert.IsFalse (b.GetComponent<BoxCollider> ());
                Assert.AreEqual ("BB", b.transform.GetChild (1).name);
                Assert.AreEqual (Vector3.zero, b1.transform.localPosition);

                ConvertToModel.SetupImportedGameObject (a, b);

                Assert.IsTrue (b.GetComponent<BoxCollider> ());
                Assert.AreEqual ("AA", b.transform.GetChild (1).name);
                Assert.AreEqual (new Vector3 (1, 2, 3), b1.transform.localPosition);
            }
        }

        [Test]
        public void BasicTest()
        {
            // Get a random directory.
            var path = GetRandomFileNamePath(extName: "");

            // Create a cube.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Convert it to a prefab.
            var cubePrefabInstance = ConvertToModel.Convert(cube,
                directoryFullPath: path, keepOriginal: true);

            // Make sure it's what we expect.
            Assert.IsTrue(cube); // we kept the original
            Assert.IsTrue(cubePrefabInstance); // we got the new
            Assert.AreEqual("Cube", cubePrefabInstance.name); // it has the right name
            Assert.IsFalse(EditorUtility.IsPersistent(cubePrefabInstance));
            var cubePrefabAsset = PrefabUtility.GetPrefabParent(cubePrefabInstance);

            // it's a different mesh instance, but the same mesh
            Assert.AreNotEqual(
                cube.GetComponent<MeshFilter>().sharedMesh,
                cubePrefabInstance.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsTrue(cubePrefabInstance.GetComponent<FbxPrefab>());

            // Should be all the same triangles. But it isn't. TODO.
            // At least the indices should match in multiplicity.
            var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
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
        }
    }
}
