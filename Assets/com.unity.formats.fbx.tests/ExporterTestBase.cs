﻿using UnityEngine;
using NUnit.Framework;
using System.IO;

namespace UnityEditor.Formats.Fbx.Exporter.UnitTests
{
    public abstract class ExporterTestBase
    {
        /// <summary>
        /// Path to the directory that holds the tests.
        ///
        /// Use this path if you want to load some data for testing, e.g. a
        /// material or scene or fbx.
        /// </summary>
        public const string PathToTestData = "Assets/com.unity.formats.fbx.tests";

        bool isAutoUpdaterOn;
        /// <summary>
        /// Sleep an amount of time (in ms) so we can safely assume that the
        /// timestamp on an fbx will change.
        /// </summary>
        public void SleepForFileTimestamp() {
            System.Threading.Thread.Sleep(1000);
        }


        private string _testDirectory;
        protected string filePath {
            get {
                if (string.IsNullOrEmpty(_testDirectory)) {
                    // Create a directory in the asset path.
                    _testDirectory = GetRandomFileNamePath("Assets", extName: "");
                    System.IO.Directory.CreateDirectory(_testDirectory);
                }
                return _testDirectory;
            }
        }

        private string _fileNamePrefix;
        protected string fileNamePrefix { get { return string.IsNullOrEmpty(_fileNamePrefix) ? "_safe_to_delete__" : _fileNamePrefix; }
            set { _fileNamePrefix = value; } }

        private string _fileNameExt;
        protected string fileNameExt    { get { return string.IsNullOrEmpty(_fileNameExt) ? ".fbx" : _fileNameExt; } set { _fileNameExt = value; } }

        private string MakeFileName(string baseName = null, string prefixName = null, string extName = null)
        {
            if (baseName==null) {
                // GetRandomFileName makes a random 8.3 filename
                // We don't want the extension.
                baseName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            }

            if (prefixName==null)
                prefixName = this.fileNamePrefix;

            if (extName==null)
                extName = this.fileNameExt;

            return prefixName + baseName + extName;
        }

        /// <summary>
        /// Create a random path for a file.
        ///
        /// By default the pathName is null, which defaults to a particular
        /// folder in the Assets folder that will be deleted on termination of
        /// this test.
        ///
        /// By default the prefix is a fixed string. You can set it differently if you care.
        ///
        /// By default the extension is ".fbx". You can set it differently if you care.
        ///
        /// By default we use platform path separators. If you want a random
        /// asset path e.g. for AssetDatabase.LoadMainAssetAtPath or for
        /// PrefabUtility.CreatePrefab, you need to use the '/' as separator
        /// (even on windows).
        ///
        /// See also convenience functions like:
        ///     GetRandomPrefabAssetPath()
        ///     GetRandomFbxFilePath()
        /// </summary>
        protected string GetRandomFileNamePath(
                string pathName = null,
                string prefixName = null,
                string extName = null,
                bool unityPathSeparator = false)
        {
            string temp;

            if (pathName == null) {
                pathName = this.filePath;
            }

            // repeat until you find a file that does not already exist
            do {
                temp = Path.Combine (pathName, MakeFileName(prefixName: prefixName, extName: extName));
            } while(File.Exists (temp));

            // Unity asset paths need a slash on all platforms.
            if (unityPathSeparator) {
                temp = temp.Replace('\\', '/');
            }

            return temp;
        }

        /// <summary>
        /// Return a random .fbx path that you can use in
        /// the File APIs.
        /// </summary>
        protected string GetRandomFbxFilePath() {
            return GetRandomFileNamePath(extName: ".fbx", unityPathSeparator: false);
        }

        /// <summary>
        /// Return a random .prefab path that you can use in
        /// PrefabUtility.CreatePrefab.
        /// </summary>
        protected string GetRandomPrefabAssetPath() {
            return GetRandomFileNamePath(extName: ".prefab", unityPathSeparator: true);
        }

        /// <summary>
        /// Creates a test hierarchy of cubes.
        ///      Root
        ///      -> Parent1
        ///      ----> Child1
        ///      ----> Child2
        ///      -> Parent2
        ///      ----> Child3
        /// </summary>
        /// <returns>The hierarchy root.</returns>
        public GameObject CreateHierarchy (string rootname = "Root")
        {
            var root = new GameObject (rootname);
            SetTransform (root.transform,
                new Vector3 (3, 4, -6),
                new Vector3 (45, 10, 34),
                new Vector3 (2, 1, 3));

            var parent1 = CreateGameObject ("Parent1", root.transform);
            SetTransform (parent1.transform,
                new Vector3 (53, 0, -1),
                new Vector3 (0, 5, 0),
                new Vector3 (1, 1, 1));

            var parent2 = CreateGameObject ("Parent2", root.transform);
            SetTransform (parent2.transform,
                new Vector3 (0, 0, 0),
                new Vector3 (90, 1, 3),
                new Vector3 (1, 0.3f, 0.5f));

            parent1.transform.SetAsFirstSibling ();

            CreateGameObject ("Child1", parent1.transform);
            CreateGameObject ("Child2", parent1.transform);
            CreateGameObject ("Child3", parent2.transform);

            return root;
        }

