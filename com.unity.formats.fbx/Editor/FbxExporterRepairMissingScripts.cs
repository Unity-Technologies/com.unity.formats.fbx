using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace UnityEditor.Formats.Fbx.Exporter
{
    internal class RepairMissingScripts
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
                var fbxPrefabObj = AssetDatabase.LoadMainAssetAtPath(FindFbxPrefabAssetPath());
                string searchID = null;
                string guid;
                long fileId;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fbxPrefabObj, out guid, out fileId))
                {
                    searchID = string.Format(IdFormat, fileId, guid);
                }
                return searchID;
            }
        }

#if COM_UNITY_FORMATS_FBX_AS_ASSET
        public const string FbxPrefabFile = "/UnityFbxPrefab.dll";
#else
        public const string FbxPrefabFile = "Packages/com.unity.formats.fbx/Runtime/FbxPrefab.cs";
#endif
        public static string FindFbxPrefabAssetPath()
        {
#if COM_UNITY_FORMATS_FBX_AS_ASSET
            // Find guids that are scripts that look like FbxPrefab.
            // That catches FbxPrefabTest too, so we have to make sure.
            var allGuids = AssetDatabase.FindAssets("FbxPrefab t:MonoScript");
            string foundPath = "";
            foreach (var guid in allGuids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(FbxPrefabFile)) {
                    if (!string.IsNullOrEmpty(foundPath)) {
                        // How did this happen? Anyway, just don't try.
                        Debug.LogWarning(string.Format("{0} found in multiple places; did you forget to delete one of these?\n{1}\n{2}",
                                FbxPrefabFile.Substring(1), foundPath, path));
                        return "";
                    }
                    foundPath = path;
                }
            }
            if (string.IsNullOrEmpty(foundPath)) {
                Debug.LogWarning(string.Format("{0} not found; are you trying to uninstall {1}?", FbxPrefabFile.Substring(1), ModelExporter.PACKAGE_UI_NAME));
            }
            return foundPath;
#else
            // In Unity 2018.1 and 2018.2.0b7, FindAssets can't find FbxPrefab.cs in a package.
            // So we hardcode the path.
            var path = FbxPrefabFile;
            if (System.IO.File.Exists(System.IO.Path.GetFullPath(path)))
            {
                return path;
            }
            else
            {
                Debug.LogWarningFormat("{0} not found; update FbxPrefabFile variable in FbxPrefabAutoUpdater.cs to point to FbxPrefab.cs path.", FbxPrefabFile);
                return "";
            }
#endif
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
