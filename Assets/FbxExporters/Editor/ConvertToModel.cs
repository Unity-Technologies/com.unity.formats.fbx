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

                // find common ancestor root & filePath;
                string filePath = "";
                string dirPath = Path.Combine (Application.dataPath, "Objects");

                GameObject unityCommonAncestor = null;
                int siblingIndex = -1;

                foreach (GameObject goObj in unityActiveGOs) {
                    siblingIndex = goObj.transform.GetSiblingIndex ();
                    unityCommonAncestor = (goObj.transform.parent != null) ? goObj.transform.parent.gameObject : null;
                    filePath = Path.Combine (dirPath, goObj.name + ".fbx");

                    break;
                }

                string fbxFileName = FbxExporters.Editor.ModelExporter.ExportObjects (filePath, unityActiveGOs) as string;

                if (fbxFileName != null) 
                {
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

                        if (unityObj != null) 
                        {
                            GameObject unityGO = unityObj as GameObject;
                            Transform unityGOTransform = unityGO.transform;

                            // configure name
                            const string cloneSuffix = "(Clone)";

                            if (unityGO.name.EndsWith (cloneSuffix, System.StringComparison.CurrentCulture)) {
                                unityGO.name = unityGO.name.Remove (cloneSuffix.Length - 1);
                            }

                            // configure transform and maintain local pose
                            if (unityCommonAncestor != null) {
                                unityGOTransform.SetParent (unityCommonAncestor.transform, false);
                            }

                            unityGOTransform.SetSiblingIndex (siblingIndex);

                            // copy the components over, assuming that the hierarchy order is unchanged
                            if (unityActiveGOs.Length == 1) {
                                CopyComponentsRecursive (unityActiveGOs [0], unityGO);
                            } else {
                                if (unityActiveGOs.Length != unityGOTransform.childCount) {
                                    Debug.LogWarning (string.Format ("Warning: Exported {0} objects, but only imported {1}",
                                        unityActiveGOs.Length, unityGOTransform.childCount));
                                }
                                for (int i = 0, c = unityGOTransform.childCount; i < c; i++) {
                                    CopyComponentsRecursive (unityActiveGOs [i], unityGOTransform.GetChild (i).gameObject);
                                }
                            }

                            result.Add (unityObj as GameObject);

                            // remove (now redundant) gameobjects
                            for (int i = 0; i < unityActiveGOs.Length; i++) {
#if UNI_19965
                                Object.DestroyImmediate (unityActiveGOs [i]);
#else
                                // rename and put under scene root in case we need to check values
                                unityActiveGOs [i].name = "_safe_to_delete_" + unityActiveGOs [i].name;
                                unityActiveGOs [i].transform.parent = null;
                                unityActiveGOs [i].SetActive (false);
#endif
                            }

                            // select the instanced Model Prefab
                            Selection.objects = new GameObject[] {unityGO};
                        }
                    }

                }

                return result;
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