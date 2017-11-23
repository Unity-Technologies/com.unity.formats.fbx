using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.FbxSdk;
using FbxExporters.Editor;

namespace FbxExporters.UnitTests
{
    public class ModelExporterTest : ExporterTestBase
    {
        [Test]
        public void TestBasics ()
        {
            Assert.That(!string.IsNullOrEmpty(ModelExporter.GetVersionFromReadme()));

            // Test GetOrCreateLayer
            using (var fbxManager = FbxManager.Create()) {
                var fbxMesh = FbxMesh.Create(fbxManager, "name");
                var layer0 = ModelExporter.GetOrCreateLayer(fbxMesh);
                Assert.That(layer0, Is.Not.Null);
                Assert.That(ModelExporter.GetOrCreateLayer(fbxMesh), Is.EqualTo(layer0));
                var layer5 = ModelExporter.GetOrCreateLayer(fbxMesh, layer: 5);
                Assert.That(layer5, Is.Not.Null);
                Assert.That(layer5, Is.Not.EqualTo(layer0));
            }

            // Test axis conversion: a x b in left-handed is the same as b x a
            // in right-handed (that's why we need to flip the winding order).
            var a = new Vector3(1,0,0);
            var b = new Vector3(0,0,1);
            var crossLeft = Vector3.Cross(a,b);

            var afbx = ModelExporter.ConvertNormalToRightHanded(a);
            var bfbx = ModelExporter.ConvertNormalToRightHanded(b);
            Assert.AreEqual(ModelExporter.ConvertNormalToRightHanded(crossLeft), bfbx.CrossProduct(afbx));

            // Test scale conversion. Nothing complicated here...
            var afbxPosition = ModelExporter.ConvertPositionToRightHanded(a);
            Assert.AreEqual(100, afbxPosition.Length());

            // Test rotation conversion.
            var q = Quaternion.Euler(new Vector3(0, 90, 0));
            var fbxAngles = ModelExporter.ConvertQuaternionToXYZEuler(q);
            Assert.AreEqual(fbxAngles.X, 0);
            Assert.That(fbxAngles.Y, Is.InRange(-90.001, -89.999));
            Assert.AreEqual(fbxAngles.Z, 0);

            Assert.That(ModelExporter.DefaultMaterial);

            // Test non-static functions.
            using (var fbxManager = FbxManager.Create()) {
                var fbxScene = FbxScene.Create(fbxManager, "scene");
                var fbxNode = FbxNode.Create (fbxScene, "node");
                var exporter = new ModelExporter();

                // Test ExportMaterial: it exports and it re-exports
                bool result = exporter.ExportMaterial (ModelExporter.DefaultMaterial, fbxScene, fbxNode);
                Assert.IsTrue (result);
                var fbxMaterial = fbxNode.GetMaterial (0);
                Assert.That(fbxMaterial, Is.Not.Null);

                result = exporter.ExportMaterial(ModelExporter.DefaultMaterial, fbxScene, fbxNode);
                var fbxMaterial2 = fbxNode.GetMaterial (1);
                Assert.AreEqual(fbxMaterial, fbxMaterial2);

                // Test ExportTexture: it finds the same texture for the default-material (it doesn't create a new one)
                var fbxMaterialNew = FbxSurfaceLambert.Create(fbxScene, "lambert");
                exporter.ExportTexture(ModelExporter.DefaultMaterial, "_MainTex",
                    fbxMaterialNew, FbxSurfaceLambert.sBump);
                Assert.AreEqual(
                    fbxMaterial.FindProperty(FbxSurfaceLambert.sDiffuse).GetSrcObject(),
                    fbxMaterialNew.FindProperty(FbxSurfaceLambert.sBump).GetSrcObject()
                );

                // Test ExportMesh: make sure we exported a mesh with welded vertices.
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var cubeNode = FbxNode.Create(fbxScene, "cube");
                exporter.ExportMesh(cube.GetComponent<MeshFilter>().sharedMesh, cubeNode);
                Assert.That(cubeNode.GetMesh(), Is.Not.Null);
                Assert.That(cubeNode.GetMesh().GetControlPointsCount(), Is.EqualTo(8));
            }

            // Test exporting a skinned-mesh. Make sure it doesn't leak (it did at one point)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var character = new GameObject();
                var smr = character.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                var meshCount = Object.FindObjectsOfType<Mesh>().Length;
                ModelExporter.ExportObject(GetRandomFbxFilePath(), character);
                Assert.AreEqual(meshCount, Object.FindObjectsOfType<Mesh>().Length);
            }
        }

        [Test]
        public void TestFindCenter ()
        {
            // Create 3 objects
            var cube = CreateGameObject ("cube");
            var cube1 = CreateGameObject ("cube1");
            var cube2 = CreateGameObject ("cube2");

            // Set their transforms
            cube.transform.localPosition = new Vector3 (23, -5, 10);
            cube1.transform.localPosition = new Vector3 (23, -5, 4);
            cube1.transform.localScale = new Vector3 (1, 1, 2);
            cube2.transform.localPosition = new Vector3 (28, 0, 10);
            cube2.transform.localScale = new Vector3 (3, 1, 1);

            // Find the center
            var center = FbxExporters.Editor.ModelExporter.FindCenter(new GameObject[]{cube,cube1,cube2});

            // Check that it is what we expect
            Assert.AreEqual(center, new Vector3(26, -2.5f, 6.75f));
        }

