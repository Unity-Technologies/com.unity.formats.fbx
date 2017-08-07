
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
            public static void OnMenu()
            {
                LastSavedModel();
            }

            private static System.IO.FileInfo GetLastSavedFile(string directoryPath, string ext = ".fbx")
            {
                System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo (directoryPath);
                if (directoryInfo == null || !directoryInfo.Exists)
                    return null;

                System.IO.FileInfo [] files = directoryInfo.GetFiles ();
                System.DateTime recentWrite = System.DateTime.MinValue;
                System.IO.FileInfo recentFile = null;

                foreach (System.IO.FileInfo file in files)
                {
                    if (string.Compare(file.Extension, ext, System.StringComparison.OrdinalIgnoreCase)!=0) 
                        continue;

                    if (file.LastWriteTime > recentWrite)
                    {
                        recentWrite = file.LastWriteTime;
                        recentFile = file;
                    }
                }
                return recentFile;                
            }

            private static string GetSceneFilePath()
            {
                return System.IO.Path.Combine (ScenesPath, SceneName + ".unity");
            }

            private static string GetLastSavedFilePath()
            {
                string modelPath = FbxExporters.EditorTools.ExportSettings.GetAbsoluteSavePath();
                System.IO.FileInfo fileInfo = GetLastSavedFile (modelPath);

                return (fileInfo!=null) ? fileInfo.FullName : null;
            }

            private static void UnloadModel(Object model)
            {
                if (model) {
                    GameObject unityGo = model as GameObject;

                    if (unityGo != null)
                        unityGo.SetActive (false);

                    Object.DestroyImmediate (model);
                }
            }

            private static Object LoadModel(string fbxFileName)
            {
                Object model = null;

                // make relative to UnityProject folder.
                string relFileName = System.IO.Path.Combine("Assets", FbxExporters.EditorTools.ExportSettings.ConvertToAssetRelativePath(fbxFileName));

                Object unityMainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath (relFileName);

                if (unityMainAsset) {
                    model = UnityEditor.PrefabUtility.InstantiatePrefab (unityMainAsset);
                }

                return model;
            }

            private static void LoadLastSavedModel()
            {
                string fbxFileName = GetLastSavedFilePath();

                if (fbxFileName == null) return;
                    
                if (fbxFileName!=LastFilePath || LastModel==null)
                {
                    Object model = LoadModel(fbxFileName);

                    if (model!=null)
                    {
                        if (LastModel!=null)
                        {
                            UnloadModel(LastModel);
                        }

                        LastModel = model as Object;
                        LastFilePath = fbxFileName;
                    }
                    else
                    {
                        Debug.LogWarning(string.Format("failed to load model : {0}", fbxFileName));
                    }
                }
            }

            public static void LastSavedModel()
            {
                // only update if we're have an Untitled scene or the turntable scene
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                if (scene.name == "") // aka Untitled
                {
                    // automatically changes to active scene
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, GetSceneFilePath());
                }
                else
                {
                    UnityEngine.SceneManagement.Scene turntableScene = 
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GetSceneFilePath (), UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(turntableScene);
                }

                if (AutoUpdateEnabled ()) 
                {
                    LoadLastSavedModel ();

                    SubscribeToEvents();
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
                LastModel = null;
                LastFilePath = null;

                UnityEditor.EditorApplication.hierarchyWindowChanged -= UpdateLastSavedModel;
            }

            private static bool AutoUpdateEnabled()
            {
                return (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneName);
            }

            private static void UpdateLastSavedModel()
            {

                if (AutoUpdateEnabled())
                {
                    LoadLastSavedModel();
                }
                else
                {
                    UnsubscribeFromEvents();
                }
            }
        }
    }
}