using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityEditor.Formats.Fbx.Exporter
{
    public class RepairMissingScripts
    {
        private const string m_forumPackageGUID = "2d81c55c4d9d85146b1d2de96e084b63";
        private const string m_assetStorePackageGUID = "628ffbda3fdf4df4588770785d91a698";

        private const string m_fbxPrefabDLLFileId = "69888640";

        private const string m_idFormat = "{{fileID: {0}, guid: {1}, type:";

        private static List<string> m_searchIDsToReplace;
        private static List<string> SearchIDsToReplace
        {
            get
            {
                if (m_searchIDsToReplace == null || m_searchIDsToReplace.Count <= 0)
                {
                    m_searchIDsToReplace = new List<string>() {
                        string.Format(m_idFormat, m_fbxPrefabDLLFileId, m_forumPackageGUID),
                        string.Format(m_idFormat, m_fbxPrefabDLLFileId, m_assetStorePackageGUID)
                    };
                }
                return m_searchIDsToReplace;
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

        public static string GetSourceCodeSearchID()
        {
            var fbxPrefabObj = AssetDatabase.LoadMainAssetAtPath(FbxExporters.FbxPrefabAutoUpdater.FindFbxPrefabAssetPath());
            string searchID = null;
            string guid;
#if UNITY_2018_2_OR_NEWER
            long fileId;
#else
            int fileId;
#endif
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fbxPrefabObj, out guid, out fileId))
            {
                searchID = string.Format(m_idFormat, fileId, guid);
            }
            return searchID;
        }

        public int GetAssetsToRepairCount(){
            return AssetsToRepair.Length;
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
                            sr.Close();
                            return false;
                        }
                    }

                    var contents = sr.ReadToEnd();
                    if (SearchIDsToReplace.Exists(searchId => contents.Contains(searchId)))
                    {
                        sr.Close();
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
            string sourceCodeSearchID = GetSourceCodeSearchID();
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
                            sr.Close ();
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