        [Test]
        public void TestRemoveRedundantObjects ()
        {
            var root = CreateGameObject ("root");
            var child1 = CreateGameObject ("child1", root.transform);
            var child2 = CreateGameObject ("child2", root.transform);
            var root2 = CreateGameObject ("root2");

            // test set: root
            // expected result: root
            var result = FbxExporters.Editor.ModelExporter.RemoveRedundantObjects(new Object[]{root});
            Assert.AreEqual (1, result.Count);
            Assert.IsTrue (result.Contains (root));

            // test set: root, child1
            // expected result: root
            result = FbxExporters.Editor.ModelExporter.RemoveRedundantObjects(new Object[]{root, child1});
            Assert.AreEqual (1, result.Count);
            Assert.IsTrue (result.Contains (root));

            // test set: root, child1, child2, root2
            // expected result: root, root2
            result = FbxExporters.Editor.ModelExporter.RemoveRedundantObjects(new Object[]{root, root2, child2, child1});
            Assert.AreEqual (2, result.Count);
            Assert.IsTrue (result.Contains (root));
            Assert.IsTrue (result.Contains (root2));

            // test set: child1, child2
            // expected result: child1, child2
            result = FbxExporters.Editor.ModelExporter.RemoveRedundantObjects(new Object[]{child2, child1});
            Assert.AreEqual (2, result.Count);
            Assert.IsTrue (result.Contains (child1));
            Assert.IsTrue (result.Contains (child2));
        }

        [Test]
        public void TestConvertToValidFilename()
        {
            // test already valid filenames
            var filename = "foobar.fbx";
            var result = FbxExporters.Editor.ModelExporter.ConvertToValidFilename (filename);
            Assert.AreEqual (filename, result);

            filename = "foo_bar 1.fbx";
            result = FbxExporters.Editor.ModelExporter.ConvertToValidFilename (filename);
            Assert.AreEqual (filename, result);

            // test invalid filenames
            filename = "?foo**bar///.fbx";
            result = FbxExporters.Editor.ModelExporter.ConvertToValidFilename (filename);
#if UNITY_EDITOR_WIN
            Assert.AreEqual ("_foo__bar___.fbx", result);
#else
            Assert.AreEqual ("?foo**bar___.fbx", result);
#endif

            filename = "foo$?ba%r 2.fbx";
            result = FbxExporters.Editor.ModelExporter.ConvertToValidFilename (filename);
#if UNITY_EDITOR_WIN
            Assert.AreEqual ("foo$_ba%r 2.fbx", result);
#else
            Assert.AreEqual ("foo$?ba%r 2.fbx", result);
#endif
        }

        class CallbackTester {
            public string filename;
            public Transform tree;

            int componentCalls;
            int objectCalls;
            bool objectResult;

            public CallbackTester(Transform t, string f) {
                filename = f;
                tree = t;
            }

            public bool CallbackForFbxPrefab(ModelExporter exporter, FbxPrefab component, FbxNode fbxNode) {
                componentCalls++;
                return true;
            }

            public bool CallbackForObject(ModelExporter exporter, GameObject go, FbxNode fbxNode) {
                objectCalls++;
                return objectResult;
            }

            public void Verify(int cCalls, int goCalls, bool objectResult = false) {
                componentCalls = 0;
                objectCalls = 0;
                this.objectResult = objectResult;

                FbxExporters.Editor.ModelExporter.ExportObject(filename, tree);

                Assert.AreEqual(cCalls, componentCalls);
                Assert.AreEqual(goCalls, objectCalls);
            }
        }

