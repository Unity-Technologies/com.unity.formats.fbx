using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections.Generic;

namespace FbxExporters.UnitTests
{
    public class FbxPrefabTest : ExporterTestBase
    {
        GameObject m_original; // stored for testing
        FbxPrefab.FbxRepresentation m_originalRep;

        GameObject m_source; // the fbx model
        GameObject m_autoPrefab; // prefab that auto-updates
        GameObject m_manualPrefab; // prefab that doesn't auto-update

        class UpdateListener : System.IDisposable
        {
            public List<string> Updated { get ; private set; }
            public int NumUpdates { get ; private set; }

            GameObject m_prefabToAttachTo;

            public UpdateListener(GameObject prefabToAttachTo) {
                m_prefabToAttachTo = prefabToAttachTo;
                Updated = new List<string>();
                NumUpdates = 0;
                FbxPrefab.OnUpdate += OnUpdate;
            }

            ~UpdateListener() {
                FbxPrefab.OnUpdate -= OnUpdate;
            }

            public void Dispose() {
                FbxPrefab.OnUpdate -= OnUpdate;
            }

            void OnUpdate(FbxPrefab prefabInstance, IEnumerable<GameObject> updated)
            {
                if (prefabInstance.name != m_prefabToAttachTo.name) {
                    return;
                }
                NumUpdates++;
                foreach(var go in updated) {
                    Updated.Add(go.name);
                }
            }
        }

        public static void AssertAreIdentical(
                FbxPrefab.FbxRepresentation a,
                FbxPrefab.FbxRepresentation b) {
            // A bit of a laborious comparison scheme. This is due to the
            // round-trip through FBX causing tiny errors in the transforms.
            var astack = new List<FbxPrefab.FbxRepresentation> ();
            astack.Add(a);
            var bstack = new List<FbxPrefab.FbxRepresentation> ();
            bstack.Add(b);

            var aDummy = new GameObject("aDummy").transform;
            var bDummy = new GameObject("bDummy").transform;
            while (astack.Count > 0) {
                Assert.AreEqual(astack.Count, bstack.Count); // should never fail
                a = astack[astack.Count - 1]; astack.RemoveAt(astack.Count - 1);
                b = bstack[bstack.Count - 1]; bstack.RemoveAt(bstack.Count - 1);

                // Verify that they have the same children (by name).
                var achildren = a.ChildNames;
                var bchildren = b.ChildNames;
                Assert.That(achildren, Is.EquivalentTo(bchildren));

                // Add the children to each stack.
                foreach(var child in achildren) {
                    astack.Add(a.GetChild(child));
                    bstack.Add(b.GetChild(child));
                }

                // Verify that they have the same components.
                var atypes = a.ComponentTypes;
                var btypes = b.ComponentTypes;
                Assert.That(atypes, Is.EquivalentTo(btypes));

                foreach(var t in atypes) {
                    var avalues = a.GetComponentValues(t);
                    var bvalues = b.GetComponentValues(t);
                    Assert.AreEqual(avalues.Count, bvalues.Count);

                    if (t != "UnityEngine.Transform") {
                        Assert.AreEqual(avalues, bvalues);
                    } else {
                        // Verify that the transforms are nearly (but don't require bitwise) equal.
                        EditorJsonUtility.FromJsonOverwrite(avalues[0], aDummy);
                        EditorJsonUtility.FromJsonOverwrite(bvalues[0], bDummy);
                        var dist = Vector3.Distance(aDummy.localPosition, bDummy.localPosition);
                        Assert.That(dist, Is.LessThan(1e-6), () => string.Format("position {0} vs {1} dist {2}",
                                aDummy.localPosition, bDummy.localPosition, dist));

                        dist = Vector3.Distance(aDummy.localScale, bDummy.localScale);
                        Assert.That(dist, Is.LessThan(1e-6), () => string.Format("scale {0} vs {1} dist {2}",
                                aDummy.localScale, bDummy.localScale, dist));

                        dist = Quaternion.Angle(aDummy.localRotation, bDummy.localRotation);
                        Assert.That(dist, Is.LessThan(1e-6), () => string.Format("rotation {0} vs {1} angle {2}",
                                aDummy.localRotation.eulerAngles, bDummy.localRotation.eulerAngles, dist));
                    }
                }
            }
        }

        public static void AssertAreDifferent(
                FbxPrefab.FbxRepresentation a,
                FbxPrefab.FbxRepresentation b) {
            Assert.AreNotEqual(a.ToJson(), b.ToJson());
        }

