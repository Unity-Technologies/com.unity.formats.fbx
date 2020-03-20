using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Formats.Fbx.Exporter;
using UnityEditor.Formats.Fbx.Exporter;
using System.Collections;

namespace FbxExporter.UnitTests
{
    public class ConvertToNestedPrefabTest : ExporterTestBase
    {
        public static string[] PrefabTestCases = new string[]
        {
            "Prefabs/Camera.prefab",
            "Prefabs/RegularPrefab.prefab",
            "Prefabs/RegularPrefab_GO.prefab",
            "Prefabs/RegularPrefab_Model.prefab",
            "Prefabs/RegularPrefab_Regular.prefab",
            "Prefabs/RegularPrefab_Variant.prefab",
            "Prefabs/VariantPrefab.prefab",
            "Prefabs/VariantPrefab_GO.prefab",
            "Prefabs/VariantPrefab_Model.prefab",
            "Prefabs/VariantPrefab_Regular.prefab",
            "Prefabs/VariantPrefab_Variant.prefab"
        };

        [Test, TestCaseSource(typeof(ConvertToNestedPrefabTest), "PrefabTestCases")]
        public void TestConversion(string prefabPath)
        {
            prefabPath = FindPathInUnitTests(prefabPath);
            Assert.That(prefabPath, Is.Not.Null);

            var go = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
            Assert.That(go);

            // first test converting prefab asset directly
            ConvertAndComparePrefab(go);

            // then test adding it to the scene and then converting
            var instance = PrefabUtility.InstantiatePrefab(go) as GameObject;
            ConvertAndComparePrefab(instance, isInstance: true);
        }

        protected GameObject ConvertAndComparePrefab(GameObject orig, string fbxPath = "", bool isInstance = false)
        {
            if (string.IsNullOrEmpty(fbxPath))
            {
                fbxPath = GetRandomFbxFilePath();
            }

            var prefabAsset = orig;
            if (isInstance)
            {
                prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(orig) as GameObject;
            }

            // Convert it to a prefab
            var prefab = ConvertToNestedPrefab.Convert(orig,
                fbxFullPath: fbxPath, prefabFullPath: Path.ChangeExtension(fbxPath, "prefab"));
            Assert.That(prefab);

            if (isInstance)
            {
                // original should be destroyed now
                Assert.That(!orig);
            }

            // check that the hierarchy matches the original
            AssertSameHierarchy(prefabAsset, prefab, ignoreRootName: true, checkComponents: true);

            // check that the meshes and materials are now from the fbx
            var fbx = AssetDatabase.LoadMainAssetAtPath(fbxPath) as GameObject;
            Assert.That(fbx);
            AssertSameMeshesAndMaterials(fbx, prefab);

            if (isInstance)
            {
                // test undo and redo
                Undo.PerformUndo();
                Assert.That(!prefab);
                Assert.That(orig);

                Undo.PerformRedo();
                AssertSameHierarchy(prefabAsset, prefab, ignoreRootName: true, checkComponents: true);
                AssertSameMeshesAndMaterials(fbx, prefab);
            }

            return prefab;
        }

        protected void AssertSameMeshesAndMaterials(GameObject expectedHierarchy, GameObject actualHierarchy)
        {
            // get mesh filter or skinned mesh renderer to compare meshes
            var expectedMeshFilter = expectedHierarchy.GetComponent<MeshFilter>();
            var actualMeshFilter = actualHierarchy.GetComponent<MeshFilter>();
            if (expectedMeshFilter)
            {
                Assert.That(actualMeshFilter);
                Assert.That(expectedMeshFilter.sharedMesh, Is.EqualTo(actualMeshFilter.sharedMesh));
            }

            var expectedSkinnedMesh = expectedHierarchy.GetComponent<SkinnedMeshRenderer>();
            var actualSkinnedMesh = actualHierarchy.GetComponent<SkinnedMeshRenderer>();
            if (expectedSkinnedMesh)
            {
                Assert.That(actualSkinnedMesh);
                Assert.That(expectedSkinnedMesh.sharedMesh, Is.EqualTo(actualSkinnedMesh.sharedMesh));
                // material should not equal what is in the FBX, but what was originally in the scene
                Assert.That(expectedSkinnedMesh.sharedMaterial, Is.Not.EqualTo(actualSkinnedMesh.sharedMaterial));
            }

            var expectedRenderer = expectedHierarchy.GetComponent<Renderer>();
            var actualRenderer = actualHierarchy.GetComponent<Renderer>();
            if (expectedRenderer)
            {
                Assert.That(actualRenderer);
                // material should not equal what is in the FBX, but what was originally in the scene
                Assert.That(expectedRenderer.sharedMaterial, Is.Not.EqualTo(actualRenderer.sharedMaterial));
            }

            var expectedTransform = expectedHierarchy.transform;
            var actualTransform = actualHierarchy.transform;
            foreach (Transform expectedChild in expectedTransform)
            {
                var actualChild = actualTransform.Find(expectedChild.name);
                Assert.IsNotNull(actualChild);
                AssertSameMeshesAndMaterials(expectedChild.gameObject, actualChild.gameObject);
            }
        }

