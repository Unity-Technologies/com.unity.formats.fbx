using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Linq;
using System.IO;
using UnityEditor.Formats.Fbx.Exporter;

namespace FbxExporter.UnitTests
{
    public class PublicAPITest : ExporterTestBaseAPI
    {
        private GameObject[] m_toExport;

        [SetUp]
        public override void Init()
        {
            base.Init();
            m_toExport = new GameObject[] {CreateGameObjectToExport(PrimitiveType.Cube), CreateGameObjectToExport(PrimitiveType.Sphere) };
        }

        [TearDown]
        public override void Term()
        {
            foreach (var go in m_toExport)
            {
                GameObject.DestroyImmediate(go);
            }
            base.Term();
        }

        /// <summary>
        /// Creates a GameObject to export.
        /// </summary>
        /// <returns>The game object to export.</returns>
        private GameObject CreateGameObjectToExport(PrimitiveType type = PrimitiveType.Sphere)
        {
            return GameObject.CreatePrimitive(type);
        }

        [Test]
        public void TestExportObject()
        {
            Assert.IsNotNull(m_toExport);
            Assert.IsNotNull(m_toExport[0]);
            var filename = GetRandomFbxFilePath();

            var fbxFileName = ModelExporter.ExportObject(filename, m_toExport[0]);
            Assert.IsNotNull(fbxFileName);
            Assert.AreEqual(fbxFileName, filename);

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(filename);
            var loadedMeshes = (from loadedObj in loaded where loadedObj as Mesh != null select loadedObj as Mesh).ToArray();

            Assert.AreEqual(1, loadedMeshes.Length);
            Assert.Greater(loadedMeshes[0].triangles.Length, 0);
        }

        [Test]
        public void TestExportObjects()
        {
            Assert.IsNotNull(m_toExport);
            Assert.Greater(m_toExport.Length, 1);
            var filename = GetRandomFbxFilePath();

            var fbxFileName = ModelExporter.ExportObjects(filename, m_toExport);

            Assert.IsNotNull(fbxFileName);
            Assert.AreEqual(fbxFileName, filename);

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(filename);
            var loadedMeshes = (from loadedObj in loaded where loadedObj as Mesh != null select loadedObj as Mesh).ToArray();

            Assert.AreEqual(2, loadedMeshes.Length);
            foreach (var mesh in loadedMeshes)
            {
                Assert.Greater(mesh.triangles.Length, 0);
            }
        }

        [Test]
        public void TestExportObjectSettings()
        {
            Assert.IsNotNull(m_toExport);
            Assert.IsNotNull(m_toExport[0]);
            var filename = GetRandomFbxFilePath();

            // set all the settings that we can
            var exportSettings = new ExportModelOptions();
            exportSettings.AnimateSkinnedMesh = true;
            exportSettings.AnimationDest = m_toExport[0].transform;
            exportSettings.AnimationSource = m_toExport[0].transform;
            exportSettings.EmbedTextures = false;
            exportSettings.ExportFormat = ExportFormat.Binary;
            exportSettings.ExportUnrendered = true;
            exportSettings.KeepInstances = false;
            exportSettings.LODExportType = LODExportType.Highest;
            exportSettings.ModelAnimIncludeOption = Include.Model;
            exportSettings.ObjectPosition = ObjectPosition.WorldAbsolute;
            exportSettings.PreserveImportSettings = false;
            exportSettings.UseMayaCompatibleNames = false;

            var fbxFileName = ModelExporter.ExportObject(filename, m_toExport[0], exportSettings);
            Assert.IsNotNull(fbxFileName);
            Assert.AreEqual(fbxFileName, filename);

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(filename);
            var loadedMeshes = (from loadedObj in loaded where loadedObj as Mesh != null select loadedObj as Mesh).ToArray();

            Assert.AreEqual(1, loadedMeshes.Length);
            Assert.Greater(loadedMeshes[0].triangles.Length, 0);
        }

        [Test]
        public void TestExportObjectsSettings()
        {
            Assert.IsNotNull(m_toExport);
            Assert.Greater(m_toExport.Length, 1);
            var filename = GetRandomFbxFilePath();

            // set all the settings that we can
            var exportSettings = new ExportModelOptions();
            exportSettings.AnimateSkinnedMesh = true;
            exportSettings.AnimationDest = m_toExport[0].transform;
            exportSettings.AnimationSource = m_toExport[0].transform;
            exportSettings.EmbedTextures = false;
            exportSettings.ExportFormat = ExportFormat.Binary;
            exportSettings.ExportUnrendered = true;
            exportSettings.KeepInstances = false;
            exportSettings.LODExportType = LODExportType.Highest;
            exportSettings.ModelAnimIncludeOption = Include.Model;
            exportSettings.ObjectPosition = ObjectPosition.WorldAbsolute;
            exportSettings.PreserveImportSettings = false;
            exportSettings.UseMayaCompatibleNames = false;

            var fbxFileName = ModelExporter.ExportObjects(filename, m_toExport, exportSettings);

            Assert.IsNotNull(fbxFileName);
            Assert.AreEqual(fbxFileName, filename);

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(filename);
            var loadedMeshes = (from loadedObj in loaded where loadedObj as Mesh != null select loadedObj as Mesh).ToArray();

            Assert.AreEqual(2, loadedMeshes.Length);
            foreach (var mesh in loadedMeshes)
            {
                Assert.Greater(mesh.triangles.Length, 0);
            }
        }

