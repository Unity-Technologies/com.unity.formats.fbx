//#define DEBUG_UNITTEST
using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using Autodesk.Fbx;
using UnityEngine.Formats.Fbx.Exporter;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    public class ModelExporterTest : ExporterTestBase
    {
        [TearDown]
        public override void Term ()
        {
            #if (!DEBUG_UNITTEST)
            base.Term ();
            #endif
        }
        
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

            var afbx = ModelExporter.ConvertToRightHanded(a);
            var bfbx = ModelExporter.ConvertToRightHanded(b);
            Assert.AreEqual(ModelExporter.ConvertToRightHanded(crossLeft), bfbx.CrossProduct(afbx));

            // Test scale conversion. Nothing complicated here...
            var afbxPosition = ModelExporter.ConvertToRightHanded(a, ModelExporter.UnitScaleFactor);
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
                bool result = exporter.ExportMaterial(ModelExporter.DefaultMaterial, fbxScene, fbxNode);
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
            var center = ModelExporter.FindCenter(new GameObject[] { cube, cube1, cube2 });

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
            var result = ModelExporter.RemoveRedundantObjects(new Object[] { root });
            Assert.AreEqual (1, result.Count);
            Assert.IsTrue (result.Contains (root));

            // test set: root, child1
            // expected result: root
            result = ModelExporter.RemoveRedundantObjects(new Object[] { root, child1 });
            Assert.AreEqual (1, result.Count);
            Assert.IsTrue (result.Contains (root));

            // test set: root, child1, child2, root2
            // expected result: root, root2
            result = ModelExporter.RemoveRedundantObjects(new Object[] { root, root2, child2, child1 });
            Assert.AreEqual (2, result.Count);
            Assert.IsTrue (result.Contains (root));
            Assert.IsTrue (result.Contains (root2));

            // test set: child1, child2
            // expected result: child1, child2
            result = ModelExporter.RemoveRedundantObjects(new Object[] { child2, child1 });
            Assert.AreEqual (2, result.Count);
            Assert.IsTrue (result.Contains (child1));
            Assert.IsTrue (result.Contains (child2));
        }

        [Test]
        public void TestConvertToValidFilename()
        {
            // test already valid filenames
            var filename = "foobar.fbx";
            var result = ModelExporter.ConvertToValidFilename(filename);
            Assert.AreEqual (filename, result);

            filename = "foo_bar 1.fbx";
            result = ModelExporter.ConvertToValidFilename(filename);
            Assert.AreEqual (filename, result);

            // test invalid filenames
            filename = "?foo**bar///.fbx";
            result = ModelExporter.ConvertToValidFilename(filename);
#if UNITY_EDITOR_WIN
            Assert.AreEqual ("_foo__bar___.fbx", result);
#else
            Assert.AreEqual ("?foo**bar___.fbx", result);
#endif

            filename = "foo$?ba%r 2.fbx";
            result = ModelExporter.ConvertToValidFilename(filename);
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

                ModelExporter.ExportObject(filename, tree);

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

            ModelExporter.RegisterMeshObjectCallback(tester.CallbackForObject);
            ModelExporter.RegisterMeshCallback<FbxPrefab>(tester.CallbackForFbxPrefab);

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
            Assert.That( () => ModelExporter.RegisterMeshCallback<FbxPrefab>(tester.CallbackForFbxPrefab),
                    Throws.Exception);
            ModelExporter.RegisterMeshObjectCallback(tester.CallbackForObject);
            tester.Verify(1, 2 * n);

            // Register twice but return true => only one call per object.
            tester.Verify(0, n, objectResult: true);

            // Unregister once => only one call per object, and no more for the prefab.
            ModelExporter.UnRegisterMeshCallback<FbxPrefab>();
            ModelExporter.UnRegisterMeshCallback(tester.CallbackForObject);
            tester.Verify(0, n);

            // Legal to unregister if already unregistered.
            ModelExporter.UnRegisterMeshCallback<FbxPrefab>();
            tester.Verify(0, n);

            // Register same callback twice gets back to original state.
            ModelExporter.UnRegisterMeshCallback(tester.CallbackForObject);
            tester.Verify(0, 0);

            // Legal to unregister if already unregistered.
            ModelExporter.UnRegisterMeshCallback(tester.CallbackForObject);
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

            GetMeshForComponent<FbxPrefab> prefabCallback =
                (ModelExporter exporter, FbxPrefab component, FbxNode node) => {
                    exporter.ExportMesh(sphereMesh, node);
                    return true;
                };
            ModelExporter.RegisterMeshCallback(prefabCallback);
            filename = GetRandomFbxFilePath();
            ModelExporter.ExportObject(filename, tree);
            ModelExporter.UnRegisterMeshCallback<FbxPrefab>();

            asset = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            assetMesh = asset.transform.Find("Parent1").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(sphereMesh.triangles.Length, assetMesh.triangles.Length);
            assetMesh = asset.transform.Find("Parent2").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(cubeMesh.triangles.Length, assetMesh.triangles.Length);

            // Try again, but this time pick on Parent2 by name (different just
            // to make sure we don't pass if the previous pass didn't
            // actually unregister).
            filename = GetRandomFbxFilePath();
            GetMeshForObject callback =
                (ModelExporter exporter, GameObject gameObject, FbxNode node) => {
                    if (gameObject.name == "Parent2") {
                        exporter.ExportMesh(sphereMesh, node);
                        return true;
                    } else {
                        return false;
                    }
                };
            ModelExporter.RegisterMeshObjectCallback(callback);
            ModelExporter.ExportObject(filename, tree);
            ModelExporter.UnRegisterMeshCallback(callback);

            asset = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            assetMesh = asset.transform.Find("Parent1").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(cubeMesh.triangles.Length, assetMesh.triangles.Length);
            assetMesh = asset.transform.Find("Parent2").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(sphereMesh.triangles.Length, assetMesh.triangles.Length);
        }

        [Test]
        [Ignore("Ignore a camera orthographic test (Uni-48092)")]        
        public void TestExportCamera2(){
            // NOTE: even though the aspect ratio is exported,
            //       it does not get imported back into Unity.
            //       Therefore don't modify or check if camera.aspect is the same
            //       after export.

            // create a Unity camera
            GameObject cameraObj = new GameObject("TestCamera");
            Camera camera = cameraObj.AddComponent<Camera> ();

            // test export orthographic camera
            camera.orthographic = true;
            camera.fieldOfView = 78;
            camera.nearClipPlane = 19;
            camera.farClipPlane = 500.6f;

            var filename = GetRandomFbxFilePath (); // export to a different file
            var fbxCamera = ExportCamera (filename, cameraObj);
            CompareCameraValues (camera, fbxCamera);
            Assert.AreEqual (camera.orthographicSize, fbxCamera.orthographicSize);
        }

        [Test]
        public void TestExportCamera1(){
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
        }

        /// <summary>
        /// Exports the camera.
        /// </summary>
        /// <returns>The exported camera.</returns>
        /// <param name="filename">Filename.</param>
        /// <param name="cameraObj">Camera object.</param>
        private Camera ExportCamera(string filename, GameObject cameraObj){
            return ExportComponent<Camera> (filename, cameraObj);
        }

        /// <summary>
        /// Exports the GameObject and returns component of type T.
        /// </summary>
        /// <returns>The component.</returns>
        /// <param name="filename">Filename.</param>
        /// <param name="obj">Object.</param>
        /// <typeparam name="T">The component type.</typeparam>
        private T ExportComponent<T>(string filename, GameObject obj) where T : Component {
            ModelExporter.ExportObject (filename, obj);

            GameObject fbxObj = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            var fbxComponent = fbxObj.GetComponent<T> ();

            Assert.IsNotNull (fbxComponent);
            return fbxComponent;
        }

        private void CompareCameraValues(Camera camera, Camera fbxCamera, float delta=0.001f){
            Assert.AreEqual (camera.orthographic, fbxCamera.orthographic);
            Assert.AreEqual (camera.fieldOfView, fbxCamera.fieldOfView, delta);
            Assert.AreEqual (camera.nearClipPlane, fbxCamera.nearClipPlane, delta);
            Assert.AreEqual (camera.farClipPlane, fbxCamera.farClipPlane, delta);
        }

        [Test]
        public void TestExportLight()
        {
            // create a Unity light
            GameObject lightObj = new GameObject("TestLight");
            Light light = lightObj.AddComponent<Light> ();

            light.type = LightType.Spot;
            light.spotAngle = 55.4f;
            light.color = Color.blue;
            light.intensity = 2.3f;
            light.range = 45;
            light.shadows = LightShadows.Soft;

            string filename = GetRandomFbxFilePath ();
            var fbxLight = ExportComponent<Light> (filename, lightObj);
            CompareLightValues (light, fbxLight);

            light.type = LightType.Point;
            light.color = Color.red;
            light.intensity = 0.4f;
            light.range = 120;
            light.shadows = LightShadows.Hard;

            filename = GetRandomFbxFilePath ();
            fbxLight = ExportComponent<Light> (filename, lightObj);
            CompareLightValues (light, fbxLight);
        }

        private void CompareLightValues(Light light, Light fbxLight, float delta=0.001f){
            Assert.AreEqual (light.type, fbxLight.type);
            if (light.type == LightType.Spot) {
                Assert.AreEqual (light.spotAngle, fbxLight.spotAngle, delta);
            }
            Assert.AreEqual (light.color, fbxLight.color);
            Assert.AreEqual (light.intensity, fbxLight.intensity, delta);
            Assert.AreEqual (light.range, fbxLight.range, delta);

            // compare shadows
            // make sure that if we exported without shadows, don't import with shadows
            if (light.shadows == LightShadows.None) {
                Assert.AreEqual (LightShadows.None, fbxLight.shadows);
            } else {
                Assert.AreNotEqual (LightShadows.None, fbxLight.shadows);
            }

            Assert.IsTrue (light.transform.rotation == fbxLight.transform.rotation);
        }

        [Test]
        public void TestComponentAttributeExport()
        {
            // test exporting of normals, tangents, uvs, and vertex colors
            // Note: won't test binormals as they are not imported into Unity

            var quad = GameObject.CreatePrimitive (PrimitiveType.Quad);
            var quadMeshFilter = quad.GetComponent<MeshFilter> ();
            var quadMesh = quadMeshFilter.sharedMesh;

            // create a simple mesh (just a quad)
            // this is to make sure we don't accidentally modify the
            // Unity internal Quad primitive.
            var mesh = new Mesh();
            mesh.name = "Test";

            mesh.vertices = quadMesh.vertices;
            mesh.triangles = quadMesh.triangles;
            mesh.tangents = quadMesh.tangents;
            mesh.normals = quadMesh.normals;
            mesh.colors = quadMesh.colors;

            Assert.IsNotNull (mesh.tangents);
            Assert.IsNotNull (mesh.vertices);
            Assert.IsNotNull (mesh.triangles);
            Assert.IsNotNull (mesh.normals);
            Assert.IsNotNull (mesh.colors);

            var gameObject = new GameObject ();
            var meshFilter = gameObject.AddComponent<MeshFilter> ();
            gameObject.AddComponent<MeshRenderer> ();

            meshFilter.sharedMesh = mesh;

            // don't need quad anymore
            Object.DestroyImmediate(quad);

            // try exporting default values
            string filename = GetRandomFbxFilePath();
            var fbxMeshFilter = ExportComponent<MeshFilter> (filename, gameObject);
            var fbxMesh = fbxMeshFilter.sharedMesh;
            CompareMeshComponentAttributes (mesh, fbxMesh);

            // try exporting mesh without vertex colors
            mesh.colors = new Color[]{ };

            filename = GetRandomFbxFilePath();
            fbxMeshFilter = ExportComponent<MeshFilter> (filename, gameObject);
            fbxMesh = fbxMeshFilter.sharedMesh;
            CompareMeshComponentAttributes (mesh, fbxMesh);

            Object.DestroyImmediate (mesh);
        }

        private void CompareMeshComponentAttributes(Mesh mesh, Mesh fbxMesh)
        {
            Assert.IsNotNull (fbxMesh);
            Assert.IsNotNull (fbxMesh.vertices);
            Assert.IsNotNull (fbxMesh.triangles);
            Assert.IsNotNull (fbxMesh.normals);
            Assert.IsNotNull (fbxMesh.colors);
            Assert.IsNotNull (fbxMesh.tangents);

            Assert.AreEqual (mesh.vertices, fbxMesh.vertices);
            Assert.AreEqual (mesh.triangles, fbxMesh.triangles);
            Assert.AreEqual (mesh.normals, fbxMesh.normals);
            Assert.AreEqual (mesh.colors, fbxMesh.colors);
            Assert.AreEqual (mesh.tangents, fbxMesh.tangents);
        }

        private void ExportSkinnedMesh(string fileToExport, out SkinnedMeshRenderer originalSkinnedMesh, out SkinnedMeshRenderer exportedSkinnedMesh){
            // add fbx to scene
            GameObject originalFbxObj = AssetDatabase.LoadMainAssetAtPath(fileToExport) as GameObject;
            Assert.IsNotNull (originalFbxObj);
            GameObject originalGO = GameObject.Instantiate (originalFbxObj);
            Assert.IsTrue (originalGO);

            // export fbx
            // get GameObject
            string filename = GetRandomFbxFilePath();
            ModelExporter.ExportObject (filename, originalGO);
            GameObject fbxObj = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            Assert.IsTrue (fbxObj);

            originalSkinnedMesh = originalGO.GetComponentInChildren<SkinnedMeshRenderer> ();
            Assert.IsNotNull (originalSkinnedMesh);

            exportedSkinnedMesh = fbxObj.GetComponentInChildren<SkinnedMeshRenderer> ();
            Assert.IsNotNull (exportedSkinnedMesh);
        }

        public class SkinnedMeshTestDataClass
        {
            public static System.Collections.IEnumerable TestCases1 {
                get {
                    // for now use this cowboy taken from the asset store as the test file
                    // TODO: find a better/simpler test file
                    yield return "Models/Cowboy/cowboyMidPoly(riged).fbx";
                }
            }
            public static System.Collections.IEnumerable TestCases2 {
                get {
                    yield return "Models/SimpleMan/SimpleMan.fbx";
                }
            }
        }

        [Test, TestCaseSource(typeof(SkinnedMeshTestDataClass), "TestCases1")]
        public void TestSkinnedMeshExport(string fbxPath){
            fbxPath = FindPathInUnitTests (fbxPath);
            Assert.That (fbxPath, Is.Not.Null);

            SkinnedMeshRenderer originalSkinnedMesh, exportedSkinnedMesh;
            ExportSkinnedMesh (fbxPath, out originalSkinnedMesh, out exportedSkinnedMesh);

            Assert.IsTrue (originalSkinnedMesh.name == exportedSkinnedMesh.name ||
                (originalSkinnedMesh.transform.parent == null && exportedSkinnedMesh.transform.parent == null));

            // check if skeletons match
            // compare bones
            var originalBones = originalSkinnedMesh.bones;
            var exportedBones = exportedSkinnedMesh.bones;

            Assert.IsNotNull (originalBones);
            Assert.IsNotNull (exportedBones);

            Assert.AreEqual (originalBones.Length, exportedBones.Length);

            for(int i = 0; i < originalBones.Length; i++){
                var originalBone = originalBones [i];
                var exportedBone = exportedBones [i];

                Assert.AreEqual (originalBone.name, exportedBone.name);
                Assert.AreEqual (originalBone.parent, exportedBone.parent);

                // NOTE: not comparing transforms as the exported transforms are taken from
                //       the bind pose whereas the originals are not necessarily.
            }

            // compare bind poses
            var origMesh = originalSkinnedMesh.sharedMesh;
            Assert.IsNotNull (origMesh);
            var exportedMesh = exportedSkinnedMesh.sharedMesh;
            Assert.IsNotNull (exportedMesh);

            var origBindposes = origMesh.bindposes;
            Assert.IsNotNull (origBindposes);
            var exportedBindposes = exportedMesh.bindposes;
            Assert.IsNotNull (exportedBindposes);

            Assert.That(origBindposes.Length == exportedBindposes.Length);

            for (int i = 0; i < origBindposes.Length; i++) {
                var origBp = origBindposes [i];
                var expBp = exportedBindposes [i];

				// TODO: (UNI-34293) fix so bones with negative scale export with correct bind pose
				if (originalBones [i].name == "EyeL") {
					continue;
				}

                for (int j = 0; j < 4; j++) {
                    for (int k = 0; k < 4; k++) {
                        Assert.That (origBp.GetColumn (j)[k], Is.EqualTo(expBp.GetColumn (j)[k]).Within(0.001f), string.Format("bind pose doesn't match {0},{1}", j, k));
                    }
                }
            }

            // TODO: find a way to compare bone weights.
            //       The boneweights are by vertex, and duplicate vertices
            //       are removed on export so the lists are not necessarily
            //       the same length or order.
            var origWeights = origMesh.boneWeights;
            Assert.IsNotNull (origWeights);
            var expWeights = exportedMesh.boneWeights;
            Assert.IsNotNull (expWeights);
        }

        [Test, TestCaseSource(typeof(SkinnedMeshTestDataClass), "TestCases2")]
        public void TestBoneWeightExport(string fbxPath){
            fbxPath = FindPathInUnitTests (fbxPath);
            Assert.That (fbxPath, Is.Not.Null);

            SkinnedMeshRenderer originalSkinnedMesh, exportedSkinnedMesh;
            ExportSkinnedMesh (fbxPath, out originalSkinnedMesh, out exportedSkinnedMesh);

            var origVerts = originalSkinnedMesh.sharedMesh.vertices;
            Assert.That (origVerts, Is.Not.Null);

            var expVerts = exportedSkinnedMesh.sharedMesh.vertices;
            Assert.That (expVerts, Is.Not.Null);

            var origBoneWeights = originalSkinnedMesh.sharedMesh.boneWeights;
            Assert.That (origBoneWeights, Is.Not.Null);
            Assert.That (origBoneWeights.Length, Is.GreaterThan (0));

            var expBoneWeights = exportedSkinnedMesh.sharedMesh.boneWeights;
            Assert.That (expBoneWeights, Is.Not.Null);
            Assert.That (expBoneWeights.Length, Is.GreaterThan (0));

            var origBones = originalSkinnedMesh.bones;
            var expBones = exportedSkinnedMesh.bones;

            int comparisonCount = 0;
            int minVertCount = Mathf.Min (origVerts.Length, expVerts.Length);
            for(int i = 0; i < minVertCount; i++){
                for (int j = 0; j < minVertCount; j++) {
                    if (origVerts [i] == expVerts [j]) {
                        // compare bone weights
                        var origBw = origBoneWeights[i];
                        var expBw = expBoneWeights [j];

                        var indexMsg = "Bone index {0} doesn't match";
                        var nameMsg = "bone names don't match";

                        Assert.That (expBw.boneIndex0, Is.EqualTo (origBw.boneIndex0), string.Format(indexMsg, 0));
                        Assert.That (expBones[expBw.boneIndex0].name, Is.EqualTo (origBones[origBw.boneIndex0].name), nameMsg);

                        Assert.That (expBw.boneIndex1, Is.EqualTo (origBw.boneIndex1), string.Format(indexMsg, 1));
                        Assert.That (expBones[expBw.boneIndex1].name, Is.EqualTo (origBones[origBw.boneIndex1].name), nameMsg);

                        Assert.That (expBw.boneIndex2, Is.EqualTo (origBw.boneIndex2), string.Format(indexMsg, 2));
                        Assert.That (expBones[expBw.boneIndex2].name, Is.EqualTo (origBones[origBw.boneIndex2].name), nameMsg);

                        Assert.That (expBw.boneIndex3, Is.EqualTo (origBw.boneIndex3), string.Format(indexMsg, 3));
                        Assert.That (expBones[expBw.boneIndex3].name, Is.EqualTo (origBones[origBw.boneIndex3].name), nameMsg);

                        var message = "Bone weight {0} doesn't match";
                        Assert.That (expBw.weight0, Is.EqualTo (origBw.weight0).Within(0.001f), string.Format(message, 0));
                        Assert.That (expBw.weight1, Is.EqualTo (origBw.weight1).Within(0.001f), string.Format(message, 1));
                        Assert.That (expBw.weight2, Is.EqualTo (origBw.weight2).Within(0.001f), string.Format(message, 2));
                        Assert.That (expBw.weight3, Is.EqualTo (origBw.weight3).Within(0.001f), string.Format(message, 3));

                        comparisonCount++;
                        break;
                    }
                }
            }
            Debug.LogWarningFormat ("Compared {0} out of a possible {1} bone weights", comparisonCount, minVertCount);
        }


        public class Vector3Comparer : IComparer<Vector3>
        {
            public int Compare(Vector3 a, Vector3 b)
            {
                Assert.That(a.x, Is.EqualTo(b.x).Within(0.00001f));
                Assert.That(a.y, Is.EqualTo(b.y).Within(0.00001f));
                Assert.That(a.z, Is.EqualTo(b.z).Within(0.00001f));
                return 0;  // we're almost equal
            }
        }

        [Test, TestCaseSource(typeof(AnimationTestDataClass), "BlendShapeTestCases")]
        public void TestBlendShapeExport(string fbxPath)
        {
            fbxPath = FindPathInUnitTests (fbxPath);
            Assert.That (fbxPath, Is.Not.Null);

            SkinnedMeshRenderer originalSMR, exportedSMR;
            ExportSkinnedMesh (fbxPath, out originalSMR, out exportedSMR);

            var originalMesh = originalSMR.sharedMesh;
            var exportedMesh = exportedSMR.sharedMesh;
            Assert.IsNotNull(originalMesh);
            Assert.IsNotNull(exportedMesh);

            // compare blend shape data
            Assert.AreNotEqual(originalMesh.blendShapeCount, 0);
            Assert.AreEqual(originalMesh.blendShapeCount, exportedMesh.blendShapeCount);
            {
                var deltaVertices = new Vector3[originalMesh.vertexCount];
                var deltaNormals = new Vector3[originalMesh.vertexCount];
                var deltaTangents = new Vector3[originalMesh.vertexCount];
                var fbxDeltaVertices = new Vector3[originalMesh.vertexCount];
                var fbxDeltaNormals = new Vector3[originalMesh.vertexCount];
                var fbxDeltaTangents = new Vector3[originalMesh.vertexCount];

                for (int bi = 0; bi < originalMesh.blendShapeCount; ++bi)
                {
                    Assert.AreEqual(originalMesh.GetBlendShapeName(bi), exportedMesh.GetBlendShapeName(bi));
                    Assert.AreEqual(originalMesh.GetBlendShapeFrameCount(bi), exportedMesh.GetBlendShapeFrameCount(bi));

                    int frameCount = originalMesh.GetBlendShapeFrameCount(bi);
                    for (int fi = 0; fi < frameCount; ++fi)
                    {
                        Assert.AreEqual(originalMesh.GetBlendShapeFrameWeight(bi, fi), exportedMesh.GetBlendShapeFrameWeight(bi, fi));

                        originalMesh.GetBlendShapeFrameVertices(bi, fi, deltaVertices, deltaNormals, deltaTangents);
                        exportedMesh.GetBlendShapeFrameVertices(bi, fi, fbxDeltaVertices, fbxDeltaNormals, fbxDeltaTangents);

                        var v3comparer = new Vector3Comparer();
                        Assert.That(deltaVertices, Is.EqualTo(fbxDeltaVertices).Using<Vector3>(v3comparer), string.Format("delta vertices don't match"));
                        Assert.That(deltaNormals, Is.EqualTo(fbxDeltaNormals).Using<Vector3>(v3comparer), string.Format("delta normals don't match"));
                        Assert.That(deltaTangents, Is.EqualTo(fbxDeltaTangents).Using<Vector3>(v3comparer), string.Format("delta tangents don't match"));

                    }
                }
            }
        }

        [Test, TestCaseSource(typeof(AnimationTestDataClass), "ColorBlendShapeCases")]
        public void TestBlendShapeColor(string fbxPath)
        {
            fbxPath = FindPathInUnitTests (fbxPath);
            Assert.That (fbxPath, Is.Not.Null);
        }

        [Test, TestCaseSource(typeof(AnimationTestDataClass), "VertexNormalBlendShapeCases")]
        public void TestBlendShapeVertexNormals(string fbxPath)
        {
            fbxPath = FindPathInUnitTests (fbxPath);
            Assert.That (fbxPath, Is.Not.Null);
        }

        [Test]
        public void LODExportTest(){
            // Create the following test hierarchy:
            //  LODGroup
            //  -- Sphere_LOD0
            //  -- Capsule_LOD0
            //  -- Cube_LOD2
            //  Cylinder_LOD1
            //
            // where sphere + capsule renderers are both in LOD0, and cylinder is in LOD1
            // but not parented under the LOD group

            var lodGroup = new GameObject ("LODGroup");
            var sphereLOD0 = GameObject.CreatePrimitive (PrimitiveType.Sphere);
            sphereLOD0.name = "Sphere_LOD0";
            var capsuleLOD0 = GameObject.CreatePrimitive (PrimitiveType.Capsule);
            capsuleLOD0.name = "Capsule_LOD0";
            var cubeLOD2 = GameObject.CreatePrimitive (PrimitiveType.Cube);
            cubeLOD2.name = "Cube_LOD2";
            var cylinderLOD1 = GameObject.CreatePrimitive (PrimitiveType.Cylinder);
            cylinderLOD1.name = "Cylinder_LOD1";

            sphereLOD0.transform.SetParent (lodGroup.transform);
            capsuleLOD0.transform.SetParent (lodGroup.transform);
            cubeLOD2.transform.SetParent (lodGroup.transform);
            cylinderLOD1.transform.SetParent (null);

            // add LOD group
            var lodGroupComp = lodGroup.AddComponent<LODGroup>();
            Assert.That (lodGroupComp, Is.Not.Null);

            LOD[] lods = new LOD[3];
            lods [0] = new LOD (1, new Renderer[]{ sphereLOD0.GetComponent<Renderer>(), capsuleLOD0.GetComponent<Renderer>() });
            lods [1] = new LOD (0.75f, new Renderer[] { cylinderLOD1.GetComponent<Renderer>() });
            lods [2] = new LOD (0.5f, new Renderer[] { cubeLOD2.GetComponent<Renderer>() });
            lodGroupComp.SetLODs (lods);
            lodGroupComp.RecalculateBounds ();

            // test export all
            // expected LODs exported: Sphere_LOD0, Capsule_LOD0, Cube_LOD2
            GameObject fbxObj = ExportToFbx(lodGroup, lodExportType:ExportSettings.LODExportType.All);
            Assert.IsTrue (fbxObj);

            HashSet<string> expectedChildren = new HashSet<string> () { sphereLOD0.name, capsuleLOD0.name, cubeLOD2.name };
            CompareGameObjectChildren (fbxObj, expectedChildren);

            // test export highest
            // expected LODs exported: Sphere_LOD0, Capsule_LOD0
            fbxObj = ExportToFbx(lodGroup, lodExportType:ExportSettings.LODExportType.Highest);
            Assert.IsTrue (fbxObj);

            expectedChildren = new HashSet<string> () { sphereLOD0.name, capsuleLOD0.name };
            CompareGameObjectChildren (fbxObj, expectedChildren);

            // test export lowest
            // expected LODs exported: Cube_LOD2
            fbxObj = ExportToFbx(lodGroup, lodExportType:ExportSettings.LODExportType.Lowest);
            Assert.IsTrue (fbxObj);

            expectedChildren = new HashSet<string> () { cubeLOD2.name };
            CompareGameObjectChildren (fbxObj, expectedChildren);

            // test convert to prefab
            // this should have the same result as "export all"
            // expected LODs exported: Sphere_LOD0, Capsule_LOD0, Cube_LOD2
            // NOTE: Cylinder_LOD1 is not exported as it is not under the LODGroup hierarchy being exported
            var convertedHierarchy = ConvertToModel.Convert(lodGroup,
                    fbxFullPath: GetRandomFbxFilePath(),
                    prefabFullPath: GetRandomPrefabAssetPath());
            Assert.That (convertedHierarchy, Is.Not.Null);

            // check both converted hierarchy and fbx
            expectedChildren = new HashSet<string> () { sphereLOD0.name, capsuleLOD0.name, cubeLOD2.name };
            CompareGameObjectChildren (convertedHierarchy, expectedChildren);

            fbxObj = convertedHierarchy.GetComponent<FbxPrefab>().FbxModel;
            Assert.IsTrue (fbxObj);

            expectedChildren = new HashSet<string> () { sphereLOD0.name, capsuleLOD0.name, cubeLOD2.name };
            CompareGameObjectChildren (fbxObj, expectedChildren);
        }


        /// <summary>
        /// Compares obj's children to the expected children in the hashset.
        /// Doesn't recurse through the children.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="expectedChildren">Expected children.</param>
        private void CompareGameObjectChildren(GameObject obj, HashSet<string> expectedChildren){
            Assert.That (obj.transform.childCount, Is.EqualTo (expectedChildren.Count));

            foreach (Transform child in obj.transform) {
                Assert.That (expectedChildren.Contains (child.name));
                expectedChildren.Remove (child.name);
            }
        }
    }
}
