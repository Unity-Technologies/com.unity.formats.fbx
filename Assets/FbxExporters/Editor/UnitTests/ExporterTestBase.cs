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
using Unity.FbxSdk;

namespace FbxExporters.UnitTests
{
    public abstract class ExporterTestBase
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
        public virtual void Term ()
        {
            foreach (string file in Directory.GetFiles (this.filePath, MakeFileName("*"))) {
                File.Delete (file);
            }
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
    }
}