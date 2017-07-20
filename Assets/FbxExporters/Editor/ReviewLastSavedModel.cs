namespace FbxExporters
{
    namespace Review
    {
        class TurnTable
        {
            const string MenuItemName = "FbxExporters/Turntable Review/Latest Model Update";

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

            public static void LastSavedModel()
            {
                string modelPath = FbxExporters.EditorTools.ExportSettings.instance.convertToModelSavePath;

                string LastSavedModel = GetLastSavedFile(modelPath).FullName;

                string fbxFileName = LastSavedModel;

                if (fbxFileName.StartsWith (UnityEngine.Application.dataPath, System.StringComparison.CurrentCulture)) 
                {
                    fbxFileName = fbxFileName.Substring (UnityEngine.Application.dataPath.Length+1);
                    fbxFileName = System.IO.Path.Combine ("Assets", fbxFileName);
                }

                UnityEngine.Object unityMainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath (fbxFileName);

                if (unityMainAsset != null) {
                    UnityEditor.PrefabUtility.InstantiatePrefab (unityMainAsset);
                }
            }
        }
    }
}