using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UnityEditor.Formats.Fbx.Exporter
{
    public class RepairMissingScripts
    {
        private const string ForumPackageGUID = "2d81c55c4d9d85146b1d2de96e084b63";
        private const string AssetStorePackageGUID = "628ffbda3fdf4df4588770785d91a698";

        private const string FbxPrefabDLLFileId = "69888640";

        private const string IdFormat = "{{fileID: {0}, guid: {1}, type:";

        private static List<string> s_searchIDsToReplace;
        private static List<string> SearchIDsToReplace
        {
            get
            {
                if (s_searchIDsToReplace == null || s_searchIDsToReplace.Count <= 0)
                {
                    s_searchIDsToReplace = new List<string>() {
                        string.Format(IdFormat, FbxPrefabDLLFileId, ForumPackageGUID),
                        string.Format(IdFormat, FbxPrefabDLLFileId, AssetStorePackageGUID)
                    };
                }
                return s_searchIDsToReplace;
            }
        }

        private string[] m_assetsToRepair;
        private string[] AssetsToRepair{
            get{
                if (m_assetsToRepair == null) {
                    m_assetsToRepair = FindAssetsToRepair ();
                }
                return m_assetsToRepair;
            }
        }

        public static string SourceCodeSearchID
        {
            get
            {
                var fbxPrefabObj = AssetDatabase.LoadMainAssetAtPath(FbxPrefabAutoUpdater.FindFbxPrefabAssetPath());
                string searchID = null;
                string guid;
#if UNITY_2018_2_OR_NEWER
                long fileId;
#else
            int fileId;
#endif
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fbxPrefabObj, out guid, out fileId))
                {
                    searchID = string.Format(IdFormat, fileId, guid);
                }
                return searchID;
            }
        }

        public int AssetsToRepairCount
        {
            get
            {
                return AssetsToRepair.Length;
            }
        }

        public string[] GetAssetsToRepair(){
            return AssetsToRepair;
        }

        public static string[] FindAssetsToRepair()
        {
            // search project for assets containing old GUID

            // ignore if forced binary
            if (UnityEditor.EditorSettings.serializationMode == SerializationMode.ForceBinary) {
                return new string[]{};
            }

            // check all scenes and prefabs
            string[] searchFilePatterns = new string[]{ "*.prefab", "*.unity" };

            List<string> assetsToRepair = new List<string> ();
            foreach (string searchPattern in searchFilePatterns) {
                foreach (string file in Directory.GetFiles(Application.dataPath, searchPattern, SearchOption.AllDirectories)) {
                    if (AssetNeedsRepair (file)) {
                        assetsToRepair.Add (file);
                    }
                }
            }
            return assetsToRepair.ToArray ();
        }

        private static bool AssetNeedsRepair(string filePath)
        {
            try{
                using(var sr = new StreamReader (filePath)){
                    if(sr.Peek() > -1){
                        var firstLine = sr.ReadLine();
                        if(!firstLine.StartsWith("%YAML")){
                            return false;
                        }
                    }

                    var contents = sr.ReadToEnd();
                    if (SearchIDsToReplace.Exists(searchId => contents.Contains(searchId)))
                    {
                        return true;
                    }
                }
            }
            catch(IOException e){
                Debug.LogError (string.Format ("Failed to check file for component update: {0} (error={1})", filePath, e));
            }
            return false;
        }

        public bool ReplaceGUIDInTextAssets ()
        {
            string sourceCodeSearchID = SourceCodeSearchID;
            if(string.IsNullOrEmpty(sourceCodeSearchID))
            {
                return false;
            }
            bool replacedGUID = false;
            foreach (string file in AssetsToRepair) {
                replacedGUID |= ReplaceGUIDInFile (file, sourceCodeSearchID);
            }
            if (replacedGUID) {
                AssetDatabase.Refresh ();
            }
            return replacedGUID;
        }

        private static bool ReplaceID(string searchId, string replacementId, ref string line)
        {
            if (line.Contains(searchId))
            {
                line = line.Replace(searchId, replacementId);
                return true;
            }
            return false;
        }

        private static bool ReplaceGUIDInFile (string path, string replacementSearchID)
        {
            // try to read file, assume it's a text file for now
            bool modified = false;

            try {
                var tmpFile = Path.GetTempFileName();
                if(string.IsNullOrEmpty(tmpFile)){
                    return false;
                }

                using(var sr = new StreamReader (path)){
                    // verify that this is a text file
                    var firstLine = "";
                    if (sr.Peek () > -1) {
                        firstLine = sr.ReadLine ();
                        if (!firstLine.StartsWith ("%YAML")) {
                            return false;
                        }
                    }

                    using(var sw = new StreamWriter (tmpFile, false)){
                        if (!string.IsNullOrEmpty (firstLine)) {
                            sw.WriteLine (firstLine);
                        }

                        while (sr.Peek () > -1) {
                            var line = sr.ReadLine ();
                            SearchIDsToReplace.ForEach(searchId =>
                                modified |= ReplaceID(searchId, replacementSearchID, ref line)
                            );

                            sw.WriteLine (line);
                        }
                    }
                }

                if (modified) {
                    File.Delete (path);
                    File.Move (tmpFile, path);

                    Debug.LogFormat("Updated FbxPrefab components in file {0}", path);
                    return true;
                } else {
                    File.Delete (tmpFile);
                }
            } catch (IOException e) {
                Debug.LogError (string.Format ("Failed to replace GUID in file {0} (error={1})", path, e));
            }

            return false;
        }
    }
}
