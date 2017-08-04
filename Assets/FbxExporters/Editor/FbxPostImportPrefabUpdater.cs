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
            // Find guids that are scripts that look like FbxSource.
            // That catches FbxSourceTest too, so we have to make sure.
            var allGuids = AssetDatabase.FindAssets("FbxSource t:MonoScript");
            foreach(var guid in allGuids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/FbxSource.cs")) {
                    return path;
                }
            }
            Debug.LogError("can't find FbxSource.cs somehow?!?");
            return "";
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
            Debug.Log("Postprocessing...");

            // Did we import an fbx file at all?
            // Optimize to not allocate in the common case of 'no'
            HashSet<string> fbxImported = null;
            foreach(var fbxModel in imported) {
                if (IsFbxAsset(fbxModel)) {
                    if (fbxImported == null) { fbxImported = new HashSet<string>(); }
                    fbxImported.Add(fbxModel);
                    Debug.Log("Tracking fbx asset " + fbxModel);
                } else {
                    Debug.Log("Not an fbx asset " + fbxModel);
                }
            }
            if (fbxImported == null) {
                Debug.Log("No fbx imported");
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
                    Debug.Log("Not a prefab: " + prefabPath);
                    continue;
                }
                if (!MayHaveFbxSourceToFbxAsset(prefabPath, fbxSourceScriptPath, fbxImported)) {
                    Debug.Log("No dependence: " + prefabPath);
                    continue;
                }
                Debug.Log("Considering updating prefab " + prefabPath);

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
                        Debug.Log("No dependence: " + prefabPath + " via " + fbxAssetPath);
                        continue;
                    }
                    Debug.Log("Updating " + prefabPath + "...");
                    fbxSourceComponent.SyncPrefab();
                }
            }
        }
    }
}
