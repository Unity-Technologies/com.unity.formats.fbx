using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace FbxExporters.UnitTests
{
    public class FbxSourceTest : ExporterTestBase
    {
        GameObject m_original; // stored for testing
        FbxSource.FbxRepresentation m_originalRep;

        GameObject m_source; // the fbx model
        GameObject m_autoPrefab; // prefab that auto-updates
        GameObject m_manualPrefab; // prefab that doesn't auto-update

        public static void AssertAreIdentical(
                FbxSource.FbxRepresentation a,
                FbxSource.FbxRepresentation b) {
            Assert.AreEqual(a.ToJson(), b.ToJson());
        }

        public static void AssertAreDifferent(
                FbxSource.FbxRepresentation a,
                FbxSource.FbxRepresentation b) {
            Assert.AreNotEqual(a.ToJson(), b.ToJson());
        }

        [SetUp]
        public void Init() {
            // Create a test hierarchy. It has unique names.
            m_original = CreateHierarchy("FbxSourceTestRoot");
            m_originalRep = Rep(m_original);

            // Convert it to FBX. The asset file will be deleted automatically
            // on termination.
            var fbxAsset = FbxExporters.Editor.ModelExporter.ExportObject(
                    GetRandomFileNamePath(), m_original);
            m_source = AssetDatabase.LoadMainAssetAtPath(fbxAsset) as GameObject;
            Assert.IsTrue(m_source);

            // Create an FbxSource linked to the Fbx file. Make it auto-update.
            {
                var prefabInstance = GameObject.Instantiate(m_original);
                var fbxSource = prefabInstance.AddComponent<FbxSource>();
                fbxSource.SetSourceModel(m_source);
                fbxSource.SetAutoUpdate(true);
                m_autoPrefab = PrefabUtility.CreatePrefab(
                        GetRandomFileNamePath(extName: ".prefab"),
                        prefabInstance);
            }

            // Create an FbxSource linked to the same Fbx file. Make it NOT auto-update.
            {
                var prefabInstance = GameObject.Instantiate(m_original);
                var fbxSource = prefabInstance.AddComponent<FbxSource>();
                fbxSource.SetSourceModel(m_source);
                fbxSource.SetAutoUpdate(false);
                m_manualPrefab = PrefabUtility.CreatePrefab(
                        GetRandomFileNamePath(extName: ".prefab"),
                        prefabInstance);
            }
        }

        FbxSource.FbxRepresentation Rep(GameObject go) {
            return FbxSource.FbxRepresentation.FromTransform(go.transform);
        }

        FbxSource.FbxRepresentation History(GameObject go) {
            return go.GetComponent<FbxSource>().GetFbxHistory();
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
            System.Threading.Thread.Sleep(1000);
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

            var newHierarchy = Rep(ModifySourceFbx());
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

            // Manual update, make sure it updated.
            m_manualPrefab.GetComponent<FbxSource>().SyncPrefab();
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));

            // Check some corner cases.
            var manualSource = m_manualPrefab.GetComponent<FbxSource>();
            Assert.AreEqual(m_source, manualSource.GetFbxAsset());

            // Illegal to set the source model to something that isn't an
            // asset.
            var go = CreateGameObject("foo");
            Assert.That( () => manualSource.SetSourceModel(go), Throws.Exception );

            // Illegal to set the source model to something that isn't an fbx
            // asset (it's a prefab).
            Assert.That( () => manualSource.SetSourceModel(m_autoPrefab), Throws.Exception );

            // Legal to set the source model to null. It doesn't change the
            // hierarchy or anything.
            Assert.That( () => manualSource.SetSourceModel(null), Throws.Nothing );
            Assert.IsNull(manualSource.GetFbxAsset());
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));
            Assert.That( () => manualSource.SyncPrefab(), Throws.Nothing );
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));

            // Switch to some other model, which looks like the original model
            // (but is a totally different file). This will cause an update
            // immediately.
            var fbxAsset = FbxExporters.Editor.ModelExporter.ExportObject(
                    GetRandomFileNamePath(), m_original);
            var newSource = AssetDatabase.LoadMainAssetAtPath(fbxAsset) as GameObject;
            Assert.IsTrue(newSource);
            manualSource.SetSourceModel(newSource);
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));
        }

        [Test]
        public void ManualToAuto() {
            // Check what happens when we go from manual to auto-update.
            var newHierarchy = Rep(ModifySourceFbx());
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));

            m_manualPrefab.GetComponent<FbxSource>().SetAutoUpdate(false);
            AssertAreIdentical(m_originalRep, Rep(m_manualPrefab));
            AssertAreIdentical(m_originalRep, History(m_manualPrefab));

            m_manualPrefab.GetComponent<FbxSource>().SetAutoUpdate(true);
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));
        }
    }
}
