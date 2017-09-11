// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Unity.FbxSdk;

namespace FbxExporters
{
    namespace Editor
    {
        public static class ConvertToModel
        {
            const string MenuItemName1 = "GameObject/Convert To Prefab";

            /// <summary>
            /// OnContextItem is called either:
            /// * when the user selects the menu item via the top menu (with a null MenuCommand), or
            /// * when the user selects the menu item via the context menu (in which case there's a context)
            ///
            /// OnContextItem gets called once per selected object (if the
            /// parent and child are selected, then OnContextItem will only be
            /// called on the parent)
            /// </summary>
            [MenuItem (MenuItemName1, false, 30)]
            static void OnContextItem (MenuCommand command)
            {
                GameObject [] selection = null;

                if (command == null || command.context == null) {
                    // We were actually invoked from the top GameObject menu, so use the selection.
                    selection = Selection.GetFiltered<GameObject> (SelectionMode.Editable | SelectionMode.TopLevel);
                } else {
                    // We were invoked from the right-click menu, so use the context of the context menu.
                    var selected = command.context as GameObject;
                    if (selected) {
                        selection = new GameObject[] { selected };
                    }
                }

                if (selection == null || selection.Length == 0) {
                    ModelExporter.DisplayNoSelectionDialog ();
                    return;
                }

                Selection.objects = CreateInstantiatedModelPrefab (selection);
            }

            /// <summary>
            // Validate the menu item defined by the function above.
            /// </summary>
            [MenuItem (MenuItemName1, true, 30)]
            public static bool OnValidateMenuItem ()
            {
                return true;
            }

            /// <summary>
            /// Create instantiated model prefabs from a selection of objects.
            ///
            /// Every hierarchy in the selection will be exported, under the name of the root.
            ///
            /// If an object and one of its descendents are both selected, the descendent is not promoted to be a prefab -- we only export the root.
            /// </summary>
            /// <returns>list of instanced Model Prefabs</returns>
            /// <param name="unityGameObjectsToConvert">Unity game objects to convert to Model Prefab instances</param>
            /// <param name="path">Path to save Model Prefab; use FbxExportSettings if null</param>
            /// <param name="keepOriginal">If set to <c>true</c> keep original gameobject hierarchy.</param>
            public static GameObject[] CreateInstantiatedModelPrefab (GameObject [] unityGameObjectsToConvert, string directoryFullPath = null, bool keepOriginal = true)
            {
                if (directoryFullPath == null) {
                    directoryFullPath = FbxExporters.EditorTools.ExportSettings.GetAbsoluteSavePath();
                } else {
                    directoryFullPath = Path.GetFullPath(directoryFullPath);
                }

                var toExport = ModelExporter.RemoveRedundantObjects (unityGameObjectsToConvert);
                var wasExported = new List<GameObject>();
                foreach(var go in toExport) {
                    try {
                        wasExported.Add(Convert(go,
                            directoryFullPath: directoryFullPath,
                            keepOriginal: keepOriginal));
                    } catch(System.Exception xcp) {
                        Debug.LogException(xcp);
                    }
                }
                return wasExported.ToArray();
            }

