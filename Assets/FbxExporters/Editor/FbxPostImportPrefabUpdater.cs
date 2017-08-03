using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FbxExporters
{
    public class FbxPostImportPrefabUpdater : UnityEditor.AssetPostprocessor
    {
        public static string FindFbxSourceAssetPath()
        {
            var allGuids = AssetDatabase.FindAssets("FbxSource t:MonoScript");
            switch (allGuids.Length) {
                case 0:
                    Debug.LogError("can't find FbxSource.cs somehow?!?");
                    return "";
                case 1:
                    return AssetDatabase.GUIDToAssetPath(allGuids[0]);
                default:
                    Debug.LogWarning(string.Format("{0} versions of FbxSource.cs somehow?!?", allGuids.Length));
                    return AssetDatabase.GUIDToAssetPath(allGuids[0]);
            }
        }

        public static bool IsFbxAsset(string assetPath) {
            return assetPath.EndsWith(".fbx");
        }

        public static bool IsPrefabAsset(string assetPath) {
            return assetPath.EndsWith(".prefab");
        }

        /// <summary>
        /// Return false if the prefab definitely does not have an
        /// FbxSource component that points to one of the Fbx assets
        /// that were imported.
        ///
        /// May return a false positive. This is a cheap check.
        /// </summary>
        public static bool MayHaveFbxSourceToFbxAsset(string prefabPath,
                string fbxSourceScriptPath, HashSet<string> fbxImported) {
            var depPaths = AssetDatabase.GetDependencies(prefabPath, recursive: false);
            bool dependsOnFbxSource = false;
            bool dependsOnImportedFbx = false;
            foreach(var dep in depPaths) {
                if (dep == fbxSourceScriptPath) {
                    if (dependsOnImportedFbx) { return true; }
                    dependsOnFbxSource = true;
                } else if (fbxImported.Contains(dep)) {
                    if (dependsOnFbxSource) { return true; }
                    dependsOnImportedFbx = true;
                }
            }
            // Either none or only one of the conditions was true, which
            // means this prefab certainly doesn't match.
            return false;
        }

        static void OnPostprocessAllAssets(string [] imported, string [] deleted, string [] moved, string [] movedFrom)
        {
            // Did we import an fbx file at all?
            // Optimize to not allocate in the common case of 'no'
            HashSet<string> fbxImported = null;
            foreach(var fbxModel in imported) {
                if (IsFbxAsset(fbxModel)) {
                    if (fbxImported == null) { fbxImported = new HashSet<string>(); }
                    fbxImported.Add(fbxModel);
                }
            }
            if (fbxImported == null) {
                return;
            }

            //
            // Iterate over all the prefabs that have an FbxSource component that
            // points to an FBX file that got (re)-imported.
            //
            // There's no one-line query to get those, so we search for a much
            // larger set and whittle it down, hopefully without needing to
            // load the asset into memory if it's not necessary.
            //
            var fbxSourceScriptPath = FindFbxSourceAssetPath();
            var allObjectGuids = AssetDatabase.FindAssets("t:GameObject");
            foreach(var guid in allObjectGuids) {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsPrefabAsset(prefabPath)) {
                    continue;
                }
                if (!MayHaveFbxSourceToFbxAsset(prefabPath, fbxSourceScriptPath, fbxImported)) {
                    continue;
                }

                // We're now guaranteed that this is a prefab, and it depends
                // on the FbxSource script, and it depends on an Fbx file that
                // was imported.
                //
                // To be sure it has an FbxSource component that points to an
                // Fbx file, we need to load the asset (which we need to do to
                // update the prefab anyway).
                var prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
                if (!prefab) {
                    Debug.LogWarning("FbxSource reimport: failed to update prefab " + prefabPath);
                    continue;
                }
                foreach(var fbxSourceComponent in prefab.GetComponentsInChildren<FbxSource>()) {
                    var fbxAssetPath = fbxSourceComponent.GetFbxAssetPath();
                    if (!fbxImported.Contains(fbxAssetPath)) {
                        continue;
                    }
                    fbxSourceComponent.SyncPrefab();
                }
            }
        }
    }
}