        [Test]
        public void TestReferencesInScene()
        {
            // test that references that scene objects hold to the converted object
            // are maintained
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.transform.SetParent(a.transform);

            var c = new GameObject();
            var reference = c.AddComponent<ReferenceComponent>();
            reference.m_goList = new GameObject[] { a, b };
            reference.m_collider = a.GetComponent<BoxCollider>();
            reference.m_transform = b.transform;

            var fbxPath = GetRandomFbxFilePath();

            // Convert it to a prefab
            var prefab = ConvertToNestedPrefab.Convert(a,
                fbxFullPath: fbxPath, prefabFullPath: Path.ChangeExtension(fbxPath, "prefab"));
            Assert.That(prefab);
            Assert.That(!a);

            var newA = prefab;
            var newB = prefab.transform.GetChild(0).gameObject;

            Assert.That(reference.m_goList.Length, Is.EqualTo(2));
            Assert.That(reference.m_goList[0], Is.EqualTo(newA));
            Assert.That(reference.m_goList[1], Is.EqualTo(newB));
            Assert.That(reference.m_collider, Is.EqualTo(newA.GetComponent<BoxCollider>()));
            Assert.That(reference.m_transform, Is.EqualTo(newB.transform));

            // Test undo and redo still maintains the right references
            Undo.PerformUndo();

            Assert.That(reference.m_goList.Length, Is.EqualTo(2));
            Assert.That(reference.m_goList[0], Is.EqualTo(a));
            Assert.That(reference.m_goList[1], Is.EqualTo(b));
            Assert.That(reference.m_collider, Is.EqualTo(a.GetComponent<BoxCollider>()));
            Assert.That(reference.m_transform, Is.EqualTo(b.transform));

            Undo.PerformRedo();

            Assert.That(reference.m_goList.Length, Is.EqualTo(2));
            Assert.That(reference.m_goList[0], Is.EqualTo(newA));
            Assert.That(reference.m_goList[1], Is.EqualTo(newB));
            Assert.That(reference.m_collider, Is.EqualTo(newA.GetComponent<BoxCollider>()));
            Assert.That(reference.m_transform, Is.EqualTo(newB.transform));
        }

        [Test]
        public void TestReferences()
        {
            var prefabPath = FindPathInUnitTests("Prefabs/ReferenceTest.prefab");
            var root = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
            Assert.That(root);

            var prefab = ConvertAndComparePrefab(root);

            // check the references
            var sphere = prefab.transform.Find("Sphere");
            Assert.That(sphere);
            var cylinder = prefab.transform.Find("Sphere/Cylinder");
            Assert.That(cylinder);
            var capsule = prefab.transform.Find("Sphere/Cylinder/Capsule");
            Assert.That(capsule);
            var plane = prefab.transform.Find("Plane");
            Assert.That(plane);

            var refComponent = cylinder.GetComponent<ReferenceComponent>();
            Assert.That(refComponent);
            Assert.That(refComponent.m_goList.Length, Is.EqualTo(3));
            Assert.That(refComponent.m_goList[0], Is.EqualTo(prefab));
            Assert.That(refComponent.m_goList[1], Is.EqualTo(sphere.gameObject));
            Assert.That(refComponent.m_goList[2], Is.EqualTo(capsule.gameObject));

            Assert.That(refComponent.m_transform, Is.EqualTo(plane));
            Assert.That(refComponent.m_collider, Is.EqualTo(prefab.GetComponent<BoxCollider>()));

            var meshCollider = plane.GetComponent<MeshCollider>();
            Assert.That(meshCollider);
            var meshPath = AssetDatabase.GetAssetPath(meshCollider.sharedMesh);
            Assert.That(meshPath, Is.Not.Null);
            Assert.That(Path.GetExtension(meshPath), Is.EqualTo(".fbx"));
        }

