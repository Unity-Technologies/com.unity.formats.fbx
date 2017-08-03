// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using FbxSdk;

namespace FbxExporters.UnitTests
{
    public abstract class ExporterTestBase
    {
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

            if (pathName == null) {
                pathName = this.filePath;
            }

            // repeat until you find a file that does not already exist
            do {
                temp = Path.Combine (pathName, MakeFileName(prefixName: prefixName, extName: extName));

            } while(File.Exists (temp));

            return temp;
        }

        void DeleteOnNextUpdate()
        {
            Directory.Delete(filePath, recursive: true);
            AssetDatabase.Refresh();
            EditorApplication.update -= DeleteOnNextUpdate;
        }

        public virtual void Term ()
        {
            if (string.IsNullOrEmpty(_testDirectory)) {
                return;
            }

            // Delete the directory on the next editor update.  Otherwise,
            // prefabs don't get deleted and the directory delete fails.
            EditorApplication.update += DeleteOnNextUpdate;
        }

        /// <summary>
        /// Exports the Objects in selected.
        /// </summary>
        /// <returns>Root of Model Prefab.</returns>
        /// <param name="selected">Objects to export.</param>
        protected virtual GameObject ExportSelection(Object[] selected)
        {
            // export selected to a file, then return the root
            var filename = GetRandomFileNamePath();

            Debug.unityLogger.logEnabled = false;
            var fbxFileName = FbxExporters.Editor.ModelExporter.ExportObjects (filename, selected) as string;
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
    }
}
