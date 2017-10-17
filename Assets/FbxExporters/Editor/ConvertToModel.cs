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
            const string MenuItemName1 = "GameObject/Convert To Linked Prefab Instance";

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
            /// Gets the export settings.
            /// </summary>
            public static EditorTools.ExportSettings ExportSettings {
                get { return EditorTools.ExportSettings.instance; }
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
            public static GameObject[] CreateInstantiatedModelPrefab (
                GameObject [] unityGameObjectsToConvert,
                string directoryFullPath = null)
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
                            directoryFullPath: directoryFullPath));
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
            public static GameObject Convert (
                GameObject toConvert,
                string directoryFullPath = null,
                string fbxFullPath = null)
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

                // Copy the mesh/materials from the FBX
                UpdateFromFBX (toConvert, unityMainAsset);

                // Set up the FbxPrefab component so it will auto-update.
                // Make sure to delete whatever FbxPrefab history we had.
                var fbxPrefab = toConvert.GetComponent<FbxPrefab>();
                if (fbxPrefab) {
                    Object.DestroyImmediate(fbxPrefab);
                }
                fbxPrefab = toConvert.AddComponent<FbxPrefab>();
                fbxPrefab.SetSourceModel(unityMainAsset);

                // Create a prefab from the instantiated and componentized unityGO.
                var prefabFileName = Path.ChangeExtension(projectRelativePath, ".prefab");
                var prefab = PrefabUtility.CreatePrefab(prefabFileName, toConvert, ReplacePrefabOptions.ConnectToPrefab);
                if (!prefab) {
                    throw new System.Exception(
                        string.Format("Failed to create prefab asset in [{0}] from fbx [{1}]",
                            prefabFileName, fbxFullPath));
                }

                return toConvert;
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
                // file, space, number, extension.
                string format = "{0} {1}{2}";

                int index = 1;

                // try extracting the current index from the name and incrementing it
                var result = System.Text.RegularExpressions.Regex.Match(fileWithoutExt, @"\d+$");
                if (result != null) {
                    var number = result.Value;

                    // Parse the number.
                    int tempIndex;
                    if (int.TryParse (number, out tempIndex)) {
                        fileWithoutExt = fileWithoutExt.Remove (fileWithoutExt.LastIndexOf (number));
                        // Change the format to remove the extra space we'd add
                        // if there weren't already a number. Also, try to use the
                        // same width (so Cube001 increments to Cube002, not Cube2).
                        format = "{0}{1:D" + number.Length + "}{2}"; // file, number with padding, extension
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
            /// Updates the meshes and materials of the exported GameObjects
            /// to link to those imported from the FBX.
            /// </summary>
            /// <param name="orig">Original.</param>
            /// <param name="fbx">Fbx.</param>
            public static void UpdateFromFBX(GameObject orig, GameObject fbx)
            {
                // recurse over orig, for each transform finding the corresponding transform in the FBX
                // and copying the meshes and materials over from the FBX
                var goDict = GetNameToFbxGameObject(orig, fbx);
                var q = new Queue<Transform> ();
                q.Enqueue (orig.transform);
                while (q.Count > 0) {
                    var t = q.Dequeue ();

                    if (goDict [t.name] == null) {
                        Debug.LogWarning (string.Format ("Warning: Could not find Object {0} in FBX", t.name));
                        continue;
                    } 
                    CopyComponents (t.gameObject, goDict [t.name]);
                    foreach (Transform child in t) {
                        q.Enqueue (child);
                    }
                }
            }

            /// <summary>
            /// Gets a dictionary linking exported GameObject name to fbx game object.
            /// </summary>
            /// <returns>Dictionary containing the name to fbx game object.</returns>
            /// <param name="orig">Original.</param>
            /// <param name="fbx">Fbx.</param>
            private static Dictionary<string,GameObject> GetNameToFbxGameObject(GameObject orig, GameObject fbx){
                var nameToGO = new Dictionary<string,GameObject> ();

                var q = new Queue<Transform> ();
                q.Enqueue (orig.transform);
                while (q.Count > 0) {
                    var t = q.Dequeue ();
                    nameToGO [t.name] = null;
                    foreach (Transform child in t) {
                        q.Enqueue (child);
                    }
                }

                nameToGO [orig.name] = fbx;

                var fbxQ = new Queue<Transform> ();
                foreach (Transform child in fbx.transform) {
                    fbxQ.Enqueue (child);
                }

                while (fbxQ.Count > 0) {
                    var t = fbxQ.Dequeue ();
                    if (!nameToGO.ContainsKey (t.name)) {
                        Debug.LogWarning (string.Format("Warning: {0} in FBX but not in converted hierarchy", t.name));
                        continue;
                    }
                    nameToGO [t.name] = t.gameObject;
                    foreach (Transform child in t) {
                        fbxQ.Enqueue (child);
                    }
                }

                return nameToGO;
            }

            /// <summary>
            /// Copy components on the 'from' object which is the FBX,
            /// over to the 'to' object which is the object in the
            /// scene we exported.
            ///
            /// Only copy over meshes and materials, since that is all the FBX contains
            /// that is not already in the scene.
            ///
            /// The 'from' hierarchy is not modified.
            /// </summary>
            public static void CopyComponents(GameObject to, GameObject from){
                var originalComponents = new List<Component>(to.GetComponents<Component> ());
                // copy over meshes, materials, and nothing else
                foreach (var component in from.GetComponents<Component>()) {

                    var json = EditorJsonUtility.ToJson(component);
                    if (string.IsNullOrEmpty (json)) {
                        // this happens for missing scripts
                        continue;
                    }

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
                        // copy over mesh filter and mesh renderer to replace
                        // skinned mesh renderer
                        if (component is MeshFilter) {
                            var skinnedMesh = to.GetComponent<SkinnedMeshRenderer> ();
                            if (skinnedMesh) {
                                toComponent = to.AddComponent(component.GetType());
                                EditorJsonUtility.FromJsonOverwrite (json, toComponent);

                                var toRenderer = to.AddComponent <MeshRenderer>();
                                var fromRenderer = from.GetComponent<MeshRenderer> ();
                                if (toRenderer && fromRenderer) {
                                    EditorJsonUtility.FromJsonOverwrite (EditorJsonUtility.ToJson(fromRenderer), toRenderer);
                                }

                                Object.DestroyImmediate (skinnedMesh);
                            }
                        }
                        continue;
                    }

                    if (toComponent is SkinnedMeshRenderer) {
                        var skinnedMesh = toComponent as SkinnedMeshRenderer;
                        var fromSkinnedMesh = component as SkinnedMeshRenderer;
                        skinnedMesh.sharedMesh = fromSkinnedMesh.sharedMesh;
                        skinnedMesh.sharedMaterials = fromSkinnedMesh.sharedMaterials;
                    } else if (toComponent is MeshFilter) {
                        EditorJsonUtility.FromJsonOverwrite (json, toComponent);
                    } else if (toComponent is Renderer) {
                        var toRenderer = toComponent as Renderer;
                        var fromRenderer = component as Renderer;
                        toRenderer.sharedMaterials = fromRenderer.sharedMaterials;
                    }
                }
            }
        }
    }
}