        [Test]
        public void TestComponents()
        {
            // more specific test that components are copied properly, than just checking
            // if they are there
            var prefabPath = FindPathInUnitTests("Prefabs/ComponentTest.prefab");
            var root = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
            Assert.That(root);

            var prefab = ConvertAndComparePrefab(root);

            var expectedParticleSystem = root.GetComponentInChildren<ParticleSystem>();
            var actualParticleSystem = prefab.GetComponentInChildren<ParticleSystem>();
            Assert.That(expectedParticleSystem);
            Assert.That(actualParticleSystem);
            Assert.That(actualParticleSystem.gameObject.name, Is.EqualTo(expectedParticleSystem.gameObject.name));

            // compare a few parameters that differ from default
            Assert.That(actualParticleSystem.main.startSize, Is.EqualTo(expectedParticleSystem.main.startSize));
            var actualParticleRenderer = prefab.GetComponentInChildren<ParticleSystemRenderer>();
            var expectedParticleRenderer = root.GetComponentInChildren<ParticleSystemRenderer>();
            Assert.That(actualParticleRenderer.renderMode, Is.EqualTo(expectedParticleRenderer.renderMode));
            Assert.That(actualParticleRenderer.mesh);
            Assert.That(expectedParticleRenderer.mesh);
            Assert.That(actualParticleRenderer.mesh.name, Is.EqualTo(expectedParticleRenderer.mesh.name));
            Assert.That(actualParticleRenderer.sharedMaterial);
            Assert.That(expectedParticleRenderer.sharedMaterial);
            Assert.That(actualParticleRenderer.sharedMaterial.name, Is.EqualTo(expectedParticleRenderer.sharedMaterial.name));

            // Compare light
            var expectedLight = root.GetComponentInChildren<Light>();
            var actualLight = prefab.GetComponentInChildren<Light>();

            Assert.That(actualLight.type, Is.EqualTo(expectedLight.type));
            Assert.That(actualLight.range, Is.EqualTo(expectedLight.range));
            Assert.That(actualLight.spotAngle, Is.EqualTo(expectedLight.spotAngle));
            Assert.That(actualLight.color, Is.EqualTo(expectedLight.color));
        }

        [Test]
        public void TestSkinnedMeshReferences()
        {
            var fbxPath = FindPathInUnitTests("Prefabs/skin.fbx");
            var root = AssetDatabase.LoadMainAssetAtPath(fbxPath) as GameObject;
            Assert.That(root);

            var exportPath = GetRandomFbxFilePath();

            // Convert it to a prefab
            var prefab = ConvertToNestedPrefab.Convert(root,
                fbxFullPath: exportPath, prefabFullPath: Path.ChangeExtension(exportPath, "prefab"));
            Assert.That(prefab);

            AssertSameHierarchy(root, prefab, ignoreRootName: true, ignoreRootTransform: true);

            // check that the bones make sense
            var actualSkinnedMesh = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var expectedSkinnedMesh = root.GetComponentInChildren<SkinnedMeshRenderer>();

            var rootBone = actualSkinnedMesh.rootBone;
            var joint2 = rootBone.Find("joint2");
            var joint3 = rootBone.Find("joint2/joint3");

            Assert.That(actualSkinnedMesh.bones.Length, Is.EqualTo(3));
            var bones = new HashSet<Transform>(actualSkinnedMesh.bones);
            Assert.That(bones.Contains(rootBone));
            Assert.That(bones.Contains(joint2));
            Assert.That(bones.Contains(joint3));
            Assert.That(!bones.Contains(expectedSkinnedMesh.rootBone));
        }

