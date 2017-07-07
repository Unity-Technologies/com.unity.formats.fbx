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
                OnConvertInPlace ();
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
            [MenuItem (MenuItemName2, false, 30)]
            static void OnContextItem (MenuCommand command)
            {
                OnConvertInPlace ();
            }

            private static List<GameObject> OnConvertInPlace ()
            {
                List<GameObject> result = new List<GameObject> ();

                GameObject [] unityActiveGOs = Selection.GetFiltered<GameObject> (SelectionMode.Editable | SelectionMode.TopLevel);

                var exportSet = ModelExporter.RemoveRedundantObjects (unityActiveGOs);
                GameObject[] gosToExport = new GameObject[exportSet.Count];
                exportSet.CopyTo (gosToExport);

                // find common ancestor root & filePath;
                string[] filePaths = new string[gosToExport.Length];
                string dirPath = Path.Combine (Application.dataPath, "Objects");

                for(int n = 0; n < gosToExport.Length; n++){
                    filePaths[n] = Path.Combine (dirPath, gosToExport[n].name + ".fbx");
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
                        Debug.Log (string.Format ("Warning: Export failed for GameObject {0}", gosToExport [i].name));
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
                        Object unityObj = PrefabUtility.InstantiateAttachedAsset (unityMainAsset);
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
                    // if to already has this component, then skip it
                    if(components[i] == null || to.GetComponent(components[i].GetType()) != null){
                        continue;
                    }
                    bool success = UnityEditorInternal.ComponentUtility.CopyComponent (components[i]);
                    if (success) {
                        success = UnityEditorInternal.ComponentUtility.PasteComponentAsNew (to);
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