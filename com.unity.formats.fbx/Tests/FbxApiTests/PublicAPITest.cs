using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Diagnostics;
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
            return GameObject.CreatePrimitive (PrimitiveType.Sphere);
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
    }
}