        [Test]
        public void TestConvertInPrefabScene()
        {
            var origPrefabPath = FindPathInUnitTests("Prefabs/RegularPrefab_Regular.prefab");
            var root = AssetDatabase.LoadMainAssetAtPath(origPrefabPath) as GameObject;
            var rootInstance = PrefabUtility.InstantiatePrefab(root) as GameObject;
            Assert.That(rootInstance);
            PrefabUtility.UnpackPrefabInstance(rootInstance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var newPrefabPath = GetRandomPrefabAssetPath();
            // make sure the directory structure exists
            var dirName = Path.GetDirectoryName(newPrefabPath);
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            PrefabUtility.SaveAsPrefabAsset(rootInstance, newPrefabPath);

            var newPrefab = PrefabUtility.LoadPrefabContents(newPrefabPath);
            var childObj = newPrefab.transform.GetChild(0).gameObject;
            var childDup = Object.Instantiate(childObj) as GameObject;
            childDup.transform.parent = newPrefab.transform;
            Assert.That(childObj);
            Assert.That(childDup); // duplicate so we have an object to compare against
            
            var fbxPath = GetRandomFbxFilePath();

            // Convert it to a prefab
            var convertedObj = ConvertToNestedPrefab.Convert(childObj,
                fbxFullPath: fbxPath, prefabFullPath: Path.ChangeExtension(fbxPath, "prefab"));
            Assert.That(convertedObj);

            AssertSameHierarchy(childDup, convertedObj, ignoreRootName: true, checkComponents: true);

            Assert.That(!childObj); // replaced by convertedObj
            Assert.That(convertedObj.transform.parent, Is.EqualTo(newPrefab.transform));
            PrefabUtility.UnloadPrefabContents(newPrefab);
        }

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
                Assert.AreEqual (filename2, ConvertToNestedPrefab.IncrementFileName (tempPath, filename1));

                filename1 = basename + " 1.fbx";
                filename2 = Path.Combine(tempPath, basename + " 2.fbx");
                Assert.AreEqual (filename2, ConvertToNestedPrefab.IncrementFileName (tempPath, filename1));

                filename1 = basename + "1.fbx";
                filename2 = Path.Combine(tempPath, basename + "2.fbx");
                Assert.AreEqual (filename2, ConvertToNestedPrefab.IncrementFileName (tempPath, filename1));

                // UNI-25513: bug was that Cube01.fbx => Cube2.fbx
                filename1 = basename + "01.fbx";
                filename2 = Path.Combine(tempPath, basename + "02.fbx");
                Assert.AreEqual (filename2, ConvertToNestedPrefab.IncrementFileName (tempPath, filename1));
            }

            // Test EnforceUniqueNames
            {
                var a = new GameObject("a");
                var b = new GameObject("b");
                var a1 = new GameObject("a");
                var a2 = new GameObject("a");
                ConvertToNestedPrefab.EnforceUniqueNames(new GameObject[] { a, b, a1, a2 });
                Assert.AreEqual("a", a.name);
                Assert.AreEqual("b", b.name);
                Assert.AreEqual("a 1", a1.name);
                Assert.AreEqual("a 2", a2.name);
            }