        [Test]
        public void TestExporterCallbacks()
        {
            var tree = CreateHierarchy();
            var tester = new CallbackTester(tree.transform, GetRandomFbxFilePath());
            var n = tree.GetComponentsInChildren<Transform>().Length;

            // No callbacks registered => no calls.
            tester.Verify(0, 0);

            FbxExporters.Editor.ModelExporter.RegisterMeshObjectCallback(tester.CallbackForObject);
            FbxExporters.Editor.ModelExporter.RegisterMeshCallback<FbxPrefab>(tester.CallbackForFbxPrefab);

            // No fbprefab => no component calls, but every object called.
            tester.Verify(0, n);

            // Add a fbxprefab, check every object called and the prefab called.
            tree.transform.Find("Parent1").gameObject.AddComponent<FbxPrefab>();
            tester.Verify(1, n);

            // Make the object report it's replacing everything => no component calls.
            tester.Verify(0, n, objectResult: true);

            // Make sure we can't register for a component twice, but we can
            // for an object.  Register twice for an object means two calls per
            // object.
            Assert.That( () => FbxExporters.Editor.ModelExporter.RegisterMeshCallback<FbxPrefab>(tester.CallbackForFbxPrefab),
                    Throws.Exception);
            FbxExporters.Editor.ModelExporter.RegisterMeshObjectCallback(tester.CallbackForObject);
            tester.Verify(1, 2 * n);

            // Register twice but return true => only one call per object.
            tester.Verify(0, n, objectResult: true);

            // Unregister once => only one call per object, and no more for the prefab.
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback<FbxPrefab>();
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback(tester.CallbackForObject);
            tester.Verify(0, n);

            // Legal to unregister if already unregistered.
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback<FbxPrefab>();
            tester.Verify(0, n);

            // Register same callback twice gets back to original state.
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback(tester.CallbackForObject);
            tester.Verify(0, 0);

            // Legal to unregister if already unregistered.
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback(tester.CallbackForObject);
            tester.Verify(0, 0);

            ///////////////////////
            // Test that the callbacks not only get called, but can replace
            // meshes.
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var sphereMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;

            // Pick on the fbxprefab to output a sphere in place of the cube.
            // the fbxprefab is on parent1.
            string filename;
            GameObject asset;
            Mesh assetMesh;

            FbxExporters.Editor.ModelExporter.GetMeshForComponent<FbxPrefab> prefabCallback =
                (ModelExporter exporter, FbxPrefab component, FbxNode node) => {
                    exporter.ExportMesh(sphereMesh, node);
                    return true;
                };
            FbxExporters.Editor.ModelExporter.RegisterMeshCallback(prefabCallback);
            filename = GetRandomFbxFilePath();
            FbxExporters.Editor.ModelExporter.ExportObject(filename, tree);
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback<FbxPrefab>();

            asset = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            assetMesh = asset.transform.Find("Parent1").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(sphereMesh.triangles.Length, assetMesh.triangles.Length);
            assetMesh = asset.transform.Find("Parent2").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(cubeMesh.triangles.Length, assetMesh.triangles.Length);

            // Try again, but this time pick on Parent2 by name (different just
            // to make sure we don't pass if the previous pass didn't
            // actually unregister).
            filename = GetRandomFbxFilePath();
            FbxExporters.Editor.ModelExporter.GetMeshForObject callback =
                (ModelExporter exporter, GameObject gameObject, FbxNode node) => {
                    if (gameObject.name == "Parent2") {
                        exporter.ExportMesh(sphereMesh, node);
                        return true;
                    } else {
                        return false;
                    }
                };
            FbxExporters.Editor.ModelExporter.RegisterMeshObjectCallback(callback);
            FbxExporters.Editor.ModelExporter.ExportObject(filename, tree);
            FbxExporters.Editor.ModelExporter.UnRegisterMeshCallback(callback);

            asset = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            assetMesh = asset.transform.Find("Parent1").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(cubeMesh.triangles.Length, assetMesh.triangles.Length);
            assetMesh = asset.transform.Find("Parent2").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(sphereMesh.triangles.Length, assetMesh.triangles.Length);
        }

        [Test]
        public void TestExportCamera(){
            // NOTE: even though the aspect ratio is exported,
            //       it does not get imported back into Unity.
            //       Therefore don't modify or check if camera.aspect is the same
            //       after export.

            // create a Unity camera
            GameObject cameraObj = new GameObject("TestCamera");
            Camera camera = cameraObj.AddComponent<Camera> ();

            // change some of the default settings
            camera.orthographic = false;
            camera.fieldOfView = 17.5f;
            camera.nearClipPlane = 1.2f;
            camera.farClipPlane = 1345;

            // export the camera
            string filename = GetRandomFbxFilePath();
            var fbxCamera = ExportCamera (filename, cameraObj);
            CompareCameraValues (camera, fbxCamera);

            // test export orthographic camera
            camera.orthographic = true;
            camera.fieldOfView = 78;
            camera.nearClipPlane = 19;
            camera.farClipPlane = 500.6f;

            fbxCamera = ExportCamera (filename, cameraObj);
            CompareCameraValues (camera, fbxCamera);
            Assert.AreEqual (camera.orthographicSize, fbxCamera.orthographicSize);
        }

        /// <summary>
        /// Exports the camera.
        /// </summary>
        /// <returns>The exported camera.</returns>
        /// <param name="filename">Filename.</param>
        /// <param name="cameraObj">Camera object.</param>
        private Camera ExportCamera(string filename, GameObject cameraObj){
            ModelExporter.ExportObject (filename, cameraObj);

            GameObject fbxObj = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            var fbxCamera = fbxObj.GetComponent<Camera> ();

            Assert.IsNotNull (fbxCamera);
            return fbxCamera;
        }

        private void CompareCameraValues(Camera camera, Camera fbxCamera, float delta=0.001f){
            Assert.AreEqual (camera.orthographic, fbxCamera.orthographic);
            Assert.AreEqual (camera.fieldOfView, fbxCamera.fieldOfView, delta);
            Assert.AreEqual (camera.nearClipPlane, fbxCamera.nearClipPlane, delta);
            Assert.AreEqual (camera.farClipPlane, fbxCamera.farClipPlane, delta);
        }
    }
}
