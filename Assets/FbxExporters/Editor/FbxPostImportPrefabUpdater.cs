using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FbxExporters
{
    public class FbxPostImportPrefabUpdater : UnityEditor.AssetPostprocessor
    {
        static bool AssetDependsOn(string assetPath, string otherAssetPath)
        {
            var depPaths = AssetDatabase.GetDependencies(assetPath, recursive: false);
            foreach(var dep in depPaths) {
                if (dep == otherAssetPath) {
                    return true;
                }
            }
            return false;
        }

        static string FindFbxSourceAssetPath()
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

        static void OnPostprocessAllAssets(string [] imported, string [] deleted, string [] moved, string [] movedFrom)
        {
            //
            // TODO: lots of optimization potential! On project load right now we're running
            // an algorithm that takes O(#fbx * (#prefabs + #fbx)).
            //
            // Could easily be #prefabs + #fbx only.
            //
            // Ideally it'd be even faster: we'd know which prefab points to which FBX, no
            // searching needed.
            //
            var fbxSourceScript = FindFbxSourceAssetPath();

            foreach(var fbxModel in imported) {
                if (!fbxModel.EndsWith(".fbx")) {
                    // This is not the asset type we are looking for.
                    // Move along.
                    continue;
                }

                //
                // Find the prefabs in the project that have an FbxSource
                // component that points to the reimported model.
                //
                var fbxSourcePrefabPaths = new HashSet<string>();
                Debug.Log("Looking for prefabs that have a " + fbxSourceScript + " component that points to " + fbxModel);

                // First we find all game objects.
                // Among those we choose those that are prefabs.
                // Among those we check the immediate references. If they refer
                // directly to the FbxSource script *and* to the FBX file, choose
                // it for expensive processing.
                var allObjectGuids = AssetDatabase.FindAssets("t:GameObject");
                foreach(var guid in allObjectGuids) {
                    var pathname = AssetDatabase.GUIDToAssetPath(guid);
                    if (!pathname.EndsWith(".prefab")) {
                        continue;
                    }
                    Debug.Log("found prefab at " + pathname + " with deps " + string.Join("\n\t", AssetDatabase.GetDependencies(pathname, recursive: false)));

                    if (!AssetDependsOn(pathname, fbxSourceScript)) {
                        continue;
                    }
                    Debug.Log("Has an FbxSource component: " + pathname);
                    if (!AssetDependsOn(pathname, fbxModel)) {
                        continue;
                    }
                    Debug.Log("Probably linked to " + fbxModel);
                    fbxSourcePrefabPaths.Add(pathname);
                }
                if (fbxSourcePrefabPaths.Count == 0) {
                    continue;
                }

                //
                // Load each prefab we found, see if its FbxSource points to the
                // FBX file we just imported (we could have both an FbxSource
                // component and a dependency on the FBX without having the
                // FbxSource point to the FBX).
                //
                // There's no reason why a prefab can't have multiple
                // FbxSource, nor need it be at the root of the prefab!
                //
                foreach(var prefabPath in fbxSourcePrefabPaths) {
                    var genericObj = AssetDatabase.LoadMainAssetAtPath(prefabPath);
                    if (!genericObj) {
                        Debug.Log("failed to load " + prefabPath);
                        continue;
                    }
                    var gameObj = genericObj as GameObject;
                    if (!gameObj) {
                        Debug.Log("loaded but not a game object prefab: " + prefabPath);
                        continue;
                    }
                    foreach(var fbxSourceComponent in gameObj.GetComponentsInChildren<FbxSource>()) {
                        Debug.Log("looking at " + fbxSourceComponent.gameObject.name);
                        if (!fbxSourceComponent.MatchesFbxFile(fbxModel)) {
                            continue;
                        }
                        Debug.Log("updating starting at " + fbxSourceComponent.gameObject.name + " in prefab " + prefabPath + " from fbx model " + fbxModel);
                        fbxSourceComponent.SyncPrefab();
                    }
                }
            }
        }
    }
}
