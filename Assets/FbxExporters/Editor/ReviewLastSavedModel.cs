using UnityEngine;

namespace FbxExporters
{
    namespace Review
    {
        class TurnTable 
        {
            const string MenuItemName = "FbxExporters/Turntable Review/Latest Model Published";

            const string ScenesPath = "Assets";
            const string SceneName = "FbxExporters_TurnTableReview";

            static string LastFilePath = null;
            static Object LastModel = null;

            [UnityEditor.MenuItem (MenuItemName, false, 10)]
            public static void OnMenu()
            {
                UpdateLastSavedModel();
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

            private static string GetLastSavedFilePath()
            {
                string modelPath = FbxExporters.EditorTools.ExportSettings.instance.convertToModelSavePath;

                return GetLastSavedFile(modelPath).FullName;
            }

            private static void UnloadModel(Object model)
            {
                if (model) {
                    GameObject unityGo = model as GameObject;
                    unityGo.SetActive (false);

                    DestroyImmediate (model);
                }
            }

            private static Object LoadModel(string fbxFileName)
            {
                Object model = null;

                if (fbxFileName.StartsWith (UnityEngine.Application.dataPath, System.StringComparison.CurrentCulture))
                {
                    fbxFileName = fbxFileName.Substring (UnityEngine.Application.dataPath.Length+1);
                    fbxFileName = System.IO.Path.Combine ("Assets", fbxFileName);
                }

                UnityEngine.Object unityMainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath (fbxFileName);

                if (unityMainAsset) {
                    model = UnityEditor.PrefabUtility.InstantiatePrefab (unityMainAsset);
                }

                return model;
            }

            private static void UpdateLastSavedModel()
            {
                string fbxFileName = GetLastSavedFilePath();

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
                        UnityEngine.Debug.LogWarning(string.Format("failed to load model : {0}", fbxFileName));
                    }
                }
            }

            public static void LastSavedModel()
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                if (scene.name == "")
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, System.IO.Path.Combine( ScenesPath, SceneName + ".unity" ));
                }

                if (AutoUpdateEnabled ()) 
                {
                    UnityEditor.EditorApplication.hierarchyWindowChanged += Update;

                    UpdateLastSavedModel ();
                }
            }

            private static bool AutoUpdateEnabled()
            {
                return (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneName);
            }

            public static void Update()
            {
                if (AutoUpdateEnabled())
                {
                    UpdateLastSavedModel();
                }
            }
        }
    }
}