        [SetUp]
        public void Init() {
            // Create a test hierarchy. It has unique names.
            m_original = CreateHierarchy("FbxPrefabTestRoot");
            m_originalRep = Rep(m_original);

            // Convert it to FBX. The asset file will be deleted automatically
            // on termination.
            var fbxAsset = FbxExporters.Editor.ModelExporter.ExportObject(
                    GetRandomFbxFilePath(), m_original);
            m_source = AssetDatabase.LoadMainAssetAtPath(fbxAsset) as GameObject;
            Assert.IsTrue(m_source);

            // Create an FbxPrefab linked to the Fbx file. Make it auto-update.
            {
                var prefabInstance = GameObject.Instantiate(m_original);
                var fbxPrefab = prefabInstance.AddComponent<FbxPrefab>();
                fbxPrefab.SetSourceModel(m_source);
                fbxPrefab.SetAutoUpdate(true);
                m_autoPrefab = PrefabUtility.CreatePrefab(
                        GetRandomPrefabAssetPath(),
                        prefabInstance);
            }

            // Create an FbxPrefab linked to the same Fbx file. Make it NOT auto-update.
            {
                var prefabInstance = GameObject.Instantiate(m_original);
                var fbxPrefab = prefabInstance.AddComponent<FbxPrefab>();
                fbxPrefab.SetSourceModel(m_source);
                fbxPrefab.SetAutoUpdate(false);
                m_manualPrefab = PrefabUtility.CreatePrefab(
                        GetRandomPrefabAssetPath(),
                        prefabInstance);
            }
        }

        FbxPrefab.FbxRepresentation Rep(GameObject go) {
            return new FbxPrefab.FbxRepresentation(go.transform);
        }

        FbxPrefab.FbxRepresentation History(GameObject go) {
            return go.GetComponent<FbxPrefab>().GetFbxHistory();
        }

        GameObject ModifySourceFbx()
        {
            // Modify the source fbx file:
            // - delete parent1
            // - add parent3
            var newModel = PrefabUtility.InstantiatePrefab(m_source) as GameObject;
            GameObject.DestroyImmediate(newModel.transform.Find("Parent1").gameObject);
            CreateGameObject("Parent3", newModel.transform);

            // Export it to clobber the old FBX file.
            // Sleep one second first to make sure the timestamp differs
            // enough, so the asset database knows to reload it. I was getting
            // test failures otherwise.
            SleepForFileTimestamp();
            FbxExporters.Editor.ModelExporter.ExportObjects (
                    AssetDatabase.GetAssetPath(m_source),
                    new Object[] { newModel } );
            AssetDatabase.Refresh();

            return newModel;
        }

        [Test]
        public void BasicTest() {
            // Check the history is good at the start
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, Rep(m_autoPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_autoPrefab));

            FbxPrefab.FbxRepresentation newHierarchy;
            using(var updateSet = new UpdateListener(m_autoPrefab)) {
                Debug.Log("Testing auto update");
                newHierarchy = Rep(ModifySourceFbx());
                AssertAreDifferent(m_originalRep, newHierarchy);

                // Make sure the fbx source changed.
                AssertAreDifferent(m_originalRep, Rep(m_source));
                AssertAreIdentical(newHierarchy, Rep(m_source));

                // Make sure the auto-update prefab changed.
                AssertAreIdentical(newHierarchy, Rep(m_autoPrefab));
                AssertAreIdentical(newHierarchy, History(m_autoPrefab));

                // Make sure the manual-update prefab didn't.
                AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
                AssertAreIdentical(m_originalRep, History(m_manualPrefab));

                // Make sure we got the right changes.
                Assert.That (updateSet.Updated, Is.EquivalentTo (new string [] {
                    // TODO: UNI-24579 - we should only be seeing Parent3 here,
                    // the other two are for transform changes, but
                    // they shouldn't have changed at all
                    "Parent2", "Parent3", "Child3"
                }
                ));
            }