            /// <summary>
            /// Convert one object (and the hierarchy below it) to an auto-updating prefab.
            ///
            /// This returns a new object; the converted object may be modified or destroyed.
            ///
            /// This refreshes the asset database.
            ///
            /// If "keepOriginal" is set, the converted object is modified but remains in the scene.
            /// </summary>
            /// <returns>The instance that replaces 'toConvert' in the scene.</returns>
            /// <param name="toConvert">GameObject hierarchy to replace with a prefab.</param>
            /// <param name="fbxFullPath">Absolute platform-specific path to
            /// the fbx file. May be null, in which case we construct a unique
            /// filename from the object name and the
            /// directoryFullPath.</param>
            /// <param name="directoryFullPath">Absolute platform-specific path
            /// to a directory in which to put the fbx file. Ignored if
            /// fbxFullPath is specified. May be null, in which case we use the
            /// export settings.</param>
            /// <param name="keepOriginal">If set to <c>true</c>, keep the original in the scene.</param>
            public static GameObject Convert (
                GameObject toConvert,
                string directoryFullPath = null,
                string fbxFullPath = null,
                bool keepOriginal = true)
            {
                if (string.IsNullOrEmpty(fbxFullPath)) {
                    // Generate a unique filename.
                    if (string.IsNullOrEmpty (directoryFullPath)) {
                        directoryFullPath = FbxExporters.EditorTools.ExportSettings.GetAbsoluteSavePath ();
                    } else {
                        directoryFullPath = Path.GetFullPath (directoryFullPath);
                    }
                    var fbxBasename = ModelExporter.ConvertToValidFilename (toConvert.name + ".fbx");

                    fbxFullPath = Path.Combine (directoryFullPath, fbxBasename);
                    if (File.Exists (fbxFullPath)) {
                        fbxFullPath = IncrementFileName (directoryFullPath, fbxFullPath);
                    }
                }
                var assetRelativePath = FbxExporters.EditorTools.ExportSettings.ConvertToAssetRelativePath(fbxFullPath);
                var projectRelativePath = "Assets/" + assetRelativePath;
                if (string.IsNullOrEmpty(assetRelativePath)) {
                    throw new System.Exception("Path " + fbxFullPath + " must be in the Assets folder.");
                }

                // Make sure that the object names in the hierarchy are unique.
                // The import back in to Unity would do this automatically but
                // we prefer to control it so that the Maya artist can see the
                // same names as exist in Unity.
                EnforceUniqueNames (new GameObject[] {toConvert});

                // Export to FBX. It refreshes the database.
                {
                    var fbxActualPath = ModelExporter.ExportObject (fbxFullPath, toConvert);
                    if (fbxActualPath != fbxFullPath) {
                        throw new System.Exception ("Failed to convert " + toConvert.name);
                    }
                }

                // Replace w Model asset. LoadMainAssetAtPath wants a path
                // relative to the project, not relative to the assets folder.
                var unityMainAsset = AssetDatabase.LoadMainAssetAtPath (projectRelativePath) as GameObject;
                if (!unityMainAsset) {
                    throw new System.Exception ("Failed to convert " + toConvert.name);;
                }

                // Instantiate the FBX file.
                var unityGO = PrefabUtility.InstantiatePrefab (unityMainAsset, toConvert.scene)
                    as GameObject;
                if (!unityGO) {
                    throw new System.Exception ("Failed to convert " + toConvert.name);;
                }

                // Copy the components over to the instance of the FBX.
                SetupImportedGameObject (toConvert, unityGO);

                // Set up the FbxPrefab component so it will auto-update.
                var fbxPrefab = unityGO.AddComponent<FbxPrefab>();
                fbxPrefab.SetSourceModel(unityMainAsset);

                // Disconnect from the FBX file.
                PrefabUtility.DisconnectPrefabInstance(unityGO);

                // Create a prefab from the instantiated and componentized unityGO.
                var prefabFileName = Path.ChangeExtension(projectRelativePath, ".prefab");
                var prefab = PrefabUtility.CreatePrefab(prefabFileName, unityGO);
                if (!prefab) {
                    throw new System.Exception(
                        string.Format("Failed to create prefab asset in [{0}] from fbx [{1}]",
                            prefabFileName, fbxFullPath));
                }

                // Connect to the prefab file.
                unityGO = PrefabUtility.ConnectGameObjectToPrefab(unityGO, prefab);

                // Remove (now redundant) gameobject
                if (!keepOriginal) {
                    Object.DestroyImmediate (toConvert);
                } else {
                    // rename and put under scene root in case we need to check values
                    toConvert.name = "_safe_to_delete_" + toConvert.name;
                    toConvert.SetActive (false);
                }

                return unityGO;
            }

