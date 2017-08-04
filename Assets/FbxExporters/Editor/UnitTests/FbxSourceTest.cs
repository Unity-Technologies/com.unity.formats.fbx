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
        GameObject m_updatingPrefab; // the version with the FbxSource on it
        FbxSource m_fbxSource; // the fbxsource on the m_updatingPrefab

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

            // Create an FbxSource linked to the Fbx file.
            var prefabInstance = GameObject.Instantiate(m_original);
            var fbxSource = prefabInstance.AddComponent<FbxSource>();
            fbxSource.SetSourceModel(m_source);
            m_updatingPrefab = PrefabUtility.CreatePrefab(GetRandomFileNamePath(extName: ".prefab"),
                    prefabInstance);
            m_fbxSource = m_updatingPrefab.GetComponent<FbxSource>();
        }

        [Test]
        public void BasicTest() {
            // Check the history is good.
            var origHistory = FbxSource.FbxRepresentation.FromTransform(m_original.transform);
            var curHistory = m_fbxSource.GetFbxHistory();
            AssertAreIdentical(origHistory, curHistory);

            // Modify the 'source' a tiny little bit:
            // - delete parent1
            // - add parent3
            var newModel = PrefabUtility.InstantiatePrefab(m_source) as GameObject;
            GameObject.DestroyImmediate(newModel.transform.Find("Parent1").gameObject);
            CreateGameObject("Parent3", newModel.transform);
            var newHistory = FbxSource.FbxRepresentation.FromTransform(newModel.transform);

            // Export it to clobber the old FBX file.
            FbxExporters.Editor.ModelExporter.ExportObjects (
                    AssetDatabase.GetAssetPath(m_source),
                    new Object[] { newModel } );
            AssetDatabase.Refresh();

            // Sync to the new source. This will already have been done
            // automatically if the post-import hook is installed.
            m_fbxSource.SyncPrefab();
            AssetDatabase.Refresh();

            // Assert the fbxSource changed.
            curHistory = FbxSource.FbxRepresentation.FromTransform(m_fbxSource.transform);
            AssertAreIdentical(newHistory, curHistory);
            AssertAreDifferent(origHistory, curHistory);

            // Check the history also changed.
            curHistory = m_fbxSource.GetFbxHistory();
            AssertAreIdentical(newHistory, curHistory);
            AssertAreDifferent(origHistory, curHistory);
        }
    }
}
