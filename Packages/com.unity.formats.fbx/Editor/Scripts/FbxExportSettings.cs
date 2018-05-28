using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Formats.Fbx.Exporter {

    [CustomEditor(typeof(ExportSettings))]
    public class ExportSettingsEditor : UnityEditor.Editor {
        Vector2 scrollPos = Vector2.zero;
        const float LabelWidth = 144;
        const float SelectableLabelMinWidth = 90;
        const float BrowseButtonWidth = 25;
        const float FieldOffset = 18;
        const float BrowseButtonOffset = 5;

        public override void OnInspectorGUI() {
            ExportSettings exportSettings = (ExportSettings)target;

            // Increasing the label width so that none of the text gets cut off
            EditorGUIUtility.labelWidth = LabelWidth;

            scrollPos = GUILayout.BeginScrollView (scrollPos);

            var version = UnityEditor.Formats.Fbx.Exporter.ModelExporter.GetVersionFromReadme ();
            if (!string.IsNullOrEmpty(version)) {
                GUILayout.Label ("Version: " + version, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space ();
            }
            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            GUILayout.BeginVertical();
            exportSettings.autoUpdaterEnabled = EditorGUILayout.Toggle(
                new GUIContent("Auto-Updater:",
                    "Automatically updates prefabs with new fbx data that was imported."),
                exportSettings.autoUpdaterEnabled
            );

            exportSettings.showConvertToPrefabDialog = EditorGUILayout.Toggle(
                new GUIContent("Show Convert UI:", "Enable Convert dialog when converting to a Linked Prefab"),
                exportSettings.showConvertToPrefabDialog
            );
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Integration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            GUILayout.BeginHorizontal ();
            EditorGUILayout.LabelField(new GUIContent (
                "3D Application:",
                "Select the 3D Application for which you would like to install the Unity integration."), GUILayout.Width(LabelWidth - FieldOffset));
            
            // dropdown to select Maya version to use
            var options = ExportSettings.GetDCCOptions();

            exportSettings.selectedDCCApp = EditorGUILayout.Popup(exportSettings.selectedDCCApp, options);

            if (GUILayout.Button(new GUIContent("...", "Browse to a 3D application in a non-default location"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth))) {
                var ext = "";
                switch (Application.platform) {
                case RuntimePlatform.WindowsEditor:
                    ext = "exe";
                    break;
                case RuntimePlatform.OSXEditor:
                    ext = "app";
                    break;
                default:
                    throw new System.NotImplementedException ();
                }

                string dccPath = EditorUtility.OpenFilePanel ("Select Digital Content Creation Application", ExportSettings.GetFirstValidVendorLocation(), ext);

                // check that the path is valid and references the maya executable
                if (!string.IsNullOrEmpty (dccPath)) {
                    ExportSettings.DCCType foundDCC = ExportSettings.DCCType.Maya;
                    var foundDCCPath = TryFindDCC (dccPath, ext, ExportSettings.DCCType.Maya);
                    if (foundDCCPath == null && Application.platform == RuntimePlatform.WindowsEditor) {
                        foundDCCPath = TryFindDCC (dccPath, ext, ExportSettings.DCCType.Max);
                        foundDCC = ExportSettings.DCCType.Max;
                    }
                    if (foundDCCPath == null) {
                        Debug.LogError (string.Format ("Could not find supported 3D application at: \"{0}\"", Path.GetDirectoryName (dccPath)));
                    } else {
                        dccPath = foundDCCPath;
                        ExportSettings.AddDCCOption (dccPath, foundDCC);
                    }
                    Repaint ();
                }
            }
            GUILayout.EndHorizontal ();

            EditorGUILayout.Space();

            exportSettings.launchAfterInstallation = EditorGUILayout.Toggle(
                new GUIContent("Keep Open:",
                    "Keep the selected 3D application open after Unity integration install has completed."),
                exportSettings.launchAfterInstallation
            );

            exportSettings.HideSendToUnityMenu = EditorGUILayout.Toggle(
                new GUIContent("Hide Native Menu:",
                    "Replace Maya's native 'Send to Unity' menu with the Unity Integration's menu"),
                exportSettings.HideSendToUnityMenu
            );

            EditorGUILayout.Space();

            // disable button if no 3D application is available
            EditorGUI.BeginDisabledGroup (!ExportSettings.CanInstall());
            var installIntegrationContent = new GUIContent(
                    "Install Unity Integration",
                    "Install and configure the Unity integration for the selected 3D application so that you can import and export directly with this project.");
            if (GUILayout.Button (installIntegrationContent)) {
                EditorApplication.delayCall += UnityEditor.Formats.Fbx.Exporter.IntegrationsUI.InstallDCCIntegration;
            }
            EditorGUI.EndDisabledGroup ();

            EditorGUILayout.Space ();

            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField ("FBX Prefab Component Updater", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.Space ();

            var repairMissingScripts = new GUIContent (
                                       "Run Component Updater",
                                        "If FBX exporter version 1.3.0f1 or earlier was previously installed, then links to the FbxPrefab component will need updating.\n" +
                                        "Run this to update all FbxPrefab references in text serialized prefabs and scene files.");

            if (GUILayout.Button (repairMissingScripts)) {
                var componentUpdater = new UnityEditor.Formats.Fbx.Exporter.RepairMissingScripts ();
                var filesToRepairCount = componentUpdater.GetAssetsToRepairCount ();
                var dialogTitle = "FBX Prefab Component Updater";
                if (filesToRepairCount > 0) {
                    bool result = UnityEditor.EditorUtility.DisplayDialog (dialogTitle,
                        string.Format("Found {0} prefab(s) and/or scene(s) with components requiring update.\n\n" +
                        "If you choose 'Go Ahead', the FbxPrefab components in these assets " +
                        "will be automatically updated to work with the latest FBX exporter.\n" +
                            "You should make a backup before proceeding.", filesToRepairCount),
                        "I Made a Backup. Go Ahead!", "No Thanks");
                    if (result) {
                        componentUpdater.ReplaceGUIDInTextAssets ();
                    } else {
                        var assetsToRepair = componentUpdater.GetAssetsToRepair ();
                        Debug.LogFormat ("Failed to update the FbxPrefab components in the following files:\n{0}", string.Join ("\n", assetsToRepair));
                    }
                } else {
                    UnityEditor.EditorUtility.DisplayDialog (dialogTitle,
                        "Couldn't find any prefabs or scenes that require updating", "Ok");
                }
            }

            GUILayout.FlexibleSpace ();
            GUILayout.EndVertical();
            GUILayout.EndScrollView ();

            if (GUI.changed) {
                EditorUtility.SetDirty (exportSettings);
                exportSettings.Save ();
            }
        }

        private static string TryFindDCC(string dccPath, string ext, ExportSettings.DCCType dccType){
            string dccName = "";
            switch (dccType) {
            case ExportSettings.DCCType.Maya:
                dccName = "maya";
                break;
            case ExportSettings.DCCType.Max:
                dccName = "3dsmax";
                break;
            default:
                throw new System.NotImplementedException ();
            }

            if (Path.GetFileNameWithoutExtension (dccPath).ToLower ().Equals (dccName)) {
                return dccPath;
            }

            // clicked on the wrong application, try to see if we can still find
            // a dcc in this directory.
            var dccDir = new DirectoryInfo(Path.GetDirectoryName(dccPath));
            FileSystemInfo[] files = {};
            switch(Application.platform){
            case RuntimePlatform.OSXEditor:
                files = dccDir.GetDirectories ("*." + ext);
                break;
            case RuntimePlatform.WindowsEditor:
                files = dccDir.GetFiles ("*." + ext);
                break;
            default:
                throw new System.NotImplementedException();
            }

            string newDccPath = null;
            foreach (var file in files) {
                var filename = Path.GetFileNameWithoutExtension (file.Name).ToLower ();
                if (filename.Equals (dccName)) {
                    newDccPath = file.FullName.Replace("\\","/");
                    break;
                }
            }
            return newDccPath;
        }

    }

    [FilePath("ProjectSettings/FbxExportSettings.asset",FilePathAttribute.Location.ProjectFolder)]
    public class ExportSettings : ScriptableSingleton<ExportSettings>
    {
        public enum ExportFormat { ASCII = 0, Binary = 1}

        public enum Include { Model = 0, Anim = 1, ModelAndAnim = 2 }

        public enum ObjectPosition { LocalCentered = 0, WorldAbsolute = 1, Reset = 2 /* For convert to model only, no UI option*/}

        public enum LODExportType { All = 0, Highest = 1, Lowest = 2 }

        public const string kDefaultSavePath = ".";
        private static List<string> s_PreferenceList = new List<string>() {kMayaOptionName, kMayaLtOptionName, kMaxOptionName};
        //Any additional names require a space after the name
        public const string kMaxOptionName = "3ds Max ";
        public const string kMayaOptionName = "Maya ";
        public const string kMayaLtOptionName = "Maya LT";

        public bool Verbose = false;

        private static string DefaultIntegrationSavePath {
            get{
                return Path.GetDirectoryName(Application.dataPath);
            }
        }

        private static string GetMayaLocationFromEnvironmentVariable(string env)
        {
            string result = null;

            if (string.IsNullOrEmpty(env))
                return null;

            string location = Environment.GetEnvironmentVariable(env);

            if (string.IsNullOrEmpty(location))
                return null;

            //Remove any extra slashes on the end
            //Maya would accept a single slash in either direction, so we should be able to
            location = location.Replace("\\", "/");
            location = location.TrimEnd('/');

            if (!Directory.Exists(location))
                return null;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                //If we are on Windows, we need only go up one location to get to the "Autodesk" folder.                        
                result = Directory.GetParent(location).ToString();
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                //We can assume our path is: /Applications/Autodesk/maya2017/Maya.app/Contents
                //So we need to go up three folders.                

                var appFolder = Directory.GetParent(location);
                if (appFolder != null)
                {
                    var versionFolder = Directory.GetParent(appFolder.ToString());
                    if (versionFolder != null)
                    {
                        var autoDeskFolder = Directory.GetParent(versionFolder.ToString());
                        if (autoDeskFolder != null)
                        {
                            result = autoDeskFolder.ToString();
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a set of valid vendor folder paths with no trailing '/'
        /// </summary>
        private static HashSet<string> GetCustomVendorLocations()
        {
            HashSet<string> result = null;

            var environmentVariable = Environment.GetEnvironmentVariable("UNITY_3DAPP_VENDOR_LOCATIONS");

            if (!string.IsNullOrEmpty(environmentVariable))
            {
                result = new HashSet<string>();
                string[] locations = environmentVariable.Split(';');
                foreach (var location in locations)
                {
                    if (Directory.Exists(location))
                    {
                        result.Add(location);
                    }
                }
            }
            return result;
        }

        private static HashSet<string> GetDefaultVendorLocations()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                HashSet<string> windowsDefaults = new HashSet<string>() { "C:/Program Files/Autodesk", "D:/Program Files/Autodesk" };
                HashSet<string> existingDirectories = new HashSet<string>();
                foreach (string path in windowsDefaults)
                {
                    if (Directory.Exists(path))
                    {
                        existingDirectories.Add(path);
                    }
                }
                return existingDirectories;

            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                HashSet<string> MacOSDefaults = new HashSet<string>() { "/Applications/Autodesk" };
                HashSet<string> existingDirectories = new HashSet<string>();
                foreach (string path in MacOSDefaults)
                {
                    if (Directory.Exists(path))
                    {
                        existingDirectories.Add(path);
                    }
                }
                return existingDirectories;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieve available vendor locations.
        /// If there is valid alternative vendor locations, do not use defaults
        /// always use MAYA_LOCATION when available
        /// </summary>
        public static string[] DCCVendorLocations
        {
            get
            {
                HashSet<string> result = GetCustomVendorLocations();

                if (result == null)
                {
                    result = GetDefaultVendorLocations();
                }

                var additionalLocation = GetMayaLocationFromEnvironmentVariable("MAYA_LOCATION");

                if (!string.IsNullOrEmpty(additionalLocation))
                {
                    result.Add(additionalLocation);
                }

                return result.ToArray<string>();
            }
        }

        // Note: default values are set in LoadDefaults().
        public bool autoUpdaterEnabled = true;
        public bool launchAfterInstallation = true;
        public bool HideSendToUnityMenu = true;
        public bool BakeAnimation = true;
        public bool showConvertToPrefabDialog = true;

        public string IntegrationSavePath;

        public int selectedDCCApp = 0;

        /// <summary>
        /// The path where Convert To Model will save the new fbx and prefab.
        ///
        /// To help teams work together, this is stored to be relative to the
        /// Application.dataPath, and the path separator is the forward-slash
        /// (e.g. unix and http, not windows).
        ///
        /// Use GetRelativeSavePath / SetRelativeSavePath to get/set this
        /// value, properly interpreted for the current platform.
        /// </summary>
        [SerializeField]
        private List<string> prefabSavePaths = new List<string> ();

        [SerializeField]
        private List<string> fbxSavePaths = new List<string> ();

        [SerializeField]
        public int selectedFbxPath = 0;

        [SerializeField]
        public int selectedPrefabPath = 0;

        private int maxStoredSavePaths = 5;

        // List of names in order that they appear in option list
        [SerializeField]
        private List<string> dccOptionNames = new List<string>();
        // List of paths in order that they appear in the option list
        [SerializeField]
        private List<string> dccOptionPaths;

        // don't serialize as ScriptableObject does not get properly serialized on export
        [System.NonSerialized]
        public ExportModelSettings exportModelSettings;

        // store contents of export model settings for serialization
        [SerializeField]
        private ExportModelSettingsSerialize exportModelSettingsSerialize;

        [System.NonSerialized]
        public ConvertToPrefabSettings convertToPrefabSettings;

        [SerializeField]
        private ConvertToPrefabSettingsSerialize convertToPrefabSettingsSerialize;

        protected override void LoadDefaults()
        {
            autoUpdaterEnabled = true;
            showConvertToPrefabDialog = true;
            launchAfterInstallation = true;
            HideSendToUnityMenu = true;
            prefabSavePaths = new List<string>(){ kDefaultSavePath };
            fbxSavePaths = new List<string> (){ kDefaultSavePath };
            IntegrationSavePath = DefaultIntegrationSavePath;
            dccOptionPaths = null;
            dccOptionNames = null;
            BakeAnimation = true;
            exportModelSettings = ScriptableObject.CreateInstance (typeof(ExportModelSettings)) as ExportModelSettings;
            exportModelSettingsSerialize = exportModelSettings.info;
            convertToPrefabSettings = ScriptableObject.CreateInstance (typeof(ConvertToPrefabSettings)) as ConvertToPrefabSettings;
            convertToPrefabSettingsSerialize = convertToPrefabSettings.info;
        }

        /// <summary>
        /// Increments the name if there is a duplicate in dccAppOptions.
        /// </summary>
        /// <returns>The unique name.</returns>
        /// <param name="name">Name.</param>
        public static string GetUniqueDCCOptionName(string name){
            Debug.Assert(instance != null);
            if (name == null)
            {
                return null;
            }
            if (!instance.dccOptionNames.Contains(name)) {
                return name;
            }
            var format = "{1} ({0})";
            int index = 1;
            // try extracting the current index from the name and incrementing it
            var result = System.Text.RegularExpressions.Regex.Match(name, @"\((?<number>\d+?)\)$");
            if (result != null) {
                var number = result.Groups["number"].Value;
                int tempIndex;
                if (int.TryParse (number, out tempIndex)) {
                    var indexOfNumber = name.LastIndexOf (number);
                    format = name.Remove (indexOfNumber, number.Length).Insert (indexOfNumber, "{0}");
                    index = tempIndex+1;
                }
            }

            string uniqueName = null;
            do {
                uniqueName = string.Format (format, index, name);
                index++;
            } while (instance.dccOptionNames.Contains(uniqueName));

            return uniqueName;
        }

        public void SetDCCOptionNames(List<string> newList)
        {
            dccOptionNames = newList;
        }

        public void SetDCCOptionPaths(List<string> newList)
        {
            dccOptionPaths = newList;
        }

        public void ClearDCCOptionNames()
        {
            dccOptionNames.Clear();
        }

        public void ClearDCCOptions()
        {
            SetDCCOptionNames(null);
            SetDCCOptionPaths(null);
        }

        /// <summary>
        ///
        /// Find the latest program available and make that the default choice.
        /// Will always take any Maya version over any 3ds Max version.
        ///
        /// Returns the index of the most recent program in the list of dccOptionNames
        /// Returns -1 on error.
        /// </summary>
        public int GetPreferredDCCApp()
        {
            if (dccOptionNames == null)
            {
                return -1;
            }

            int result = -1;
            int newestDCCVersionNumber = -1;
            
            for (int i = 0; i < dccOptionNames.Count; i++)
            {
                int versionToCheck = FindDCCVersion(dccOptionNames[i]);
                if (versionToCheck == -1)
                {
                    if (dccOptionNames[i]=="MAYA_LOCATION")
                        return i;
                    
                    continue;
                }
                if (versionToCheck > newestDCCVersionNumber)
                {
                    result = i;
                    newestDCCVersionNumber = versionToCheck;
                }
                else if (versionToCheck == newestDCCVersionNumber)
                {
                    int selection = ChoosePreferredDCCApp(result, i);
                    if (selection == i)
                    {
                        result = i;
                        newestDCCVersionNumber = FindDCCVersion(dccOptionNames[i]);
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Takes the index of two program names from dccOptionNames and chooses our preferred one based on the preference list
        /// This happens in case of a tie between two programs with the same release year / version
        /// </summary>
        /// <param name="optionA"></param>
        /// <param name="optionB"></param>
        /// <returns></returns>
        private int ChoosePreferredDCCApp(int optionA, int optionB)
        {
            Debug.Assert(optionA >= 0 && optionB >= 0 && optionA < dccOptionNames.Count && optionB < dccOptionNames.Count);
            if (dccOptionNames.Count == 0)
            {
                return -1;
            }
            var appA = dccOptionNames[optionA];
            var appB = dccOptionNames[optionB];
            if (appA == null || appB == null || appA.Length <= 0 || appB.Length <= 0)
            {
                return -1;
            }

            int scoreA = s_PreferenceList.FindIndex(app => RemoveSpacesAndNumbers(app).Equals(RemoveSpacesAndNumbers(appA)));

            int scoreB = s_PreferenceList.FindIndex(app => RemoveSpacesAndNumbers(app).Equals(RemoveSpacesAndNumbers(appB)));

            return scoreA < scoreB ? optionA : optionB;
        }

        /// <summary>
        /// Takes a given string and removes any spaces or numbers from it
        /// </summary>
        /// <param name="s"></param>
        public static string RemoveSpacesAndNumbers(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, @"[\s^0-9]", "");
        }

        /// <summary>
        /// Finds the version based off of the title of the application
        /// </summary>
        /// <param name="path"></param>
        /// <returns> the year/version  OR -1 if the year could not be parsed </returns>
        private static int FindDCCVersion(string AppName)
        {
            if (string.IsNullOrEmpty(AppName))
            {
                return -1;
            }
            AppName = AppName.Trim();
            if (string.IsNullOrEmpty(AppName)) 
                return -1;
                
            string[] piecesArray = AppName.Split(' ');
            if (piecesArray.Length < 2)
            {
                return -1;
            }
            //Get the number, which is always the last chunk separated by a space.
            string number = piecesArray[piecesArray.Length - 1];

            int version;
            if (int.TryParse(number, out version))
            {
                return version;
            }
            else
            {
                //remove any letters in the string in a final attempt to extract an int from it (this will happen with MayaLT, for example)
                string AppNameCopy = AppName;
                string stringWithoutLetters = System.Text.RegularExpressions.Regex.Replace(AppNameCopy, "[^0-9]", "");

                if (int.TryParse(stringWithoutLetters, out version))
                {
                    return version;
                }

                float fVersion;
                //In case we are looking at something with a decimal based version- the int parse will fail so we'll need to parse it as a float.
                if (float.TryParse(number, out fVersion))
                {
                    return (int)fVersion;
                }
                return -1;
            }
        }

        /// <summary>
        /// Find Maya and 3DsMax installations at default install path.
        /// Add results to given dictionary.
        /// 
        /// If MAYA_LOCATION is set, add this to the list as well.
        /// </summary>
        private static void FindDCCInstalls() {
            var dccOptionNames = instance.dccOptionNames;
            var dccOptionPaths = instance.dccOptionPaths;

            // find dcc installation from vendor locations
            for (int i = 0; i < DCCVendorLocations.Length; i++)
            {
                if (!Directory.Exists(DCCVendorLocations[i]))
                {
                    // no autodesk products installed
                    continue;
                }
                // List that directory and find the right version:
                // either the newest version, or the exact version we wanted.
                var adskRoot = new System.IO.DirectoryInfo(DCCVendorLocations[i]);
                foreach (var productDir in adskRoot.GetDirectories())
                {
                    var product = productDir.Name;

                    // Only accept those that start with 'maya' in either case.
                    if (product.StartsWith ("maya", StringComparison.InvariantCultureIgnoreCase)) {
                        string version = product.Substring ("maya".Length);
                        dccOptionPaths.Add (GetMayaExePathFromLocation (productDir.FullName.Replace ("\\", "/")));
                        dccOptionNames.Add (GetUniqueDCCOptionName(kMayaOptionName + version));
                        continue;
                    }

                    if (product.StartsWith("3ds max", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var exePath = string.Format("{0}/{1}", productDir.FullName.Replace("\\", "/"), "3dsmax.exe");

                        string version = product.Substring("3ds max ".Length);
                        var maxOptionName = GetUniqueDCCOptionName(kMaxOptionName + version);

                        if (IsEarlierThanMax2017(maxOptionName))
                        {
                            continue;
                        }

                        dccOptionPaths.Add(exePath);
                        dccOptionNames.Add(maxOptionName);
                    }
                }
            }

            // add extra locations defined by special environment variables
            string location = GetMayaLocationFromEnvironmentVariable("MAYA_LOCATION");

            if (!string.IsNullOrEmpty(location))
            {
                dccOptionPaths.Add(GetMayaExePathFromLocation(location));
                dccOptionNames.Add("MAYA_LOCATION");
            }

            instance.selectedDCCApp = instance.GetPreferredDCCApp();
        }

        /// <summary>
        /// Returns the first valid folder in our list of vendor locations
        /// </summary>
        /// <returns>The first valid vendor location</returns>
        public static string GetFirstValidVendorLocation()
        {
            string[] locations = DCCVendorLocations;
            for (int i = 0; i < locations.Length; i++)
            {
                //Look through the list of locations we have and take the first valid one
                if (Directory.Exists(locations[i]))
                {
                    return locations[i];
                }
            }
            //if no valid locations exist, just take us to the project folder
            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Gets the maya exe at Maya install location.
        /// </summary>
        /// <returns>The maya exe path.</returns>
        /// <param name="location">Location of Maya install.</param>
        private static string GetMayaExePathFromLocation(string location)
        {
            switch (Application.platform) {
            case RuntimePlatform.WindowsEditor:
                return location + "/bin/maya.exe";
            case RuntimePlatform.OSXEditor:
                // MAYA_LOCATION on mac is set by Autodesk to be the
                // Contents directory. But let's make it easier on people
                // and allow just having it be the app bundle or a
                // directory that holds the app bundle.
                if (location.EndsWith(".app/Contents")) {
                    return location + "/MacOS/Maya";
                } else if (location.EndsWith(".app")) {
                    return location + "/Contents/MacOS/Maya";
                } else {
                    return location + "/Maya.app/Contents/MacOS/Maya";
                }
            default:
                throw new NotImplementedException ();
            }
        }

        public static GUIContent[] GetDCCOptions(){
            if (instance.dccOptionNames == null ||
                instance.dccOptionNames.Count != instance.dccOptionPaths.Count ||
                instance.dccOptionNames.Count == 0) {

                instance.dccOptionPaths = new List<string> ();
                instance.dccOptionNames = new List<string> ();
                FindDCCInstalls ();
            }
            // store the selected app if any
            string prevSelection = GetSelectedDCCPath();

            // remove options that no longer exist
            List<string> pathsToDelete = new List<string>();
            List<string> namesToDelete = new List<string>();
            for(int i = 0; i < instance.dccOptionPaths.Count; i++) {
                var dccPath = instance.dccOptionPaths [i];
                if (!File.Exists (dccPath)) {
                    namesToDelete.Add (instance.dccOptionNames [i]);
                    pathsToDelete.Add (dccPath);
                }
            }
            foreach (var str in pathsToDelete) {
                instance.dccOptionPaths.Remove (str);
            }
            foreach (var str in namesToDelete) {
                instance.dccOptionNames.Remove (str);
            }

            // set the selected DCC app to the previous selection
            instance.selectedDCCApp = instance.dccOptionPaths.IndexOf (prevSelection);
            if (instance.selectedDCCApp < 0) {
                // find preferred app if previous selection no longer exists
                instance.selectedDCCApp = instance.GetPreferredDCCApp ();
            }

            if (instance.dccOptionPaths.Count <= 0) {
                instance.selectedDCCApp = 0;
                return new GUIContent[]{
                    new GUIContent("<No 3D Application found>")
                };
            }

            GUIContent[] optionArray = new GUIContent[instance.dccOptionPaths.Count];
            for(int i = 0; i < instance.dccOptionPaths.Count; i++){
                optionArray [i] = new GUIContent(
                    instance.dccOptionNames[i],
                    instance.dccOptionPaths[i]
                );
            }
            return optionArray;
        }

        public enum DCCType { Maya, Max };

        public static void AddDCCOption(string newOption, DCCType dcc){
            if (Application.platform == RuntimePlatform.OSXEditor && dcc == DCCType.Maya) {
                // on OSX we get a path ending in .app, which is not quite the exe
                newOption = GetMayaExePathFromLocation(newOption);
            }

            var dccOptionPaths = instance.dccOptionPaths;
            if (dccOptionPaths.Contains(newOption)) {
                instance.selectedDCCApp = dccOptionPaths.IndexOf (newOption);
                return;
            }

            string optionName = "";
            switch (dcc) {
            case DCCType.Maya:
                var version = AskMayaVersion(newOption);
                if (version == null)
                {
                    Debug.LogError("This version of Maya could not be launched properly");
                    UnityEditor.EditorUtility.DisplayDialog("Error Loading 3D Application",
                        "Failed to add Maya option, could not get version number from maya.exe",
                        "Ok");
                    return;
                }
                optionName = GetUniqueDCCOptionName("Maya " + version);
                break;
            case DCCType.Max:
                optionName = GetMaxOptionName (newOption);
                if (ExportSettings.IsEarlierThanMax2017(optionName))
                {
                    Debug.LogError("Earlier than 3ds Max 2017 is not supported");
                    UnityEditor.EditorUtility.DisplayDialog(
                        "Error adding 3D Application",
                        "Unity Integration only supports 3ds Max 2017 or later",
                        "Ok");
                        return;
                }
                    break;
            default:
                throw new System.NotImplementedException();
            }

            instance.dccOptionNames.Add (optionName);
            dccOptionPaths.Add (newOption);
            instance.selectedDCCApp = dccOptionPaths.Count - 1;
        }

        /// <summary>
        /// Ask the version number by running maya.
        /// </summary>
        static string AskMayaVersion(string exePath) {
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = exePath;
            myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.Arguments = "-v";
            myProcess.EnableRaisingEvents = true;
            myProcess.Start();
            string resultString = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();

            // Output is like: Maya 2018, Cut Number 201706261615
            // We want the stuff after 'Maya ' and before the comma.
            // (Uni-31601) less brittle! Consider also the mel command "about -version".
            if (string.IsNullOrEmpty(resultString))
            {
                return null;
            }

            resultString = resultString.Trim();
            var commaIndex = resultString.IndexOf(',');

            if (commaIndex != -1)
            {
                const int versionStart = 5; // length of "Maya "
                return resultString.Length > versionStart ? resultString.Substring(0, commaIndex).Substring(versionStart) : null;
            }
            else
            {
                //This probably means we tried to launch Maya to check the version but it was some sort of broken maya.
                //We'll just return null and throw an error for it.
                return null;
            }
        }

        /// <summary>
        /// Gets the unique label for a new 3DsMax dropdown option.
        /// </summary>
        /// <returns>The 3DsMax dropdown option label.</returns>
        /// <param name="exePath">Exe path.</param>
        public static string GetMaxOptionName(string exePath){
            return GetUniqueDCCOptionName(Path.GetFileName(Path.GetDirectoryName (exePath)));
        }

        public static bool IsEarlierThanMax2017(string AppName){
            int version = FindDCCVersion(AppName);
            return version != -1 && version < 2017;
        }

        public static string GetSelectedDCCPath()
        {
            return (instance.dccOptionPaths.Count>0 &&
                instance.selectedDCCApp >= 0 &&
                instance.selectedDCCApp < instance.dccOptionPaths.Count) ? instance.dccOptionPaths [instance.selectedDCCApp] : "";
        }

        public static string GetSelectedDCCName()
        {
            return (instance.dccOptionNames.Count>0 &&
                instance.selectedDCCApp >= 0 &&
                instance.selectedDCCApp < instance.dccOptionNames.Count) ? instance.dccOptionNames [instance.selectedDCCApp] : "";
        }

        public static bool CanInstall()
        {
            return instance.dccOptionPaths.Count > 0;
        }

        public static string GetProjectRelativePath(string fullPath){
            var assetRelativePath = UnityEditor.Formats.Fbx.Exporter.ExportSettings.ConvertToAssetRelativePath(fullPath);
            var projectRelativePath = "Assets/" + assetRelativePath;
            if (string.IsNullOrEmpty(assetRelativePath)) {
                throw new System.Exception("Path " + fullPath + " must be in the Assets folder.");
            }
            return projectRelativePath;
        }

        /// <summary>
        /// The relative save paths for given absolute paths.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// </summary>
        public static string[] GetRelativeSavePaths(List<string> exportSavePaths){
            if (exportSavePaths.Count == 0) {
                exportSavePaths.Add (kDefaultSavePath);
            }
            string[] relSavePaths = new string[exportSavePaths.Count];
            // use special forward slash unicode char as "/" is a special character
            // that affects the dropdown layout.
            string forwardslash = " \u2044 ";
            for (int i = 0; i < relSavePaths.Length; i++) {
                relSavePaths [i] = string.Format("Assets{0}{1}", forwardslash, exportSavePaths[i] == "."? "" : NormalizePath(exportSavePaths [i], isRelative: true).Replace("/", forwardslash));
            }
            return relSavePaths;
        }

        /// <summary>
        /// The path where Export model will save the new fbx.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// </summary>
        public static string[] GetRelativeFbxSavePaths(){
            return GetRelativeSavePaths(instance.fbxSavePaths);
        }

        /// <summary>
        /// The path where Convert to Prefab will save the new prefab.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// </summary>
        public static string[] GetRelativePrefabSavePaths(){
            return GetRelativeSavePaths(instance.prefabSavePaths);
        }

        /// <summary>
        /// Adds the save path to given save path list.
        /// </summary>
        /// <param name="savePath">Save path.</param>
        /// <param name="exportSavePaths">Export save paths.</param>
        public static void AddSavePath(string savePath, ref List<string> exportSavePaths){
            savePath = NormalizePath (savePath, isRelative: true);
            if (exportSavePaths.Contains (savePath)) {
                // move to first place if it isn't already
                if (exportSavePaths [0] == savePath) {
                    return;
                }
                exportSavePaths.Remove (savePath);
            }

            if (exportSavePaths.Count >= instance.maxStoredSavePaths) {
                // remove last used path
                exportSavePaths.RemoveAt(exportSavePaths.Count-1);
            }

            exportSavePaths.Insert (0, savePath);
        }

        public static void AddFbxSavePath(string savePath){
            AddSavePath (savePath, ref instance.fbxSavePaths);
            instance.selectedFbxPath = 0;
        }

        public static void AddPrefabSavePath(string savePath){
            AddSavePath (savePath, ref instance.prefabSavePaths);
            instance.selectedPrefabPath = 0;
        }

        public static string GetAbsoluteSavePath(string relativePath){
            var absolutePath = Path.Combine(Application.dataPath, relativePath);
            return NormalizePath(absolutePath, isRelative: false,
                separator: Path.DirectorySeparatorChar);
        }

        public static string GetFbxAbsoluteSavePath(){
            if (instance.fbxSavePaths.Count <= 0) {
                instance.fbxSavePaths.Add (kDefaultSavePath);
            }
            return GetAbsoluteSavePath (instance.fbxSavePaths [instance.selectedFbxPath]);
        }

        public static string GetPrefabAbsoluteSavePath(){
            if (instance.prefabSavePaths.Count <= 0) {
                instance.prefabSavePaths.Add (kDefaultSavePath);
            }
            return GetAbsoluteSavePath (instance.prefabSavePaths [instance.selectedPrefabPath]);
        }

        public static string GetIntegrationSavePath()
        {
            //If the save path gets messed up and ends up not being valid, just use the project folder as the default
            if (string.IsNullOrEmpty(instance.IntegrationSavePath) ||
                !Directory.Exists(instance.IntegrationSavePath))
            {
                //The project folder, above the asset folder
                instance.IntegrationSavePath = DefaultIntegrationSavePath;
            }
            return instance.IntegrationSavePath;
        }

        public static void SetIntegrationSavePath(string newPath)
        {
            instance.IntegrationSavePath = newPath;
        }

        /// <summary>
        /// Convert an absolute path into a relative path like what you would
        /// get from GetRelativeSavePath.
        ///
        /// This uses '/' as the path separator.
        ///
        /// If 'requireSubdirectory' is the default on, return empty-string if the full
        /// path is not in a subdirectory of assets.
        /// </summary>
        public static string ConvertToAssetRelativePath(string fullPathInAssets, bool requireSubdirectory = true)
        {
            if (!Path.IsPathRooted(fullPathInAssets)) {
                fullPathInAssets = Path.GetFullPath(fullPathInAssets);
            }
            var relativePath = GetRelativePath(Application.dataPath, fullPathInAssets);
            if (requireSubdirectory && relativePath.StartsWith("..")) {
                if (relativePath.Length == 2 || relativePath[2] == '/') {
                    // The relative path has us pop out to another directory,
                    // so return an empty string as requested.
                    return "";
                }
            }
            return relativePath;
        }

        /// <summary>
        /// Compute how to get from 'fromDir' to 'toDir' via a relative path.
        /// </summary>
        public static string GetRelativePath(string fromDir, string toDir,
                char separator = '/')
        {
            // https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
            // Except... the MakeRelativeUri that ships with Unity is buggy.
            // e.g. https://bugzilla.xamarin.com/show_bug.cgi?id=5921
            // among other bugs. So we roll our own.

            // Normalize the paths, assuming they're absolute paths (if they
            // aren't, they get normalized as relative paths)
            fromDir = NormalizePath(fromDir, isRelative: false);
            toDir = NormalizePath(toDir, isRelative: false);

            // Break them into path components.
            var fromDirs = fromDir.Split('/');
            var toDirs = toDir.Split('/');

            // Find the least common ancestor
            int lca = -1;
            for(int i = 0, n = System.Math.Min(fromDirs.Length, toDirs.Length); i < n; ++i) {
                if (fromDirs[i] != toDirs[i]) { break; }
                lca = i;
            }

            // Step up from the fromDir to the lca, then down from lca to the toDir.
            // If from = /a/b/c/d
            // and to  = /a/b/e/f/g
            // Then we need to go up 2 and down 3.
            var nStepsUp = (fromDirs.Length - 1) - lca;
            var nStepsDown = (toDirs.Length - 1) - lca;
            if (nStepsUp + nStepsDown == 0) {
                return ".";
            }

            var relDirs = new string[nStepsUp + nStepsDown];
            for(int i = 0; i < nStepsUp; ++i) {
                relDirs[i] = "..";
            }
            for(int i = 0; i < nStepsDown; ++i) {
                relDirs[nStepsUp + i] = toDirs[lca + 1 + i];
            }

            return string.Join("" + separator, relDirs);
        }

        /// <summary>
        /// Normalize a path, cleaning up path separators, resolving '.' and
        /// '..', removing duplicate and trailing path separators, etc.
        ///
        /// If the path passed in is a relative path, we remove leading path separators.
        /// If it's an absolute path we don't.
        ///
        /// If you claim the path is absolute but actually it's relative, we
        /// treat it as a relative path.
        /// </summary>
        public static string NormalizePath(string path, bool isRelative,
                char separator = '/')
        {
            // Use slashes to simplify the code (we're going to clobber them all anyway).
            path = path.Replace('\\', '/');

            // If we're supposed to be an absolute path, but we're actually a
            // relative path, ignore the 'isRelative' flag.
            if (!isRelative && !Path.IsPathRooted(path)) {
                isRelative = true;
            }

            // Build up a list of directory items.
            var dirs = path.Split('/');

            // Modify dirs in-place, reading from readIndex and remembering
            // what index we've written to.
            int lastWriteIndex = -1;
            for (int readIndex = 0, n = dirs.Length; readIndex < n; ++readIndex) {
                var dir = dirs[readIndex];

                // Skip duplicate path separators.
                if (dir == "") {
                    // Skip if it's not a leading path separator.
                   if (lastWriteIndex >= 0) {
                       continue; }

                   // Also skip if it's leading and we have a relative path.
                   if (isRelative) {
                       continue;
                   }
                }

                // Skip '.'
                if (dir == ".") {
                    continue;
                }

                // Erase the previous directory we read on '..'.
                // Exception: we can start with '..'
                // Exception: we can have multiple '..' in a row.
                //
                // Note: this ignores the actual file system and the funny
                // results you see when there are symlinks.
                if (dir == "..") {
                    if (lastWriteIndex == -1) {
                        // Leading '..' => handle like a normal directory.
                    } else if (dirs[lastWriteIndex] == "..") {
                        // Multiple ".." => handle like a normal directory.
                    } else {
                        // Usual case: delete the previous directory.
                        lastWriteIndex--;
                        continue;
                    }
                }

                // Copy anything else to the next index.
                ++lastWriteIndex;
                dirs[lastWriteIndex] = dirs[readIndex];
            }

            if (lastWriteIndex == -1 || (lastWriteIndex == 0 && dirs[lastWriteIndex] == "")) {
                // If we didn't keep anything, we have the empty path.
                // For an absolute path that's / ; for a relative path it's .
                if (isRelative) {
                    return ".";
                } else {
                    return "" + separator;
                }
            } else {
                // Otherwise print out the path with the proper separator.
                return String.Join("" + separator, dirs, 0, lastWriteIndex + 1);
            }
        }

        [MenuItem("Edit/Project Settings/Fbx Export", priority = 300)]
        static void ShowManager()
        {
            instance.name = "Fbx Export Settings";
            Selection.activeObject = instance;
            instance.Load();
        }

        protected override void Load ()
        {
            base.Load ();
            if (!instance.exportModelSettings) {
                instance.exportModelSettings = ScriptableObject.CreateInstance (typeof(ExportModelSettings)) as ExportModelSettings;
            }
            instance.exportModelSettings.info = instance.exportModelSettingsSerialize;

            if (!instance.convertToPrefabSettings) {
                instance.convertToPrefabSettings = ScriptableObject.CreateInstance (typeof(ConvertToPrefabSettings)) as ConvertToPrefabSettings;
            }
            instance.convertToPrefabSettings.info = instance.convertToPrefabSettingsSerialize;

        }

        public void Save()
        {
            exportModelSettingsSerialize = exportModelSettings.info;
            convertToPrefabSettingsSerialize = convertToPrefabSettings.info;
            instance.Save (true);
        }
    }

    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
    {
        private static T s_Instance;
        public static T instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = ScriptableObject.CreateInstance<T>();
                    s_Instance.Load();
                }
                return s_Instance;
            }
        }

        protected ScriptableSingleton()
        {
            if (s_Instance != null)
            {
                Debug.LogError(typeof(T) + " already exists. Did you query the singleton in a constructor?");
            }
        }

        protected abstract void LoadDefaults();

        protected virtual void Load()
        {
            string filePath = GetFilePath();
            if (!System.IO.File.Exists(filePath)) {
                LoadDefaults();
            } else {
                try {
                    var fileData = System.IO.File.ReadAllText(filePath);
                    EditorJsonUtility.FromJsonOverwrite(fileData, s_Instance);
                } catch(Exception xcp) {
                    // Quash the exception and take the default settings.
                    Debug.LogException(xcp);
                    LoadDefaults();
                }
            }
        }

        protected virtual void Save(bool saveAsText)
        {
            if (s_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");
                return;
            }
            string filePath = GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                string directoryName = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                System.IO.File.WriteAllText(filePath, EditorJsonUtility.ToJson(s_Instance, true));
            }
        }

        private static string GetFilePath()
        {
            foreach(var attr in typeof(T).GetCustomAttributes(true)) {
                FilePathAttribute filePathAttribute = attr as FilePathAttribute;
                if (filePathAttribute != null)
                {
                    return filePathAttribute.filepath;
                }
            }
            return null;
        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        public enum Location
        {
            PreferencesFolder,
            ProjectFolder
        }
        public string filepath
        {
            get;
            set;
        }
        public FilePathAttribute(string relativePath, FilePathAttribute.Location location)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogError("Invalid relative path! (its null or empty)");
                return;
            }
            if (relativePath[0] == '/')
            {
                relativePath = relativePath.Substring(1);
            }
            if (location == FilePathAttribute.Location.PreferencesFolder)
            {
                this.filepath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
            }
            else
            {
                this.filepath = relativePath;
            }
        }
    }

}
