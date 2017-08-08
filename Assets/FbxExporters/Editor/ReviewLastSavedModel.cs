
using UnityEngine;

namespace FbxExporters
{
    namespace Review
    {
        class TurnTable
        {
            const string MenuItemName = "FbxExporters/Turntable Review/Autoload Last Saved Prefab";

            const string ScenesPath = "Assets";
            const string SceneName = "FbxExporters_TurnTableReview";

            static string LastFilePath = null;
            static Object LastModel = null;

            [UnityEditor.MenuItem (MenuItemName, false, 10)]
            public static void OnMenu ()
            {
                LastSavedModel ();
            }

            private static System.IO.FileInfo GetLastSavedFile (string directoryPath, string ext = ".fbx")
            {
                System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo (directoryPath);
                if (directoryInfo == null || !directoryInfo.Exists)
                    return null;

                System.IO.FileInfo [] files = directoryInfo.GetFiles ();
                System.DateTime recentWrite = System.DateTime.MinValue;
                System.IO.FileInfo recentFile = null;

                foreach (System.IO.FileInfo file in files) {
                    if (string.Compare (file.Extension, ext, System.StringComparison.OrdinalIgnoreCase) != 0)
                        continue;

                    if (file.LastWriteTime > recentWrite) {
                        recentWrite = file.LastWriteTime;
                        recentFile = file;
                    }
                }
                return recentFile;
            }

            private static string GetSceneFilePath ()
            {
                return System.IO.Path.Combine (ScenesPath, SceneName + ".unity");
            }

            private static string GetLastSavedFilePath ()
            {
                string modelPath = FbxExporters.EditorTools.ExportSettings.GetAbsoluteSavePath ();
                System.IO.FileInfo fileInfo = GetLastSavedFile (modelPath);

                return (fileInfo != null) ? fileInfo.FullName : null;
            }

            private static void UnloadModel (Object model)
            {
                if (model) {
                    GameObject unityGo = model as GameObject;

                    if (unityGo != null)
                        unityGo.SetActive (false);

                    Object.DestroyImmediate (model);
                }
            }

            private static Object LoadModel (string fbxFileName)
            {
                Object model = null;

                // make relative to UnityProject folder.
                string relFileName = System.IO.Path.Combine ("Assets", FbxExporters.EditorTools.ExportSettings.ConvertToAssetRelativePath (fbxFileName));

                Object unityMainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath (relFileName);

                if (unityMainAsset) {
                    model = UnityEditor.PrefabUtility.InstantiatePrefab (unityMainAsset);
                }

                return model;
            }

            private static void LoadLastSavedModel ()
            {
                string fbxFileName = GetLastSavedFilePath ();

                if (fbxFileName == null) return;

                if (fbxFileName != LastFilePath || LastModel == null) {
                    Object model = LoadModel (fbxFileName);

                    if (model != null) {
                        if (LastModel != null) {
                            UnloadModel (LastModel);
                        }

                        LastModel = model as Object;
                        LastFilePath = fbxFileName;
                    } else {
                        Debug.LogWarning (string.Format ("failed to load model : {0}", fbxFileName));
                    }
                }
            }

            public static void LastSavedModel ()
            {
                UnityEngine.SceneManagement.Scene scene = new UnityEngine.SceneManagement.Scene();

                // get all scenes
                System.Collections.Generic.List<UnityEngine.SceneManagement.Scene> scenes
                      = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene> ();

                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
                    UnityEngine.SceneManagement.Scene toAdd = UnityEngine.SceneManagement.SceneManager.GetSceneAt (i);

                    // skip Untitled scene. 
                    // The Untitled scene cannot be unloaded, if modified, and we don't want to force the user to save it.
                    if (toAdd.name == "") continue;

                    if (toAdd.name == SceneName) 
                    {
                        scene = toAdd;
                        continue;
                    }
                    scenes.Add (toAdd);
                }

                // if turntable scene not added to list of scenes
                if (!scene.IsValid ()) 
                {
                    // and if for some reason the turntable scene is missing create an empty scene
                    // NOTE: we cannot use NewScene because it will force me to save the modified Untitled scene
                    if (!System.IO.File.Exists(GetSceneFilePath ())) 
                    {
                        var writer = System.IO.File.CreateText (GetSceneFilePath ());
                        writer.WriteLine ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:");
                        writer.Close ();
                        UnityEditor.AssetDatabase.Refresh ();
                    }

                    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene (GetSceneFilePath (), UnityEditor.SceneManagement.OpenSceneMode.Additive);
                }

                // save unmodified scenes (but not the untitled or turntable scene)
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveModifiedScenesIfUserWantsTo (scenes.ToArray ())) 
                {
                    // close all scene except turntable & untitled
                    // NOTE: you cannot unload scene in editor
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
                        UnityEngine.SceneManagement.Scene toUnload = UnityEngine.SceneManagement.SceneManager.GetSceneAt (i);

                        // skip Untitled scene
                        if (toUnload.name == "")
                            continue;

                        // skip Turntable scene
                        if (scene.Equals (toUnload))
                            continue;
                        
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene (toUnload, false);
                    }
                } 
                else
                {
                    Debug.Log ("Cannot enable turntable review when there are modified scenes");
                    return;    
                }

                // make turntable the active scene
                UnityEngine.SceneManagement.SceneManager.SetActiveScene (scene);

                if (AutoUpdateEnabled ()) {
                    LoadLastSavedModel ();

                    SubscribeToEvents ();
                }
            }

            private static void SubscribeToEvents ()
            {
                // ensure we only subscribe once
                UnityEditor.EditorApplication.hierarchyWindowChanged -= UpdateLastSavedModel;
                UnityEditor.EditorApplication.hierarchyWindowChanged += UpdateLastSavedModel;
            }

            private static void UnsubscribeFromEvents ()
            {
                UnloadModel (LastModel);

                LastModel = null;
                LastFilePath = null;

                UnityEditor.EditorApplication.hierarchyWindowChanged -= UpdateLastSavedModel;
            }

            private static bool AutoUpdateEnabled ()
            {
                return (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name == SceneName);
            }

            private static void UpdateLastSavedModel ()
            {

                if (AutoUpdateEnabled ()) {
                    LoadLastSavedModel ();
                } else {
                    UnsubscribeFromEvents ();
                }
            }
        }
    }
}