        /// <summary>
        /// Sets the transform.
        /// </summary>
        /// <param name="t">Transform.</param>
        /// <param name="pos">Position.</param>
        /// <param name="rot">Rotation.</param>
        /// <param name="scale">Scale.</param>
        private void SetTransform (Transform t, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            t.localPosition = pos;
            t.localEulerAngles = rot;
            t.localScale = scale;
        }

        /// <summary>
        /// Creates a GameObject.
        /// </summary>
        public GameObject CreateGameObject (string name, Transform parent = null, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive (type);
            go.name = name;
            go.transform.SetParent (parent);
            return go;
        }

        // Helper for the tear-down. This is run from the editor's update loop.
        void DeleteOnNextUpdate()
        {
            EditorApplication.update -= DeleteOnNextUpdate;
            try {
                Directory.Delete(filePath, recursive: true);
                AssetDatabase.Refresh();
            } catch(IOException) {
                // ignore -- something else must have deleted this.
            }
        }

        [TearDown]
        public virtual void Term ()
        {
            if (string.IsNullOrEmpty(_testDirectory)) {
                return;
            }

            // Delete the directory on the next editor update.  Otherwise,
            // prefabs don't get deleted and the directory delete fails.
            EditorApplication.update += DeleteOnNextUpdate;

            // Put back the initial setting for the auto-updater toggle
            ExportSettings.instance.AutoUpdaterEnabled = isAutoUpdaterOn;
        }

        [SetUp]
        public virtual void Init()
        {
            isAutoUpdaterOn = ExportSettings.instance.AutoUpdaterEnabled;
            ExportSettings.instance.AutoUpdaterEnabled = true;
        }


        /// <summary>
        /// Exports the Objects in selected.
        /// </summary>
        /// <returns>Root of Model Prefab.</returns>
        /// <param name="selected">Objects to export.</param>
        protected virtual GameObject ExportSelection(Object selected, IExportOptions exportOptions = null)
        {
            // export selected to a file, then return the root
            var filename = GetRandomFileNamePath();
            return ExportSelection (filename, selected, exportOptions);
        }

        protected virtual GameObject ExportSelection(Object[] selected, IExportOptions exportOptions = null){
            var filename = GetRandomFileNamePath();
            return ExportSelection (filename, selected, exportOptions);
        }

        protected virtual GameObject ExportSelection(string filename, Object selected, IExportOptions exportOptions = null)
        {
            // export selected to a file, then return the root
            return ExportSelection (filename, new Object[]{selected}, exportOptions);
        }

        protected virtual GameObject ExportSelection(string filename, Object[] selected, IExportOptions exportOptions = null){
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
        protected GameObject ExportToFbx (
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

        /// <summary>
        /// Adds the asset at asset path to the scene.
        /// </summary>
        /// <returns>The new GameObject in the scene.</returns>
        /// <param name="assetPath">Asset path.</param>
        protected GameObject AddAssetToScene(string assetPath){
            if (!assetPath.StartsWith("Assets/")) {
                assetPath = "Assets/" + assetPath;
            }
            GameObject originalObj = AssetDatabase.LoadMainAssetAtPath (assetPath) as GameObject;
            Assert.IsNotNull (originalObj);
            GameObject originalGO = GameObject.Instantiate (originalObj);
            Assert.IsTrue (originalGO);

            return originalGO;
        }

        /// <summary>
        /// Compares two hierarchies, asserts that they match precisely.
        /// The root can be allowed to mismatch. That's normal with
        /// GameObject.Instantiate.
        /// </summary>
        public static void AssertSameHierarchy (
            GameObject expectedHierarchy, GameObject actualHierarchy,
            bool ignoreRootName = false, bool ignoreRootTransform = false)
        {
            if (!ignoreRootName) {
                Assert.AreEqual (expectedHierarchy.name, actualHierarchy.name);
            }

            var expectedTransform = expectedHierarchy.transform;
            var actualTransform = actualHierarchy.transform;

            if (!ignoreRootTransform) {
                Assert.AreEqual (expectedTransform, actualTransform);
            }

            Assert.AreEqual (expectedTransform.childCount, actualTransform.childCount);

            foreach (Transform expectedChild in expectedTransform) {
                var actualChild = actualTransform.Find (expectedChild.name);
                Assert.IsNotNull (actualChild);
                AssertSameHierarchy (expectedChild.gameObject, actualChild.gameObject);
            }
        }

        /// <summary>
        /// Given a relative path starting from the folder that contains the
        /// unit tests, generate the corresponding Unity virtual path.
        ///
        /// Return null if the file doesn't actually exist.
        /// </summary>
        public string FindPathInUnitTests(string path) {
            // This used to be complicated; not so much anymore.
            var virtualPath = PathToTestData + '/' + path;
            if (!System.IO.File.Exists(virtualPath)) {
                return null;
            }
            return virtualPath;
        }
    }
}