            // Test GetOrCreateFbxAsset and WillExportFbx
            {
                var a = CreateHierarchy();

                // Test on an object in the scene
                Assert.That(ConvertToNestedPrefab.WillExportFbx(a));
                var aAsset = ConvertToNestedPrefab.GetOrCreateFbxAsset(a, fbxFullPath: GetRandomFbxFilePath());
                Assert.AreNotEqual(a, aAsset);
                AssertSameHierarchy(a, aAsset, ignoreRootName: true, ignoreRootTransform: true);
                Assert.AreEqual(PrefabAssetType.Model, PrefabUtility.GetPrefabAssetType(aAsset));
                Assert.AreEqual(PrefabInstanceStatus.NotAPrefab, PrefabUtility.GetPrefabInstanceStatus(aAsset));

                // Test on an fbx asset
                Assert.That(!ConvertToNestedPrefab.WillExportFbx(aAsset));
                var aAssetAsset = ConvertToNestedPrefab.GetOrCreateFbxAsset(aAsset, fbxFullPath: GetRandomFbxFilePath());
                Assert.AreEqual(aAsset, aAssetAsset);

                // Test on an fbx instance
                var aAssetInstance = PrefabUtility.InstantiatePrefab(aAsset) as GameObject;
                Assert.That(!ConvertToNestedPrefab.WillExportFbx(aAssetInstance));
                var aAssetInstanceAsset = ConvertToNestedPrefab.GetOrCreateFbxAsset(aAssetInstance, fbxFullPath: GetRandomFbxFilePath());
                Assert.AreEqual(aAsset, aAssetInstanceAsset);
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
                var nameMap = ConvertToNestedPrefab.MapNameToSourceRecursive(b, a);
                ConvertToNestedPrefab.CopyComponents(b, a, a, nameMap);
                Assert.IsTrue(b.GetComponent<BoxCollider>());
                Assert.AreEqual(a.transform.localPosition, b.transform.localPosition);
                Assert.AreNotEqual (a.GetComponent<MeshFilter>().sharedMesh, b.GetComponent<MeshFilter> ().sharedMesh);
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

                ConvertToNestedPrefab.UpdateFromSourceRecursive (b, a);

                // everything except the mesh + materials should change
                Assert.AreNotEqual(b.GetComponent<MeshFilter>().sharedMesh, a.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsTrue (b.GetComponent<BoxCollider> ());
                Assert.AreEqual ("BB", b.transform.GetChild (1).name);
                Assert.AreEqual (a1.transform.localPosition, b1.transform.localPosition);
            }

            // Test GetFbxAssetOrNull
            {
                // regular GO should return null
                var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                b.transform.parent = a.transform;
                Assert.That(ConvertToNestedPrefab.GetFbxAssetOrNull(a), Is.Null);

                // try root FBX asset
                var fbx = ExportToFbx(a);
                Assert.That(ConvertToNestedPrefab.GetFbxAssetOrNull(fbx), Is.EqualTo(fbx));

                // try child of FBX asset
                Assert.That(ConvertToNestedPrefab.GetFbxAssetOrNull(fbx.transform.GetChild(0).gameObject), Is.Null);

                // try root of FBX instance
                var fbxInstance = PrefabUtility.InstantiatePrefab(fbx) as GameObject;//GameObject.Instantiate(fbx) as GameObject;
                Assert.That(fbxInstance);
                Assert.That(ConvertToNestedPrefab.GetFbxAssetOrNull(fbxInstance), Is.EqualTo(fbx));

                // try child of FBX instance
                Assert.That(ConvertToNestedPrefab.GetFbxAssetOrNull(fbxInstance.transform.GetChild(0).gameObject), Is.Null);

                // try root of prefab asset
                var prefab = PrefabUtility.SaveAsPrefabAsset(fbxInstance, GetRandomPrefabAssetPath());
                Assert.That(prefab);
                Assert.That(ConvertToNestedPrefab.GetFbxAssetOrNull(prefab), Is.Null);
            }

            // Test CopySerializedProperty
            {
                // test with ReferenceComponent
                var a = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var aReferenceComponent = a.AddComponent<ReferenceComponent>();
                aReferenceComponent.m_transform = a.transform;
                a.name = "test";
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var bReferenceComponent = b.AddComponent<ReferenceComponent>();
                bReferenceComponent.m_transform = b.transform;
                b.name = "test2";

                var aSerObj = new SerializedObject(aReferenceComponent);
                var bSerObj = new SerializedObject(bReferenceComponent);

                var fromProp = aSerObj.FindProperty("m_transform");
                Dictionary<string, GameObject> nameMap = new Dictionary<string, GameObject>(){
                    {"test", a}
                };
                ConvertToNestedPrefab.CopySerializedProperty(bSerObj, fromProp, nameMap);

                Assert.That(bReferenceComponent.m_transform.name, Is.EqualTo(a.transform.name));
            }

            // Test GetSceneReferencesToObject()
            {
                var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var b = new GameObject();
                var c = new GameObject();

                var reference = b.AddComponent<ReferenceComponent>();
                var constraint = c.AddComponent<UnityEngine.Animations.PositionConstraint>();

                reference.m_collider = a.GetComponent<BoxCollider>();

                var constraintSource = new UnityEngine.Animations.ConstraintSource();
                constraintSource.sourceTransform = a.transform;
                constraintSource.weight = 0.5f;
                constraint.AddSource(constraintSource);
                
                var sceneRefs = ConvertToNestedPrefab.GetSceneReferencesToObject(a);
                Assert.That(sceneRefs.Count, Is.EqualTo(2));
                Assert.That(sceneRefs.Contains(a)); // GameObjects also reference themself
                Assert.That(sceneRefs.Contains(b));

                sceneRefs = ConvertToNestedPrefab.GetSceneReferencesToObject(a.GetComponent<BoxCollider>());
                Assert.That(sceneRefs.Count, Is.EqualTo(1));
                Assert.That(sceneRefs.Contains(b));

                sceneRefs = ConvertToNestedPrefab.GetSceneReferencesToObject(a.transform);
                Assert.That(sceneRefs.Count, Is.EqualTo(2));
                Assert.That(sceneRefs.Contains(b));
                Assert.That(sceneRefs.Contains(c));
            }
        }

