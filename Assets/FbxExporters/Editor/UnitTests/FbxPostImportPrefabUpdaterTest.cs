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
        List<GameObject> m_goToDestroy = new List<GameObject>();
        List<GameObject> m_assetToDestroy = new List<GameObject>();

        [SetUp]
        public void Init ()
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            m_goToDestroy.Add(capsule);
            m_fbx = ExportSelection(new Object[] { capsule });
            m_assetToDestroy.Add(m_fbx);
            m_fbxPath = AssetDatabase.GetAssetPath(m_fbx);

            // Instantiate the fbx and create a prefab from it.
            var fbxInstance = PrefabUtility.InstantiatePrefab(m_fbx) as GameObject;
            fbxInstance.AddComponent<FbxSource>().SetSourceModel(m_fbx);
            m_prefabPath = GetRandomFileNamePath(extName: ".prefab");
            m_prefab = PrefabUtility.CreatePrefab(m_prefabPath, fbxInstance);
            m_assetToDestroy.Add(m_prefab);
            AssetDatabase.Refresh ();
            Assert.AreEqual(m_prefabPath, AssetDatabase.GetAssetPath(m_prefab));
            GameObject.DestroyImmediate(fbxInstance);
        }

        [TearDown]
        public override void Term ()
        {
            // Important to first lose all connection to the assets.
            foreach(var toDestroy in m_goToDestroy) {
                if (toDestroy) {
                    UnityEngine.Object.DestroyImmediate (toDestroy);
                }
            }
            foreach(var toDestroy in m_assetToDestroy) {
                if (toDestroy) {
                    UnityEngine.Object.DestroyImmediate (toDestroy, true);
                }
            }

            base.Term ();
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
            m_goToDestroy.Add(oldInstance);
            Assert.IsTrue(oldInstance);

            // Create a new hierarchy. It's marked for delete already.
            var newHierarchy = CreateHierarchy();

            // Export it to the same fbx path.
            FbxExporters.Editor.ModelExporter.ExportObjects (m_fbxPath, new Object[] { newHierarchy });

            // Verify that a new instance of the prefab got updated.
            var newInstance = PrefabUtility.InstantiatePrefab(m_prefab) as GameObject;
            m_goToDestroy.Add(newInstance);
            Assert.IsTrue(newInstance);
            AssertSameHierarchy(newHierarchy, newInstance, ignoreRootName: true, ignoreRootTransform: true);

            // Verify that the old instance also got updated.
            AssertSameHierarchy(newHierarchy, oldInstance, ignoreRootName: true, ignoreRootTransform: true);
        }

        /// <summary>
        /// Creates test hierarchy. It'll be destroyed upon termination.
        /// </summary>
        /// <returns>The hierarchy root.</returns>
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
        /// Creates a GameObject. The object will be destroyed upon termination.
        /// </summary>
        /// <returns>The created GameObject.</returns>
        /// <param name="name">Name.</param>
        /// <param name="parent">Parent.</param>
        private GameObject CreateGameObject (string name, Transform parent = null)
        {
            var go = new GameObject (name);
            go.transform.SetParent (parent);
            m_goToDestroy.Add(go);
            return go;
        }
    }
}
