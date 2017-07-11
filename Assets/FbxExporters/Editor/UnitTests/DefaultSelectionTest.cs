using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.IO;
using System.Collections.Generic;

namespace FbxExporters.UnitTests
{
    /// <summary>
    /// Tests the default selection export behavior.
    /// Tests that the right GameObjects are exported and
    /// that they have the expected transforms.
    /// </summary>
    public class DefaultSelectionTest
    {
        private string _filePath;
        protected string filePath       { get { return string.IsNullOrEmpty(_filePath) ? Application.dataPath : _filePath; } set { _filePath = value; } }

        private string _fileNamePrefix;
        protected string fileNamePrefix { get { return string.IsNullOrEmpty(_fileNamePrefix) ? "_safe_to_delete__" : _fileNamePrefix; }
            set { _fileNamePrefix = value; } }

        private string _fileNameExt;
        protected string fileNameExt    { get { return string.IsNullOrEmpty(_fileNameExt) ? ".fbx" : _fileNameExt; } set { _fileNameExt = value; } }

        private string MakeFileName(string baseName = null, string prefixName = null, string extName = null)
        {
            if (baseName==null)
                baseName = Path.GetRandomFileName();

            if (prefixName==null)
                prefixName = this.fileNamePrefix;

            if (extName==null)
                extName = this.fileNameExt;

            return prefixName + baseName + extName;
        }

        protected string GetRandomFileNamePath(string pathName = null, string prefixName = null, string extName = null)
        {
            string temp;

            if (pathName==null)
                pathName = this.filePath;

            if (prefixName==null)
                prefixName = this.fileNamePrefix;

            if (extName==null)
                extName = this.fileNameExt;

            // repeat until you find a file that does not already exist
            do {
                temp = Path.Combine (pathName, MakeFileName(prefixName: prefixName, extName: extName));

            } while(File.Exists (temp));

            return temp;
        }

        [TearDown]
        public void Term ()
        {
            foreach (string file in Directory.GetFiles (this.filePath, MakeFileName("*"))) {
                File.Delete (file);
            }
        }

        [Test]
        public void TestDefaultSelection ()
        {
            var root = CreateHierarchy ();
            Assert.IsNotNull (root);

            var exportedRoot = ExportSelection (root, new Object[]{root});

            // test Export Root
            // Expected result: everything gets exported

            // test Export Parent1, Child1
            // Expected result: Parent1, Child1, Child2

            // test Export Child2
            // Expected result: Child2

            // test Export Child2, Parent2
            // Expected result: Parent2, Child3, Child2

            //UnityEngine.Object.DestroyImmediate (root);
        }

        private GameObject CreateHierarchy ()
        {
            // Create the following hierarchy:
            //      Root
            //      -> Parent1
            //      ----> Child1
            //      ----> Child2
            //      -> Parent2
            //      ----> Child3

            var root = CreateGameObject ("Root");

            var parent1 = CreateGameObject ("Parent1", root.transform);
            var parent2 = CreateGameObject ("Parent2", root.transform);
            parent1.transform.SetAsFirstSibling ();

            CreateGameObject ("Child1", parent1.transform);
            CreateGameObject ("Child2", parent1.transform);
            CreateGameObject ("Child3", parent2.transform);

            return root;
        }

        private GameObject CreateGameObject(string name, Transform parent = null)
        {
            var go = new GameObject (name);
            go.transform.SetParent (parent);
            return go;
        }

        private void CompareHierarchies(GameObject expectedHierarchy, GameObject actualHierarchy, bool ignoreRoot = false)
        {
            if (!ignoreRoot) {
                Assert.AreEqual (expectedHierarchy.name, actualHierarchy.name);
                Assert.AreEqual (expectedHierarchy.transform.childCount, actualHierarchy.transform.childCount);
            }
        }

        private GameObject ExportSelection(GameObject origRoot, Object[] selected)
        {
            // export selected to a file, then return the root
            var filename = GetRandomFileNamePath();

            Debug.unityLogger.logEnabled = false;
            var fbxFileName = FbxExporters.Editor.ModelExporter.ExportObjects (filename, selected) as string;
            Debug.unityLogger.logEnabled = true;

            Debug.LogWarning (filename + ", " + fbxFileName);
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
    }
}