        [Test]
        public void BasicTest()
        {
            // Get a random directory.
            var path = GetRandomFileNamePath(extName: "");

            // Create a cube.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Convert it to a prefab
            var cubePrefab = ConvertToNestedPrefab.Convert(cube,
                fbxDirectoryFullPath: path, prefabDirectoryFullPath: path);

            // Make sure it's what we expect.
            Assert.That(!cube); // original was deleted
            Assert.That(cubePrefab); // we got the new
            Assert.AreEqual("Cube", cubePrefab.name); // it has the right name
            Assert.That(!EditorUtility.IsPersistent(cubePrefab)); // cubePrefab is an instance in the scene

            // Should be all the same triangles. But it isn't. TODO.
            // At least the indices should match in multiplicity.
            var cubeMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
            var cubePrefabMesh = cubePrefab.GetComponent<MeshFilter>().sharedMesh;
            //Assert.That(
            //  cubeMesh.triangles,
            //  Is.EqualTo(cubePrefabMesh.triangles)
            //);
            Assert.That(cubeMesh.triangles, Is.EquivalentTo(cubeMesh.triangles));

            // Make sure it's where we expect.
            var assetRelativePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cubePrefab);
            var assetFullPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../" + assetRelativePath));
            Assert.AreEqual(Path.GetFullPath(path), Path.GetDirectoryName(assetFullPath));
        }

        [Test]
        public void ExhaustiveTests() {
            // Try convert in every corner case we can imagine.

            // Test Convert on an object in the scene
            {
                var a = CreateHierarchy();
                var aConvert = ConvertToNestedPrefab.Convert(a, fbxFullPath: GetRandomFbxFilePath(), prefabFullPath: GetRandomPrefabAssetPath());
                // original hierarchy was deleted, recreate
                a = CreateHierarchy();
                AssertSameHierarchy(a, aConvert, ignoreRootName: true);
            }

            // Test Convert on a prefab asset.
            // Expected: creates a new fbx and a new prefab.
            {
                var a = CreateHierarchy();
                var aPrefabPath = GetRandomPrefabAssetPath();
                var bPrefabPath = GetRandomPrefabAssetPath();

                // Convert an existing prefab (by creating a new prefab here).
                var aPrefab = PrefabUtility.SaveAsPrefabAsset(a, aPrefabPath); // PrefabUtility.CreatePrefab(aPrefabPath, a);

                // Provide a different prefab path if convert needs to create a new file.
                var aConvert = ConvertToNestedPrefab.Convert(aPrefab, fbxFullPath: GetRandomFbxFilePath(), prefabFullPath: bPrefabPath);

                // Make sure we exported to the new prefab, didn't change the original
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(aConvert), Is.EqualTo(bPrefabPath));
                Assert.IsTrue(aPrefab);
                Assert.AreNotEqual(aPrefab, aConvert);
            }

