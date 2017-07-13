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
                GameObject [] unityActiveGOs = Selection.GetFiltered<GameObject> (SelectionMode.Editable | SelectionMode.TopLevel);
                OnConvertInPlace (unityActiveGOs);
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
                    Debug.LogError ("Error: No GameObject selected");
                    return;
                }
                GameObject selected = command.context as GameObject;
                if (selected == null) {
                    Debug.LogError (string.Format("Error: {0} is not a GameObject and cannot be converted", command.context.name));
                    return;
                }
                OnConvertInPlace (new GameObject[]{selected});
            }

            private static List<GameObject> OnConvertInPlace (GameObject [] unityActiveGOs)
            {
                List<GameObject> result = new List<GameObject> ();

                var exportSet = ModelExporter.RemoveRedundantObjects (unityActiveGOs);
                GameObject[] gosToExport = new GameObject[exportSet.Count];
                exportSet.CopyTo (gosToExport);

                // find common ancestor root & filePath;
                string[] filePaths = new string[gosToExport.Length];
                string dirPath = Path.Combine (Application.dataPath, "Objects");

                for(int n = 0; n < gosToExport.Length; n++){
                    string filename = ModelExporter.ConvertToValidFilename (gosToExport [n].name + ".fbx");
                    filePaths[n] = Path.Combine (dirPath, filename);
                }

                string[] fbxFileNames = new string[filePaths.Length];

                for (int j = 0; j < gosToExport.Length; j++) {
                    fbxFileNames[j] = FbxExporters.Editor.ModelExporter.ExportObjects (filePaths[j],
                        new UnityEngine.Object[] {gosToExport[j]}) as string;
                }

                List<GameObject> selection = new List<GameObject> ();
                for(int i = 0; i < fbxFileNames.Length; i++)
                {
                    var fbxFileName = fbxFileNames [i];
                    if (fbxFileName == null) {
                        Debug.LogWarning (string.Format ("Warning: Export failed for GameObject {0}", gosToExport [i].name));
                        continue;
                    }

                    // make filepath relative to project folder
                    if (fbxFileName.StartsWith (Application.dataPath, System.StringComparison.CurrentCulture)) 
                    {
                        fbxFileName = "Assets" + fbxFileName.Substring (Application.dataPath.Length);
                    }

                    // refresh the assetdata base so that we can query for the model
                    AssetDatabase.Refresh ();

                    // replace w Model asset
                    Object unityMainAsset = AssetDatabase.LoadMainAssetAtPath (fbxFileName);

                    if (unityMainAsset != null) {
                        Object unityObj = PrefabUtility.InstantiatePrefab (unityMainAsset);
                        GameObject unityGO = unityObj as GameObject;
                        if (unityGO != null) 
                        {
                            SetupImportedGameObject (gosToExport [i], unityGO);

                            result.Add (unityGO);

                            // remove (now redundant) gameobject
#if UNI_19965
                            Object.DestroyImmediate (unityActiveGOs [i]);
#else
                            // rename and put under scene root in case we need to check values
                            gosToExport [i].name = "_safe_to_delete_" + gosToExport[i].name;
                            gosToExport [i].SetActive (false);
#endif
                            // select the instanced Model Prefab
                            selection.Add(unityGO);
                        }
                    }

                }

                Selection.objects = selection.ToArray ();

                return result;
            }

            private static void SetupImportedGameObject(GameObject orig, GameObject imported)
            {
                Transform importedTransform = imported.transform;
                Transform origTransform = orig.transform;

                // Set the name to be the name of the instantiated asset.
                // This will get rid of the "(Clone)" if it's added
                imported.name = orig.name;

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
                CopyComponentsRecursive (orig, imported);
            }

            private static void CopyComponentsRecursive(GameObject from, GameObject to){
                if (!to.name.StartsWith(from.name) || from.transform.childCount != to.transform.childCount) {
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