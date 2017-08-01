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
using FbxSdk;

namespace FbxExporters
{
    namespace Editor
    {
        public class ConvertToModel : System.IDisposable
        {
            const string MenuItemName1 = "Assets/Convert To Model";
            const string MenuItemName2 = "GameObject/Convert To Model";

            /// <summary>
            /// Clean up this class on garbage collection
            /// </summary>
            public void Dispose () { }

            /// <summary>
            /// create menu item in the File menu
            /// </summary>
            [MenuItem (MenuItemName1, false)]
            public static void OnMenuItem ()
            {
                GameObject [] unityGameObjectsToConvert = Selection.GetFiltered<GameObject> (SelectionMode.Editable | SelectionMode.TopLevel);
                Object[] result = CreateInstantiatedModelPrefab (unityGameObjectsToConvert);
                if (result.Length>0)
                    Selection.objects = result;
            }

            /// <summary>
            // Validate the menu item defined by the function above.
            /// </summary>
            [MenuItem (MenuItemName1, true)]
            public static bool OnValidateMenuItem ()
            {
                return true;
            }

            // Add a menu item called "Export Model..." to a GameObject's context menu.
            // OnContextItem gets called once per selected object 
            // (if the parent and child are selected, then OnContextItem will only be called on the parent)
            [MenuItem (MenuItemName2, false, 30)]
            static void OnContextItem (MenuCommand command)
            {
                if (command == null || command.context == null) {
                    // We were actually invoked from the top GameObject menu,
                    // not the context menu, so treat it as such.
                    OnMenuItem();
                    return;
                }

                GameObject selected = command.context as GameObject;
                if (selected == null) {
                    Debug.LogError (string.Format("Error: {0} is not a GameObject and cannot be converted", command.context.name));
                    return;
                }
                GameObject[] result = CreateInstantiatedModelPrefab (new GameObject[]{selected});
                if (result.Length>0)
                    Selection.objects = result;

            }

            /// <summary>
            /// Create an instantiated model prefab from an game object hierarchy.
            /// </summary>
            /// <returns>list of instanced Model Prefabs</returns>
            /// <param name="unityGameObjectsToConvert">Unity game objects to convert to Model Prefab instances</param>
            /// <param name="path">Path to save Model Prefab; use FbxExportSettings if null</param>
            /// <param name="keepOriginal">If set to <c>true</c> keep original gameobject hierarchy.</param>
            public static GameObject[] CreateInstantiatedModelPrefab (GameObject [] unityGameObjectsToConvert, string path = null, bool keepOriginal = true)
            {
                if (path == null) {
                    path = FbxExporters.EditorTools.ExportSettings.GetAbsoluteSavePath();
                } else {
                    path = Path.GetFullPath(path);
                }

                List<GameObject> result = new List<GameObject> ();

                var exportSet = ModelExporter.RemoveRedundantObjects (unityGameObjectsToConvert);
                GameObject[] gosToExport = new GameObject[exportSet.Count];
                exportSet.CopyTo (gosToExport);

                EnforceUniqueNames (gosToExport);

                // find common ancestor root & filePath;
                string[] filePaths = new string[gosToExport.Length];

                for(int n = 0; n < gosToExport.Length; n++){
                    var filename = ModelExporter.ConvertToValidFilename (gosToExport [n].name + ".fbx");
                    var filePath = Path.Combine (path, filename);
                    if (File.Exists (filePath)) {
                        filePath = IncrementFileName (path, filename);
                    }
                    filePaths[n] = filePath;
                }

                string[] fbxFileNames = new string[filePaths.Length];

                for (int j = 0; j < gosToExport.Length; j++) {
                    fbxFileNames[j] = FbxExporters.Editor.ModelExporter.ExportObjects (filePaths[j],
                        new UnityEngine.Object[] {gosToExport[j]}) as string;
                }

                for(int i = 0; i < fbxFileNames.Length; i++)
                {
                    var fbxFileName = fbxFileNames [i];
                    if (fbxFileName == null) {
                        Debug.LogWarning (string.Format ("Warning: Export failed for GameObject {0}", gosToExport [i].name));
                        continue;
                    }

                    // make filepath relative to assets folder
                    var relativePath = FbxExporters.EditorTools.ExportSettings.ConvertToAssetRelativePath(fbxFileName);

                    // refresh the assetdata base so that we can query for the model
                    AssetDatabase.Refresh ();

                    // Replace w Model asset. LoadMainAssetAtPath wants a path
                    // relative to the project, not relative to the assets
                    // folder.
                    Object unityMainAsset = AssetDatabase.LoadMainAssetAtPath("Assets/" + relativePath);

                    if (unityMainAsset != null) {
                        Object unityObj = PrefabUtility.InstantiatePrefab (unityMainAsset);
                        GameObject unityGO = unityObj as GameObject;
                        if (unityGO != null) 
                        {
                            SetupImportedGameObject (gosToExport [i], unityGO);


                            // remove (now redundant) gameobject
                            if (!keepOriginal) {
                                Object.DestroyImmediate (unityGameObjectsToConvert [i]);
                            } 
                            else 
                            {
                                // rename and put under scene root in case we need to check values
                                gosToExport [i].name = "_safe_to_delete_" + gosToExport [i].name;
                                gosToExport [i].SetActive (false);
                            }

                            // add the instanced Model Prefab
                            result.Add (unityGO);
                        }
                    }

                }

                return result.ToArray ();
            }

