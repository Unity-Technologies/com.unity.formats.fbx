
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

            public const string TempSavePath = "_safe_to_delete";

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
                string modelPath = FbxExporters.Integrations.GetTempSavePath ();
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
                GameObject modelGO = null;

                // make relative to UnityProject folder.
                string relFileName = System.IO.Path.Combine ("Assets", FbxExporters.EditorTools.ExportSettings.ConvertToAssetRelativePath (fbxFileName));

                Object unityMainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath (relFileName);

                if (unityMainAsset) {
                    modelGO = UnityEditor.PrefabUtility.InstantiatePrefab (unityMainAsset) as GameObject;

                    GameObject turntableGO = GameObject.Find ("TurnTable");
                    if (turntableGO != null) {
                        modelGO.transform.parent = turntableGO.transform;
                        turntableGO.AddComponent<RotateModel> ();
                    } else {
                        modelGO.AddComponent<RotateModel> ();
                    }
                }

                FrameCameraOnModel (modelGO);

                return modelGO as Object;
            }

            private static void FrameCameraOnModel(GameObject modelGO)
            {
                // Set so camera frames model
                // Note: this code assumes the model is at 0,0,0
                Vector3 boundsSize = modelGO.GetComponent<Renderer>().bounds.size;
                float distance = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
                distance /= (2.0f * Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad));
                Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -distance * 2.0f);

                // rotate camera towards model
                Camera.main.transform.LookAt(modelGO.transform.position);
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

                var desiredScene = FbxExporters.EditorTools.ExportSettings.instance.turntableScene;
                string desiredSceneName = null;
                if (desiredScene) {
                    desiredSceneName = desiredScene.name;
                }

                bool foundScene = false;
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
                    UnityEngine.SceneManagement.Scene toAdd = UnityEngine.SceneManagement.SceneManager.GetSceneAt (i);

                    // skip Untitled scene. 
                    // The Untitled scene cannot be unloaded, if modified, and we don't want to force the user to save it.
                    if (toAdd.name == "") continue;

                    if (desiredSceneName != null && toAdd.name == desiredSceneName) {
                        scene = toAdd;
                        foundScene = true;
                        continue;
                    }

                    if (!foundScene && toAdd.name == SceneName) 
                    {
                        scene = toAdd;
                        continue;
                    }
                    scenes.Add (toAdd);
                }

                // if turntable scene not added to list of scenes
                if (!scene.IsValid ()) 
                {
                    string scenePath = null;
                    if (desiredScene) {
                        scenePath = UnityEditor.AssetDatabase.GetAssetPath (desiredScene);
                        Debug.LogWarning (scenePath);
                    } else {
                        
                        // and if for some reason the turntable scene is missing create an empty scene
                        // NOTE: we cannot use NewScene because it will force me to save the modified Untitled scene
                        if (!System.IO.File.Exists (GetSceneFilePath ())) {
                            var writer = System.IO.File.CreateText (GetSceneFilePath ());
                            writer.WriteLine ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:");
                            writer.Close ();
                            UnityEditor.AssetDatabase.Refresh ();
                        }
                        scenePath = GetSceneFilePath ();
                    }

                    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene (scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
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

                // create camera and light if none
                if (Camera.main == null) {
                    GameObject camera = new GameObject ("MainCamera");
                    camera.AddComponent<Camera> ();
                    camera.tag = "MainCamera";
                }
                
                if(!Object.FindObjectOfType<Light>()){
                    GameObject light = new GameObject ("Light");
                    light.transform.localEulerAngles = new Vector3 (50, -30, 0);
                    Light lightComp = light.AddComponent<Light> ();
                    lightComp.type = LightType.Directional;
                    lightComp.intensity = 1;
                    lightComp.shadows = LightShadows.Soft;
                }

                // maximize game window and start playing
                var gameWindow = GetMainGameView();
                if (gameWindow) {
                    gameWindow.maximized = true;
                    UnityEditor.EditorApplication.isPlaying = true;
                } else {
                    Debug.LogWarning ("Failed to access Game Window, please restart Unity to try again.");
                }

                if (AutoUpdateEnabled ()) {
                    LoadLastSavedModel ();

                    SubscribeToEvents ();
                }
            }

            public static UnityEditor.EditorWindow GetMainGameView()
            {
                System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
                System.Type type = assembly.GetType("UnityEditor.GameView");
                UnityEditor.EditorWindow gameview = UnityEditor.EditorWindow.GetWindow(type, false, null, true);
                return gameview;
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