            // Manual update, make sure it updated.
            Debug.Log("Testing manual update");
            var manualPrefabComponent = m_manualPrefab.GetComponent<FbxPrefab>();
            manualPrefabComponent.SyncPrefab();
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));

            // Check some corner cases.
            Assert.AreEqual(m_source, manualPrefabComponent.GetFbxAsset());

            // Illegal to set the source model to something that isn't an
            // asset.
            var go = CreateGameObject("foo");
            Debug.Log("Testing SetSourceModel to scene object");
            Assert.That( () => manualPrefabComponent.SetSourceModel(go), Throws.Exception );

            // Illegal to set the source model to something that isn't an fbx
            // asset (it's a prefab).
            Debug.Log("Testing SetSourceModel to prefab");
            Assert.That( () => manualPrefabComponent.SetSourceModel(m_autoPrefab), Throws.Exception );

            // Legal to set the source model to null. It doesn't change the
            // hierarchy or anything.
            Debug.Log("Testing SetSourceModel to null");
            Assert.That( () => manualPrefabComponent.SetSourceModel(null), Throws.Nothing );
            Assert.IsNull(manualPrefabComponent.GetFbxAsset());
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));
            Assert.That( () => manualPrefabComponent.SyncPrefab(), Throws.Nothing );
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));

            // Switch to some other model, which looks like the original model
            // (but is a totally different file). This will cause an update
            // immediately.
            var fbxAsset = FbxExporters.Editor.ModelExporter.ExportObject(
                    GetRandomFbxFilePath(), m_original);
            var newSource = AssetDatabase.LoadMainAssetAtPath(fbxAsset) as GameObject;
            Assert.IsTrue(newSource);
            Debug.Log("Testing SetSourceModel relink");
            manualPrefabComponent.SetSourceModel(newSource);
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));
        }

        [Test]
        public void ManualToAuto() {
            // Check what happens when we go from manual to auto-update.
            var newHierarchy = Rep(ModifySourceFbx());
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));

            m_manualPrefab.GetComponent<FbxPrefab>().SetAutoUpdate(false);
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));

            m_manualPrefab.GetComponent<FbxPrefab>().SetAutoUpdate(true);
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));
        }
    }

    public class FbxPrefabRegressions : ExporterTestBase
    {
        [Ignore("ConvertToModel return value is messed up.")]
        [Test]
        public void TestCubeAtRoot()
        {
            // vkovec found a bug when removing a mesh at the root.
            // bhudson fixed it, let's make sure it stays fixed:
            // 1. Make a cube
            // 2. Convert to model
            // 3. In Maya, make a null named 'Cube' and put it at the root.
            // 4. Update => meshfilter and meshrenderer should be gone

            // Make a cube.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            var cubeAssetPath = GetRandomFbxFilePath();
            var autoPrefab = FbxExporters.Editor.ConvertToModel.CreateInstantiatedModelPrefab(
                new GameObject[] { cube }, path: cubeAssetPath)[0];
            Assert.IsTrue(autoPrefab);

            // Make a maya locator.
            var locator = new GameObject("Cube");
            var locatorAssetPath = FbxExporters.Editor.ModelExporter.ExportObject(
                GetRandomFbxFilePath(), locator);

            // Check the prefab has all the default stuff it should have.
            Assert.IsNotNull(autoPrefab.GetComponent<MeshFilter>());
            Assert.IsNotNull(autoPrefab.GetComponent<MeshRenderer>());
            Assert.IsNotNull(autoPrefab.GetComponent<BoxCollider>());

            // Now copy the locator over and refresh.
            SleepForFileTimestamp();
            System.IO.File.Copy(locatorAssetPath, cubeAssetPath, overwrite: true);
            AssetDatabase.Refresh();

            // Check the prefab lost its mesh filter and renderer.
            Assert.IsNull(autoPrefab.GetComponent<MeshFilter>());
            Assert.IsNull(autoPrefab.GetComponent<MeshRenderer>());

            // The box collider is controversial: it got generated
            // automatically, so shouldn't it be deleted automatically? But
            // right now it doesn't get deleted, so let's test to make sure a
            // change in behaviour isn't accidental.
            Assert.IsNotNull(autoPrefab.GetComponent<BoxCollider>());
        }
    }

    public class FbxPrefabHelpersTest
    {
        class AClass { }

        [Test]
        public void TestStatics()
        {
            {
                // Test Initialize semantics.
                AClass anItem = null;
                Assert.That(() => FbxPrefab.Initialize(ref anItem), Throws.Nothing);
                Assert.IsNotNull(anItem);
                Assert.That(() => FbxPrefab.Initialize(ref anItem), Throws.Exception);
            }

            {
                // Test list append helper.
                List<string> thelist = null;
                Assert.That(() => FbxPrefab.Append(ref thelist, "hi"), Throws.Nothing);
                Assert.IsNotNull(thelist);
                Assert.AreEqual(1, thelist.Count);
                Assert.AreEqual("hi", thelist[0]);
                Assert.That(() => FbxPrefab.Append(ref thelist, "bye"), Throws.Nothing);
                Assert.IsNotNull(thelist);
                Assert.AreEqual(2, thelist.Count);
                Assert.AreEqual("hi", thelist[0]);
                Assert.AreEqual("bye", thelist[1]);
            }

            {
                // Test normal dictionary helpers.
                Dictionary<string, AClass> thedict = null;
                Dictionary<string, AClass> expected = null;

                AClass A = new AClass();
                AClass B = new AClass();

                Assert.That(() => FbxPrefab.Add(ref thedict, "a", A), Throws.Nothing);
                expected = new Dictionary<string, AClass>();
                expected["a"] = A;
                Assert.IsNotNull(thedict);
                Assert.AreEqual(expected, thedict);

                Assert.That(() => FbxPrefab.Add(ref thedict, "b", B), Throws.Nothing);
                expected["b"] = B;
                Assert.IsNotNull(thedict);
                Assert.AreEqual(expected, thedict);

                Assert.That(() => FbxPrefab.Add(ref thedict, "b", B), Throws.Exception);

                var b = FbxPrefab.GetOrCreate(thedict, "b"); // actually gets
                Assert.AreEqual(B, b);

                var c = FbxPrefab.GetOrCreate(thedict, "c"); // actually doesn't get
                Assert.IsNotNull(c);
            }

            {
                // Test dictionary-of-lists helpers.
                Dictionary<string, List<string>> thedict = null;
                Dictionary<string, List<string>> expected = null;

                Assert.That(() => FbxPrefab.Append(/* not ref */ thedict, "a", "1"), Throws.Exception);
                Assert.That(() => FbxPrefab.Append(ref thedict, "a", "1"), Throws.Nothing);
                expected = new Dictionary<string, List<string>>();
                expected["a"] = new List<string>( new string [] { "1" });
                Assert.AreEqual(expected, thedict);

                Assert.That(() => FbxPrefab.Append(ref thedict, "a", "2"), Throws.Nothing);
                expected["a"].Add("2");
                Assert.AreEqual(expected, thedict);

                Assert.That(() => FbxPrefab.Append(ref thedict, "b", "3"), Throws.Nothing);
                expected["b"] = new List<string>( new string [] { "3" });
                Assert.AreEqual(expected, thedict);
            }

            {
                // Test dict-of-dict-of-list helpers.
                Dictionary<string, Dictionary<string, List<string>>> thedict = null;
                Dictionary<string, Dictionary<string, List<string>>> expected = null;

                Assert.That(() => FbxPrefab.Append(ref thedict, "a", "1", "yo"), Throws.Nothing);
                expected = new Dictionary<string, Dictionary<string, List<string>>>();
                expected["a"] = new Dictionary<string, List<string>>();
                expected["a"]["1"] = new List<string>(new string[] { "yo" });
                Assert.AreEqual(expected, thedict);

                Assert.That(() => FbxPrefab.Append(ref thedict, "a", "1", "yoyo"), Throws.Nothing);
                expected["a"]["1"].Add("yoyo");
                Assert.AreEqual(expected, thedict);

                Assert.That(() => FbxPrefab.Append(ref thedict, "a", "2", "bar"), Throws.Nothing);
                expected["a"]["2"] = new List<string>(new string[] { "bar" });
                Assert.AreEqual(expected, thedict);
            }

            {
                // Test FbxRepresentation parsing function: consume.
                string testString = "abc  \n\tdefg\nhij\tkl m";
                int index = 0;
                Assert.IsTrue(FbxPrefab.FbxRepresentation.Consume('a', testString, ref index));
                Assert.AreEqual(1, index);
                index = 2;
                Assert.IsTrue(FbxPrefab.FbxRepresentation.Consume('c', testString, ref index));
                Assert.AreEqual(3, index);
                Assert.That(() => FbxPrefab.FbxRepresentation.Consume('c', testString, ref index), Throws.Exception);
                Assert.AreEqual(7, index);
                Assert.IsFalse(FbxPrefab.FbxRepresentation.Consume('c', testString, ref index, required: false));
                Assert.AreEqual(7, index);
                Assert.IsTrue(FbxPrefab.FbxRepresentation.Consume('d', testString, ref index));
                Assert.AreEqual(8, index);
                index = testString.Length - 1;
                Assert.IsTrue(FbxPrefab.FbxRepresentation.Consume('m', testString, ref index));
                Assert.AreEqual(testString.Length, index);
                Assert.That(() => FbxPrefab.FbxRepresentation.Consume('w', testString, ref index), Throws.Exception);
                index = testString.Length;
                Assert.That(() => FbxPrefab.FbxRepresentation.Consume('w', testString, ref index, required: false), Throws.Exception);
                index = testString.Length - 1;
                Assert.IsFalse(FbxPrefab.FbxRepresentation.Consume('n', testString, ref index, required: false));
                Assert.AreEqual(testString.Length - 1, index);
            }

            {
                // Test FbxRepresentation parsing function: readString.
                string noQuotes = " \"this string has no quotes\" ";
                string quotes = " \"this string has \\\"quotes\\\" and backslashes \\\\ and \\ nonsense\" ";
                string badEnd = "\"this string has a quote but doesn't end.";
                string badStart = "this string has a quote but doesn't start.\"";
                int index = 1;

                Assert.AreEqual("this string has no quotes", FbxPrefab.FbxRepresentation.ReadString(noQuotes, ref index));
                Assert.AreEqual(index, noQuotes.LastIndexOf('"') + 1);

                index = 1;
                Assert.AreEqual("this string has \"quotes\" and backslashes \\ and \\ nonsense",
                        FbxPrefab.FbxRepresentation.ReadString(quotes, ref index));
                Assert.AreEqual(index, quotes.LastIndexOf('"') + 1);

                index = 0;
                Assert.That(() => FbxPrefab.FbxRepresentation.ReadString(badEnd, ref index), Throws.Exception);
                Assert.AreEqual(badEnd.Length, index);
                index = 0;
                Assert.That(() => FbxPrefab.FbxRepresentation.ReadString(badStart, ref index), Throws.Exception);
                Assert.AreEqual(0, index);
            }

            {
                string unquoted = " \"this string has backslashes \\ and quotes\" ";
                string quoted = " \\\"this string has backslashes \\\\ and quotes\\\" ";
                Assert.AreEqual(quoted, FbxPrefab.FbxRepresentation.EscapeString(unquoted));
            }
        }

        static void TestFbxRepresentationMatches(FbxPrefab.FbxRepresentation repA)
        {
            // Look at the top of TestFbxRepresentation for the construction.

            // The root doesn't have the fbxprefab or transform stored -- just the collider
            Assert.That(repA.ChildNames, Is.EquivalentTo(new string[] { "b" }));
            Assert.That(repA.ComponentTypes, Is.EquivalentTo(new string[] { "UnityEngine.CapsuleCollider" }));
            Assert.IsNull(repA.GetComponentValues("UnityEngine.Transform"));
            Assert.IsNull(repA.GetComponentValues("UnityEngine.FbxPrefab"));
            Assert.AreEqual(1, repA.GetComponentValues("UnityEngine.CapsuleCollider").Count);

            // The child does have the transform.
            var repB = repA.GetChild("b");
            Assert.That(repB.ChildNames, Is.EquivalentTo(new string[] { }));
            Assert.That(repB.ComponentTypes, Is.EquivalentTo(new string[] {
                        "UnityEngine.Transform",
                        "UnityEngine.BoxCollider" }));
            Assert.AreEqual(1, repB.GetComponentValues("UnityEngine.Transform").Count);
            Assert.AreEqual(1, repB.GetComponentValues("UnityEngine.BoxCollider").Count);

            // The transform better have the right values.
            var c = new GameObject("c");
            EditorJsonUtility.FromJsonOverwrite(
                    repB.GetComponentValues("UnityEngine.Transform")[0],
                    c.transform);
            Assert.AreEqual(new Vector3(1,2,3), c.transform.localPosition);

            // The capsule collider too.
            var capsule = c.AddComponent<CapsuleCollider>();
            EditorJsonUtility.FromJsonOverwrite(
                    repA.GetComponentValues("UnityEngine.CapsuleCollider")[0],
                    capsule);
            Assert.AreEqual(4, capsule.height);
        }

        [Test]
        public void TestFbxRepresentation()
        {
            var a = new GameObject("a");
            a.AddComponent<FbxPrefab>();
            var acapsule = a.AddComponent<CapsuleCollider>();
            acapsule.height = 4;

            var b = new GameObject("b");
            b.transform.parent = a.transform;
            b.transform.localPosition = new Vector3(1,2,3);
            b.AddComponent<BoxCollider>();

            var repA = new FbxPrefab.FbxRepresentation(a.transform);
            TestFbxRepresentationMatches(repA);

            // Test that we can round-trip through a string.
            var json = repA.ToJson();
            var repAstring = new FbxPrefab.FbxRepresentation(json);
            TestFbxRepresentationMatches(repAstring);
        }
    }
}