            /// <summary>
            /// Check if the file exists, and if it does, then increment the name.
            /// e.g. if filename is Sphere.fbx and it already exists, change it to Sphere 1.fbx.
            /// </summary>
            /// <returns>new file name.</returns>
            /// <param name="filename">Filename.</param>
            private static string IncrementFileName(string path, string filename)
            {
                string fileWithoutExt = Path.GetFileNameWithoutExtension (filename);
                string ext = Path.GetExtension (filename);

                int index = 1;
                string file = null;
                do {
                    file = string.Format ("{0} {1}{2}", fileWithoutExt, index, ext);
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
            private static void EnforceUniqueNames(GameObject[] exportSet)
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
            private static void SetupImportedGameObject(GameObject orig, GameObject imported)
            {
                Transform importedTransform = imported.transform;
                Transform origTransform = orig.transform;

                // configure transform and maintain local pose
                importedTransform.SetParent (origTransform.parent, false);
                importedTransform.SetSiblingIndex (origTransform.GetSiblingIndex());

                // set the transform to be the same as before
                bool success = UnityEditorInternal.ComponentUtility.CopyComponent (origTransform);
                if (success) {
                    success = UnityEditorInternal.ComponentUtility.PasteComponentValues(importedTransform);
                }
                if (!success) {
                    Debug.LogWarning (string.Format ("Warning: Failed to copy component Transform from {0} to {1}",
                        imported.name, origTransform.name));
                }

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

            private static void FixSiblingOrder(Transform orig, Transform imported){
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

            private static void CopyComponents(GameObject from, GameObject to){
                var components = from.GetComponents<Component> ();
                for(int i = 0; i < components.Length; i++){
                    if(components[i] == null){
                        continue;
                    }
                        
                    bool success = UnityEditorInternal.ComponentUtility.CopyComponent (components[i]);
                    if (success) {
                        // if to already has this component, then copy the values over
                        var toComponent = to.GetComponent (components [i].GetType ());
                        if (toComponent != null) {
                            // don't want to copy MeshFilter because then we will replace the
                            // exported mesh with the old mesh
                            if (!(toComponent is MeshFilter)) {
                                if (toComponent is SkinnedMeshRenderer) {
                                    var skinnedMesh = toComponent as SkinnedMeshRenderer;
                                    var sharedMesh = skinnedMesh.sharedMesh;
                                    success = UnityEditorInternal.ComponentUtility.PasteComponentValues (toComponent);
                                    skinnedMesh.sharedMesh = sharedMesh;
                                } else {
                                    success = UnityEditorInternal.ComponentUtility.PasteComponentValues (toComponent);
                                }
                            }
                        } else {
                            success = UnityEditorInternal.ComponentUtility.PasteComponentAsNew (to);
                        }
                    }
                    if (!success) {
                        Debug.LogWarning (string.Format ("Warning: Failed to copy component {0} from {1} to {2}",
                            components[i].GetType().Name, from.name, to.name));
                    }
                }
            }
        }
    }
}
