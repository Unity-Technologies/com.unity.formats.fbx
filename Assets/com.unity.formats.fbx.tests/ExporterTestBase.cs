using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Formats.Fbx.Exporter.API.UnitTests;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    public abstract class ExporterTestBase: ExporterTestBaseAPI
    {
        bool isAutoUpdaterOn;

        [TearDown]
        public override void Term ()
        {
            // Put back the initial setting for the auto-updater toggle
            ExportSettings.instance.AutoUpdaterEnabled = isAutoUpdaterOn;
            base.Term();
        }

        [SetUp]
        public override void Init()
        {
            base.Init();
            isAutoUpdaterOn = ExportSettings.instance.AutoUpdaterEnabled;
            ExportSettings.instance.AutoUpdaterEnabled = true;
        }

        /// <summary>
        /// Exports the Objects in selected.
        /// </summary>
        /// <returns>Root of Model Prefab.</returns>
        /// <param name="selected">Objects to export.</param>
        internal virtual GameObject ExportSelection(Object selected, IExportOptions exportOptions = null)
        {
            // export selected to a file, then return the root
            var filename = GetRandomFileNamePath();
            return ExportSelection (filename, selected, exportOptions);
        }

        internal virtual GameObject ExportSelection(Object[] selected, IExportOptions exportOptions = null){
            var filename = GetRandomFileNamePath();
            return ExportSelection (filename, selected, exportOptions);
        }

        internal virtual GameObject ExportSelection(string filename, Object selected, IExportOptions exportOptions = null)
        {
            // export selected to a file, then return the root
            return ExportSelection (filename, new Object[]{selected}, exportOptions);
        }

        internal virtual GameObject ExportSelection(string filename, Object[] selected, IExportOptions exportOptions = null){
            Debug.unityLogger.logEnabled = false;
            var fbxFileName = ModelExporter.ExportObjects (filename, selected, exportOptions) as string;
            Debug.unityLogger.logEnabled = true;

            Assert.IsNotNull (fbxFileName);

            // make filepath relative to project folder
            if (fbxFileName.StartsWith (Application.dataPath, System.StringComparison.CurrentCulture))
            {
                fbxFileName = "Assets" + fbxFileName.Substring (Application.dataPath.Length);
            }
            // refresh the assetdata base so that we can query for the model
            AssetDatabase.Refresh ();

            Object unityMainAsset = AssetDatabase.LoadMainAssetAtPath (fbxFileName);
            var fbxRoot = unityMainAsset as GameObject;

            Assert.IsNotNull (fbxRoot);
            return fbxRoot;
        }

        /// <summary>
        /// Exports a single hierarchy to a random fbx file.
        /// </summary>
        /// <returns>The exported fbx file path.</returns>
        /// <param name="hierarchy">Hierarchy.</param>
        /// <param name="animOnly">If set to <c>true</c> export animation only.</param>
        internal GameObject ExportToFbx (
            GameObject hierarchy, bool animOnly = false,
            ExportSettings.LODExportType lodExportType = ExportSettings.LODExportType.All
        ){
            string filename = GetRandomFbxFilePath ();
            var exportOptions = new ExportModelSettingsSerialize ();
            exportOptions.SetLODExportType(lodExportType);
            if (animOnly) {
                exportOptions.SetModelAnimIncludeOption(ExportSettings.Include.Anim);
            }
            var exportedFilePath = ModelExporter.ExportObject (
                filename, hierarchy, exportOptions
            );
            Assert.That (exportedFilePath, Is.EqualTo (filename));
            var exported = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            return exported;
        }

        internal void ExportSkinnedMesh(string fileToExport, out SkinnedMeshRenderer originalSkinnedMesh, out SkinnedMeshRenderer exportedSkinnedMesh){
            // add fbx to scene
            GameObject originalFbxObj = AssetDatabase.LoadMainAssetAtPath(fileToExport) as GameObject;
            Assert.IsNotNull (originalFbxObj);
            GameObject originalGO = GameObject.Instantiate (originalFbxObj);
            Assert.IsTrue (originalGO);

            // export fbx
            // get GameObject
            string filename = GetRandomFbxFilePath();
            ModelExporter.ExportObject (filename, originalGO);
            GameObject fbxObj = AssetDatabase.LoadMainAssetAtPath(filename) as GameObject;
            Assert.IsTrue (fbxObj);

            originalSkinnedMesh = originalGO.GetComponentInChildren<SkinnedMeshRenderer> ();
            Assert.IsNotNull (originalSkinnedMesh);

            exportedSkinnedMesh = fbxObj.GetComponentInChildren<SkinnedMeshRenderer> ();
            Assert.IsNotNull (exportedSkinnedMesh);
        }

        public class Vector3Comparer : IComparer<Vector3>
        {
            public int Compare(Vector3 a, Vector3 b)
            {
                Assert.That(a.x, Is.EqualTo(b.x).Within(0.00001f));
                Assert.That(a.y, Is.EqualTo(b.y).Within(0.00001f));
                Assert.That(a.z, Is.EqualTo(b.z).Within(0.00001f));
                return 0;  // we're almost equal
            }
        }

    }
}
