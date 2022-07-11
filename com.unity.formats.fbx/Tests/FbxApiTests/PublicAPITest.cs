using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Linq;
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
                GameObject.DestroyImmediate (go);
            }
            base.Term();
        }

        /// <summary>
        /// Creates a GameObject to export.
        /// </summary>
        /// <returns>The game object to export.</returns>
        private GameObject CreateGameObjectToExport (PrimitiveType type = PrimitiveType.Sphere)
        {
            return GameObject.CreatePrimitive (type);
        }

        [Test]
        public void TestExportObject()
        {
            Assert.IsNotNull (m_toExport);
            Assert.IsNotNull (m_toExport[0]);
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
            Assert.IsNotNull (m_toExport);
            Assert.Greater(m_toExport.Length, 1);
            var filename = GetRandomFbxFilePath();

            var fbxFileName = ModelExporter.ExportObjects(filename, m_toExport);

            Assert.IsNotNull (fbxFileName);
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
            var exportSettings = new ExportModelSettingsSerialize();
            exportSettings.SetAnimatedSkinnedMesh(true);
            exportSettings.SetAnimationDest(m_toExport[0].transform);
            exportSettings.SetAnimationSource(m_toExport[0].transform);
            exportSettings.SetEmbedTextures(false);
            exportSettings.SetExportFormat(ExportFormat.Binary);
            exportSettings.SetExportUnrendered(true);
            exportSettings.SetKeepInstances(false);
            exportSettings.SetLODExportType(LODExportType.Highest);
            exportSettings.SetModelAnimIncludeOption(Include.Model);
            exportSettings.SetObjectPosition(ObjectPosition.WorldAbsolute);
            exportSettings.SetPreserveImportSettings(false);
            exportSettings.SetUseMayaCompatibleNames(false);

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
            var exportSettings = new ExportModelSettingsSerialize();
            exportSettings.SetAnimatedSkinnedMesh(true);
            exportSettings.SetAnimationDest(m_toExport[0].transform);
            exportSettings.SetAnimationSource(m_toExport[0].transform);
            exportSettings.SetEmbedTextures(false);
            exportSettings.SetExportFormat(ExportFormat.Binary);
            exportSettings.SetExportUnrendered(true);
            exportSettings.SetKeepInstances(false);
            exportSettings.SetLODExportType(LODExportType.Highest);
            exportSettings.SetModelAnimIncludeOption(Include.Model);
            exportSettings.SetObjectPosition(ObjectPosition.WorldAbsolute);
            exportSettings.SetPreserveImportSettings(false);
            exportSettings.SetUseMayaCompatibleNames(false);

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

            ConvertToPrefabSettingsSerialize convertSettings = new ConvertToPrefabSettingsSerialize();
            convertSettings.SetExportFormat(ExportFormat.Binary);

            var result = ConvertToNestedPrefab.Convert(m_toExport[0], fbxFullPath: filename, prefabFullPath: prefabFilename, exportOptions: convertSettings);

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
    }
}
