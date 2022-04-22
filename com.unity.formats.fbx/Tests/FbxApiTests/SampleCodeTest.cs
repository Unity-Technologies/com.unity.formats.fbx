using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Linq;
using UnityEditor.Formats.Fbx.Exporter;
using System.IO;
using Autodesk.Fbx;

namespace FbxExporter.UnitTests
{
    /// <summary>
    /// Unit tests for sample code included in documentation.
    /// </summary>
    public class SampleCodeTest : ExporterTestBaseAPI
    {
        // Export GameObjects sample function
        public static void ExportGameObjects(Object[] objects)
        {
            string filePath = Path.Combine(Application.dataPath, "MyGame.fbx");
            ModelExporter.ExportObjects(filePath, objects);

            // ModelExporter.ExportObject can be used instead of 
            // ModelExporter.ExportObjects to export a single game object
        }

        [Test]
        public void TestExportGameObjectsSample()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            ExportGameObjects(new Object[] { cube, sphere });

            var filename = "MyGame.fbx";
            var exportPath = Path.Combine(Application.dataPath, filename);
            Assert.That(exportPath, Does.Exist);

            var assetPath = "Assets/" + filename;

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Assert.That(loaded, Is.Not.Null.Or.Empty);
            var loadedMeshes = (from loadedObj in loaded where loadedObj as Mesh != null select loadedObj as Mesh).ToArray();

            Assert.AreEqual(2, loadedMeshes.Length);
            foreach (var mesh in loadedMeshes)
            {
                Assert.Greater(mesh.triangles.Length, 0);
            }

            AssetDatabase.DeleteAsset(assetPath);
        }

        // Export scene sample
        protected void ExportScene(string fileName)
        {
            using (FbxManager fbxManager = FbxManager.Create())
            {
                // configure IO settings.
                fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

                // Export the scene
                using (Autodesk.Fbx.FbxExporter exporter = Autodesk.Fbx.FbxExporter.Create(fbxManager, "myExporter"))
                {

                    // Initialize the exporter.
                    bool status = exporter.Initialize(fileName, -1, fbxManager.GetIOSettings());

                    // Create a new scene to export
                    FbxScene scene = FbxScene.Create(fbxManager, "myScene");

                    // Export the scene to the file.
                    exporter.Export(scene);
                }
            }
        }

        [Test]
        public void TestExportSceneSample()
        {
            var exportPath = GetRandomFileNamePath(extName: ".fbx");
            Assert.That(exportPath, Does.Not.Exist);

            ExportScene(exportPath);
            Assert.That(exportPath, Does.Exist);
        }

        // Import scene sample
        protected void ImportScene(string fileName)
        {
            using (FbxManager fbxManager = FbxManager.Create())
            {
                // configure IO settings.
                fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

                // Import the scene to make sure file is valid
                using (FbxImporter importer = FbxImporter.Create(fbxManager, "myImporter"))
                {

                    // Initialize the importer.
                    bool status = importer.Initialize(fileName, -1, fbxManager.GetIOSettings());

                    // Create a new scene so it can be populated by the imported file.
                    FbxScene scene = FbxScene.Create(fbxManager, "myScene");

                    // Import the contents of the file into the scene.
                    importer.Import(scene);
                }
            }
        }

        [Test]
        public void TestImportSceneSample()
        {
            var exportPath = GetRandomFileNamePath(extName: ".fbx");
            Assert.That(exportPath, Does.Not.Exist);

            ExportScene(exportPath);
            Assert.That(exportPath, Does.Exist);

            ImportScene(exportPath);
        }
    }
}