            /// <summary>
            /// Check if the file exists, and if it does, then increment the name.
            /// e.g. if filename is Sphere.fbx and it already exists, change it to Sphere 1.fbx.
            /// </summary>
            /// <returns>new file name.</returns>
            /// <param name="filename">Filename.</param>
            public static string IncrementFileName(string path, string filename)
            {
                string fileWithoutExt = Path.GetFileNameWithoutExtension (filename);
                string ext = Path.GetExtension (filename);
                string format = "{0} {1}{2}";

                int index = 1;

                // try extracting the current index from the name and incrementing it
                var result = System.Text.RegularExpressions.Regex.Match(fileWithoutExt, @"\d+$");
                if (result != null) {
                    var number = result.Value;
                    int tempIndex;
                    if (int.TryParse (number, out tempIndex)) {
                        fileWithoutExt = fileWithoutExt.Remove (fileWithoutExt.LastIndexOf (number));
                        format = "{0}{1}{2}"; // remove space from format
                        index = tempIndex+1;
                    }
                }

                string file = null;
                do {
                    file = string.Format (format, fileWithoutExt, index, ext);
                    file = Path.Combine(path, file);
                    index++;
                } while (File.Exists (file));

                return file;
            }

            /// <summary>
            /// Enforces that all object names be unique before exporting.
            /// If an object with a duplicate name is found, then it is incremented.
            /// e.g. Sphere becomes Sphere 1
            /// </summary>
            /// <param name="exportSet">Export set.</param>
            public static void EnforceUniqueNames(IEnumerable<GameObject> exportSet)
            {
                Dictionary<string, int> NameToIndexMap = new Dictionary<string, int> ();
                string format = "{0} {1}";

                Queue<GameObject> queue = new Queue<GameObject> (exportSet);

                while(queue.Count > 0){
                    var go = queue.Dequeue ();
                    var name = go.name;
                    if (NameToIndexMap.ContainsKey (name)) {
                        go.name = string.Format (format, name, NameToIndexMap [name]);
                        NameToIndexMap [name]++;
                    } else {
                        NameToIndexMap [name] = 1;
                    }

                    foreach (Transform child in go.transform) {
                        queue.Enqueue (child.gameObject);
                    }
                }
            }

            /// <summary>
            /// Sets up the imported GameObject to match the original.
            /// - Updates the name to be the same as original (i.e. remove the "(Clone)")
            /// - Moves the imported object to the correct position in the hierarchy
            /// - Updates the transform of the imported GameObject to match the original
            /// - Copy over missing components and component values
            /// </summary>
            /// <param name="orig">Original GameObject.</param>
            /// <param name="imported">Imported GameObject.</param>
            public static void SetupImportedGameObject(GameObject orig, GameObject imported)
            {
                Transform importedTransform = imported.transform;
                Transform origTransform = orig.transform;

                // configure transform and maintain local pose
                importedTransform.SetParent (origTransform.parent, false);
                importedTransform.SetSiblingIndex (origTransform.GetSiblingIndex());

                // copy the components over, assuming that the hierarchy order is unchanged
                if (origTransform.hierarchyCount != importedTransform.hierarchyCount) {
                    Debug.LogWarning (string.Format ("Warning: Exported {0} objects, but only imported {1}",
                        origTransform.hierarchyCount, importedTransform.hierarchyCount));
                }
                FixSiblingOrder (orig.transform, imported.transform);

                // the imported GameObject will have the same name as the file to which it was imported from,
                // which might not be the same name as the original GameObject
                CopyComponentsRecursive (orig, imported, namesExpectedMatch:false);
            }

