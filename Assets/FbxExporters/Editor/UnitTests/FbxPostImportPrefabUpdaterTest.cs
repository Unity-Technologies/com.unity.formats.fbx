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
using System.IO;
using System.Collections.Generic;
using FbxSdk;

namespace FbxExporters.UnitTests
{
    /// <summary>
    /// Test that the post-import prefab updater works properly,
    /// by triggering it to run.
    /// </summary>
    public class FbxPostImportPrefabUpdaterTest : ExporterTestBase
    {
        GameObject m_fbx;
        string m_fbxPath;
        GameObject m_prefab;
        string m_prefabPath;

        [SetUp]
        public void Init ()
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            m_fbx = ExportSelection(capsule);
            m_fbxPath = AssetDatabase.GetAssetPath(m_fbx);

            // Instantiate the fbx and create a prefab from it.
            // Delete the object right away (don't even wait for term).
            var fbxInstance = PrefabUtility.InstantiatePrefab(m_fbx) as GameObject;
            fbxInstance.AddComponent<FbxSource>().SetSourceModel(m_fbx);
            m_prefabPath = GetRandomFileNamePath(extName: ".prefab");
            m_prefab = PrefabUtility.CreatePrefab(m_prefabPath, fbxInstance);
            AssetDatabase.Refresh ();
            Assert.AreEqual(m_prefabPath, AssetDatabase.GetAssetPath(m_prefab));
            GameObject.DestroyImmediate(fbxInstance);
        }

        [Test]
        public void BasicTest ()
        {
            var fbxSourcePath = FbxPostImportPrefabUpdater.FindFbxSourceAssetPath();
            Assert.IsFalse(string.IsNullOrEmpty(fbxSourcePath));
            Assert.IsTrue(fbxSourcePath.EndsWith("FbxSource.cs"));

            Assert.IsTrue(FbxPostImportPrefabUpdater.IsFbxAsset("Assets/path/to/foo.fbx"));
            Assert.IsFalse(FbxPostImportPrefabUpdater.IsFbxAsset("Assets/path/to/foo.png"));

            Assert.IsTrue(FbxPostImportPrefabUpdater.IsPrefabAsset("Assets/path/to/foo.prefab"));
            Assert.IsFalse(FbxPostImportPrefabUpdater.IsPrefabAsset("Assets/path/to/foo.fbx"));
            Assert.IsFalse(FbxPostImportPrefabUpdater.IsPrefabAsset("Assets/path/to/foo.png"));

            var imported = new HashSet<string>( new string [] { "Assets/path/to/foo.fbx", m_fbxPath } );
            Assert.IsTrue(FbxPostImportPrefabUpdater.MayHaveFbxSourceToFbxAsset(m_prefabPath, fbxSourcePath,
                        imported));
        }

        [Test]
        public void ReplaceTest ()
        {
            // Instantiate the prefab.
            var oldInstance = PrefabUtility.InstantiatePrefab(m_prefab) as GameObject;
            Assert.IsTrue(oldInstance);

            // Create a new hierarchy. It's marked for delete already.
            var newHierarchy = CreateHierarchy();

            // Export it to the same fbx path.
            FbxExporters.Editor.ModelExporter.ExportObject(m_fbxPath, newHierarchy);
            AssetDatabase.Refresh();

            // Verify that a new instance of the prefab got updated.
            var newInstance = PrefabUtility.InstantiatePrefab(m_prefab) as GameObject;
            Assert.IsTrue(newInstance);
            AssertSameHierarchy(newHierarchy, newInstance, ignoreRootName: true, ignoreRootTransform: true);

            // Verify that the old instance also got updated.
            AssertSameHierarchy(newHierarchy, oldInstance, ignoreRootName: true, ignoreRootTransform: true);
        }

    }
}

namespace FbxExporters.PerformanceTests {

    class FbxPostImportPrefabUpdaterTestPerformance : FbxExporters.UnitTests.ExporterTestBase {
        [Test]
        public void ExpensivePerformanceTest ()
        {
            const int n = 200;
            const int NoUpdateTimeLimit = 500; // milliseconds
            const int OneUpdateTimeLimit = 500; // milliseconds

            var stopwatch = new System.Diagnostics.Stopwatch ();
            stopwatch.Start();

            // Create 1000 fbx models and 1000 prefabs.
            // Each prefab points to an fbx model.
            //
            // Then modify one fbx model. Shouldn't take longer than 1s.
            var hierarchy = CreateGameObject("the_root");
            var baseName = GetRandomFileNamePath(extName: "");
            FbxExporters.Editor.ModelExporter.ExportObject(baseName + ".fbx", hierarchy);

            // Create N fbx models by copying files. Import them all at once.
            var names = new string[n];
            names[0] = baseName;
            stopwatch.Reset();
            stopwatch.Start();
            for(int i = 1; i < n; ++i) {
                names[i] = GetRandomFileNamePath(extName : "");
                System.IO.File.Copy(names[0] + ".fbx", names[i] + ".fbx");
            }
            Debug.Log("Created fbx files in " + stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();
            AssetDatabase.Refresh();
            Debug.Log("Imported fbx files in " + stopwatch.ElapsedMilliseconds);

            // Create N/2 prefabs, each one depends on one of the fbx assets.
            // This loop is very slow, which is sad because it's not the point
            // of the test. That's the only reason we halve n.
            stopwatch.Reset();
            stopwatch.Start();
            var fbxFiles = new GameObject[n / 2];
            for(int i = 0; i < n / 2; ++i) {
                fbxFiles[i] = AssetDatabase.LoadMainAssetAtPath(names[i] + ".fbx") as GameObject;
                Assert.IsTrue(fbxFiles[i]);
            }
            Debug.Log("Loaded fbx files in " + stopwatch.ElapsedMilliseconds);

            stopwatch.Reset();
            stopwatch.Start();
            for(int i = 0; i < n / 2; ++i) {
                var instance = CreateGameObject("prefab_" + i);
                Assert.IsTrue(instance);
                var fbxSource = instance.AddComponent<FbxSource>();
                fbxSource.SetSourceModel(fbxFiles[i]);
                UnityEditor.PrefabUtility.CreatePrefab(names[i] + ".prefab", fbxFiles[i]);
            }
            Debug.Log("Created prefabs in " + stopwatch.ElapsedMilliseconds);

            // Export a new hierarchy and update one fbx file.
            var newHierarchy = CreateHierarchy();
            try {
                UnityEngine.Debug.unityLogger.logEnabled = false;
                stopwatch.Reset ();
                stopwatch.Start ();
                FbxExporters.Editor.ModelExporter.ExportObject(names[0] + ".fbx", newHierarchy);
                AssetDatabase.Refresh(); // force the update right now.
            } finally {
                UnityEngine.Debug.unityLogger.logEnabled = true;
            }
            Debug.Log("Import (one change) in " + stopwatch.ElapsedMilliseconds);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, NoUpdateTimeLimit);

            // Try what happens when nothing gets updated.
            try {
                UnityEngine.Debug.unityLogger.logEnabled = false;
                stopwatch.Reset ();
                stopwatch.Start ();
                string newHierarchyFbxFile = GetRandomFileNamePath(extName : ".fbx");
                File.Copy(names[0] + ".fbx", newHierarchyFbxFile);
                AssetDatabase.Refresh(); // force the update right now.
            } finally {
                UnityEngine.Debug.unityLogger.logEnabled = true;
            }
            Debug.Log("Import (no changes) in " + stopwatch.ElapsedMilliseconds);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, OneUpdateTimeLimit);
        }
    }
}