        [Test]
        public void TestConvert()
        {
            Assert.IsNotNull(m_toExport);
            Assert.IsNotNull(m_toExport[0]);
            var filename = GetRandomFbxFilePath();
            var prefabFilename = GetRandomPrefabAssetPath();

            ConvertToPrefabVariantOptions convertSettings = new ConvertToPrefabVariantOptions();
            convertSettings.ExportFormat = ExportFormat.Binary;
            convertSettings.AnimationDest = m_toExport[0].transform;
            convertSettings.AnimationSource = m_toExport[0].transform;
            convertSettings.AnimateSkinnedMesh = false;
            convertSettings.UseMayaCompatibleNames = false;

            var result = ConvertToNestedPrefab.ConvertToPrefabVariant(m_toExport[0], fbxFullPath: filename, prefabFullPath: prefabFilename, convertOptions: convertSettings);

            Assert.IsTrue(result);
            Assert.IsTrue(!m_toExport[0]);

            var copy = CreateGameObjectToExport(PrimitiveType.Cube);
            ConvertToNestedPrefabTest.AssertSameHierarchy(copy, result, ignoreRootName: true);
        }

        // UT-3305 Test exporting an fbx outside the Assets folder of the project
        [Test]
        public void TestExportOutsideProject()
        {
            Assert.IsNotNull(m_toExport);
            Assert.Greater(m_toExport.Length, 1);
            var filename = GetTempOutsideFilePath();

            var fbxFileName = ModelExporter.ExportObjects(filename, m_toExport);

            Assert.IsNotNull(fbxFileName);
            Assert.AreEqual(fbxFileName, filename);
        }

        [Test]
        public void TestConvertWithNoPaths()
        {
            var expectedPrefabPath = "Assets/Cube.prefab";
            var expectedFbxPath = "Assets/Cube.fbx";

            // make sure asset does not already exist
            Assert.That(File.Exists(Path.GetFullPath(expectedPrefabPath)), Is.False);
            Assert.That(File.Exists(Path.GetFullPath(expectedFbxPath)), Is.False);

            Assert.IsNotNull(m_toExport);
            Assert.IsNotNull(m_toExport[0]);

            var result = ConvertToNestedPrefab.ConvertToPrefabVariant(m_toExport[0]);

            Assert.IsTrue(result);
            Assert.IsTrue(!m_toExport[0]);

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(result);
            Assert.That(prefabPath, Is.EqualTo(expectedPrefabPath));

            // get fbx path
            var fbxObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(result);
            var fbxPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(fbxObj);

            Assert.That(fbxPath, Is.EqualTo(expectedFbxPath));

            // delete at the end of the test
            AssetDatabase.DeleteAsset(prefabPath);
            AssetDatabase.DeleteAsset(fbxPath);
        }

        [Test]
        public void TestConvertOutsideProject()
        {
            Assert.IsNotNull(m_toExport);
            Assert.IsNotNull(m_toExport[0]);
            var tempFilePath = GetTempOutsideFilePath();

            // make sure that the filepath does not exist
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            Assert.That(File.Exists(tempFilePath), Is.False);

            var fbxFilePath = GetRandomFbxFilePath();

            Assert.Throws<FbxExportSettingsException>(() => ConvertToNestedPrefab.ConvertToPrefabVariant(m_toExport[0], fbxDirectoryFullPath: tempFilePath));

            // conversion should fail
            Assert.IsTrue(m_toExport[0]);

            Assert.Throws<FbxExportSettingsException>(() => ConvertToNestedPrefab.ConvertToPrefabVariant(m_toExport[0], fbxFullPath: tempFilePath));

            Assert.IsTrue(m_toExport[0]);

            Assert.Throws<FbxExportSettingsException>(() => ConvertToNestedPrefab.ConvertToPrefabVariant(m_toExport[0], fbxFullPath: fbxFilePath, prefabDirectoryFullPath: tempFilePath));

            Assert.IsTrue(m_toExport[0]);

            Assert.Throws<FbxExportSettingsException>(() => ConvertToNestedPrefab.ConvertToPrefabVariant(m_toExport[0], fbxFullPath: fbxFilePath, prefabFullPath: tempFilePath));

            Assert.IsTrue(m_toExport[0]);
        }
    }
}
