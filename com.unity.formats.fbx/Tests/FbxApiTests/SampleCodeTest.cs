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

            ExportModelOptions exportSettings = new ExportModelOptions();
            exportSettings.ExportFormat = ExportFormat.Binary;
            exportSettings.KeepInstances = false;

            // Note: If you don't pass any export settings, Unity uses the default settings.
            ModelExporter.ExportObjects(filePath, objects, exportSettings);

            // You can use ModelExporter.ExportObject instead of
            // ModelExporter.ExportObjects to export a single GameObject.
        }

        /// <summary>
        /// Convert GameObject sample function.
        /// </summary>
        /// <param name="go">The GameObject to convert.</param>
        public static GameObject ConvertGameObject(GameObject go)
        {
            string filePath = Path.Combine(Application.dataPath, "MyObject.fbx");
            string prefabPath = Path.Combine(Application.dataPath, "MyObject.prefab");

            // Settings to use when exporting the FBX to convert to a prefab.
            // Note: If you don't pass any export settings, Unity uses the default settings.
            ConvertToPrefabVariantOptions convertSettings = new ConvertToPrefabVariantOptions();
            convertSettings.ExportFormat = ExportFormat.Binary;

            // Returns the prefab variant linked to an FBX file.
            return ConvertToNestedPrefab.ConvertToPrefabVariant(go, fbxFullPath: filePath, prefabFullPath: prefabPath, convertOptions: convertSettings);
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

        [Test]
        public void TestConvertGameObjectSample()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.parent = cube.transform;

            var result = ConvertGameObject(cube);

            var filename = "MyObject.fbx";
            var prefabFilename = "MyObject.prefab";

            var exportPath = Path.Combine(Application.dataPath, filename);
            Assert.That(exportPath, Does.Exist);
            var prefabExportPath = Path.Combine(Application.dataPath, prefabFilename);
            Assert.That(prefabExportPath, Does.Exist);

            // original hierarchy should no longer exist
            Assert.IsTrue(!cube);
            Assert.IsTrue(result);

            // create a duplicate hierarchy to check it matches
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.parent = cube.transform;

            ConvertToNestedPrefabTest.AssertSameHierarchy(cube, result, ignoreRootName: true);

            // check FBX
            var assetPath = "Assets/" + filename;

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Assert.That(loaded, Is.Not.Null.Or.Empty);
            var loadedMeshes = (from loadedObj in loaded where loadedObj as Mesh != null select loadedObj as Mesh).ToArray();

            Assert.AreEqual(2, loadedMeshes.Length);
            foreach (var mesh in loadedMeshes)
            {
                Assert.Greater(mesh.triangles.Length, 0);
            }

            AssetDatabase.DeleteAsset("Assets/" + prefabFilename);
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