            /// <summary>
            /// Given two hierarchies of nodes whose names match up,
            /// make the 'imported' hierarchy have its children be in the same
            /// order as the 'orig' hierarchy.
            ///
            /// The 'orig' hierarchy is not modified.
            /// </summary>
            public static void FixSiblingOrder(Transform orig, Transform imported){
                foreach (Transform origChild in orig) {
                    Transform importedChild = imported.Find (origChild.name);
                    if (importedChild == null) {
                        Debug.LogWarning (string.Format(
                            "Warning: Could not find {0} in parented under {1} in import hierarchy",
                            origChild.name, imported.name
                        ));
                        continue;
                    }
                    importedChild.SetSiblingIndex (origChild.GetSiblingIndex ());
                    FixSiblingOrder (origChild, importedChild);
                }
            }

            private static void CopyComponentsRecursive(GameObject from, GameObject to, bool namesExpectedMatch = true){
                if (namesExpectedMatch && !to.name.StartsWith(from.name) || from.transform.childCount != to.transform.childCount) {
                    Debug.LogError (string.Format("Error: hierarchies don't match (From: {0}, To: {1})", from.name, to.name));
                    return;
                }

                CopyComponents (from, to);
                for (int i = 0; i < from.transform.childCount; i++) {
                    CopyComponentsRecursive(from.transform.GetChild(i).gameObject, to.transform.GetChild(i).gameObject);
                }
            }

            /// <summary>
            /// Copy components on the 'from' object which is the object in the
            /// scene we exported, over to the 'to' object which is the object
            /// in the scene that we imported from the FBX.
            ///
            /// Exception: don't copy the references to assets in the scene that
            /// were also exported, in particular the meshes and materials.
            ///
            /// The 'from' hierarchy is not modified.
            /// </summary>
            public static void CopyComponents(GameObject from, GameObject to){
                var originalComponents = new List<Component>(to.GetComponents<Component> ());
                foreach(var component in from.GetComponents<Component> ()) {
                    var json = EditorJsonUtility.ToJson(component);

                    System.Type expectedType = component.GetType();
                    Component toComponent = null;

                    // Find the component to copy to.
                    for (int i = 0, n = originalComponents.Count; i < n; i++) {
                        if (originalComponents[i].GetType() == expectedType) {
                            // We have found the component we are looking for,
                            // remove it so we don't try to copy to it again
                            toComponent = originalComponents[i];
                            originalComponents.RemoveAt (i);
                            break;
                        }
                    }

                    if (!toComponent) {
                        // It doesn't exist => create and copy.
                        toComponent = to.AddComponent(component.GetType());
                        EditorJsonUtility.FromJsonOverwrite(json, toComponent);
                    } else {
                        // It exists => copy.
                        // But we want to override that behaviour in a few
                        // cases, to avoid clobbering references to the new FBX
                        // TODO: interpret the object or the json more directly
                        // TODO: be more generic
                        // TODO: handle references to other objects in the same hierarchy

                        if (toComponent is MeshFilter) {
                            // Don't copy the mesh. But there's nothing else to
                            // copy, so just don't copy anything.
                        } else if (toComponent is SkinnedMeshRenderer) {
                            // Don't want to clobber materials or the mesh.
                            var skinnedMesh = toComponent as SkinnedMeshRenderer;
                            var sharedMesh = skinnedMesh.sharedMesh;
                            var sharedMats = skinnedMesh.sharedMaterials;
                            EditorJsonUtility.FromJsonOverwrite(json, toComponent);
                            skinnedMesh.sharedMesh = sharedMesh;
                            skinnedMesh.sharedMaterials = sharedMats;
                        } else if (toComponent is Renderer) {
                            // Don't want to clobber materials.
                            var renderer = toComponent as Renderer;
                            var sharedMats = renderer.sharedMaterials;
                            EditorJsonUtility.FromJsonOverwrite(json, toComponent);
                            renderer.sharedMaterials = sharedMats;
                        } else {
                            // Normal case: copy everything.
                            EditorJsonUtility.FromJsonOverwrite(json, toComponent);
                        }
                    }
                }
            }
        }
    }
}
