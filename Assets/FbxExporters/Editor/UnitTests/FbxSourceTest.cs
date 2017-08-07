using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace FbxExporters.UnitTests
{
    public class FbxSourceTest : ExporterTestBase
    {
        GameObject m_original; // stored for testing
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

            // Convert it to FBX. The asset file will be deleted automatically
            // on termination.
            var fbxAsset = FbxExporters.Editor.ModelExporter.ExportObjects (GetRandomFileNamePath(),
                    new Object[] { m_original } );
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

        [Test]
        public void BasicTest() {
            // Check the history is good at the start
            var originalHierarchy = Rep(m_original);

            AssertAreIdentical(originalHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(originalHierarchy, Rep(m_autoPrefab));
            AssertAreIdentical(originalHierarchy, History(m_manualPrefab));
            AssertAreIdentical(originalHierarchy, History(m_autoPrefab));

            // Modify the source fbx file:
            // - delete parent1
            // - add parent3
            var newModel = PrefabUtility.InstantiatePrefab(m_source) as GameObject;
            GameObject.DestroyImmediate(newModel.transform.Find("Parent1").gameObject);
            CreateGameObject("Parent3", newModel.transform);
            var newHierarchy = Rep(newModel);
            AssertAreDifferent(originalHierarchy, newHierarchy);

            // Export it to clobber the old FBX file.
            // Sleep one second first to make sure the timestamp differs
            // enough, so the asset database knows to reload it. I was getting
            // test failures otherwise.
            System.Threading.Thread.Sleep(1000);
            FbxExporters.Editor.ModelExporter.ExportObjects (
                    AssetDatabase.GetAssetPath(m_source),
                    new Object[] { newModel } );
            AssetDatabase.Refresh();

            // Now: the fbx source changed.
            // The auto-update prefab changed.
            // The manual-update prefab didn't.
            AssertAreDifferent(originalHierarchy, Rep(m_source));
            AssertAreIdentical(newHierarchy, Rep(m_source));

            AssertAreIdentical(newHierarchy, Rep(m_autoPrefab));
            AssertAreIdentical(newHierarchy, History(m_autoPrefab));

            AssertAreIdentical(originalHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(originalHierarchy, History(m_manualPrefab));

            // Manual update, make sure it updated.
            m_manualPrefab.GetComponent<FbxSource>().SyncPrefab();
            AssertAreIdentical(newHierarchy, Rep(m_manualPrefab));
            AssertAreIdentical(newHierarchy, History(m_manualPrefab));
        }
    }
}