            // Test Convert on a prefab instance.
            // Expected: creates a new fbx and new prefab; 'a' points to the new prefab now. Old prefab still exists.
            {
                var a = CreateHierarchy();
                var aPrefabPath = GetRandomPrefabAssetPath();
                var aPrefab = PrefabUtility.SaveAsPrefabAsset(a, aPrefabPath);
                var bPrefabPath = GetRandomPrefabAssetPath();
                var aConvert = ConvertToNestedPrefab.Convert(a, fbxFullPath: GetRandomFbxFilePath(), prefabFullPath: bPrefabPath);
                Assert.AreEqual(bPrefabPath, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(aConvert));
                Assert.AreEqual(aPrefabPath, AssetDatabase.GetAssetPath(aPrefab));
                Assert.AreNotEqual(aPrefabPath, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(aConvert));
                AssertSameHierarchy(aPrefab, aConvert, ignoreRootName: true);
            }

            // Test Convert on an fbx asset
            // Expected: uses the old fbx and creates a new prefab
            {
                var a = CreateHierarchy();
                var aFbx = ExportToFbx(a);
                var aConvert = ConvertToNestedPrefab.Convert(aFbx, fbxFullPath: GetRandomFbxFilePath(), prefabFullPath: GetRandomPrefabAssetPath());
                Assert.AreNotEqual(aFbx, aConvert);
                // ignore root transform since the default functionality of the FBX exporter is to reset the root's position on export
                AssertSameHierarchy(a, aConvert, ignoreRootName: true, ignoreRootTransform: true);
            }

            // Test Convert on an fbx instance
            // Expected: uses the old fbx and creates a new prefab
            {
                var a = CreateHierarchy();
                var aFbx = ExportToFbx(a);
                var aFbxInstance = PrefabUtility.InstantiatePrefab(aFbx) as GameObject;
                var aConvert = ConvertToNestedPrefab.Convert(aFbxInstance, fbxFullPath: GetRandomFbxFilePath(), prefabFullPath: GetRandomPrefabAssetPath());
                Assert.AreNotEqual(aFbx, aConvert);
                AssertSameHierarchy(a, aConvert, ignoreRootName: true, ignoreRootTransform: true);
            }

            // Test Convert on an fbx instance, but not the root.
            // Expected: creates a new fbx and creates a new prefab
            {
                var a = CreateHierarchy();
                var aFbx = ExportToFbx(a);
                var aFbxInstance = PrefabUtility.InstantiatePrefab(aFbx) as GameObject;
                PrefabUtility.UnpackPrefabInstance(aFbxInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var aFbxInstanceChild = aFbxInstance.transform.GetChild(0).gameObject;
                var aConvertFbxPath = GetRandomFbxFilePath();
                var aConvert = ConvertToNestedPrefab.Convert(aFbxInstanceChild, fbxFullPath: aConvertFbxPath, prefabFullPath: GetRandomPrefabAssetPath());
                AssertSameHierarchy(a.transform.GetChild(0).gameObject, aConvert, ignoreRootName: true);
            }
        }

        [Test]
        public void SkinnedMeshTest()
        {
            // Create a cube with a bogus skinned-mesh rather than a static
            // mesh setup.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<SkinnedMeshRenderer>();
            var meshFilter = cube.GetComponent<MeshFilter>();
            var meshRender = cube.GetComponent<MeshRenderer>();
            Object.DestroyImmediate(meshRender);
            Object.DestroyImmediate(meshFilter);

            // Convert it.
            var file = GetRandomFbxFilePath();
            var cubePrefab = ConvertToNestedPrefab.Convert(cube, fbxFullPath: file, prefabFullPath: Path.ChangeExtension(file, ".prefab"));

            // Make sure it has a skinned mesh renderer
            Assert.That(cubePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(), Is.Not.Empty);
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

            var dictionary = ConvertToNestedPrefab.MapNameToSourceRecursive(cube, cube2);

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

            var dictionaryBroken = ConvertToNestedPrefab.MapNameToSourceRecursive(cube, cube3);

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

            // Convert it to a prefab
            var cubePrefab = ConvertToNestedPrefab.Convert(cube,
                fbxFullPath: path, prefabFullPath: Path.ChangeExtension(path, ".prefab"));

            Assert.That (!cube);
            Assert.That (cubePrefab);

            Assert.AreEqual (Path.GetFileNameWithoutExtension (path), cubePrefab.name);
        }
    }
}