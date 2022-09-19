using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor.Presets;

namespace UnityEditor.Formats.Fbx.Exporter
{
    /// <summary>
    /// FBX export format options.
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// Output the FBX in ASCII format.
        /// </summary>
        ASCII = 0,
        /// <summary>
        /// Output the FBX in Binary format.
        /// </summary>
        Binary = 1
    }

    /// <summary>
    /// Options for the type of data to include in the export
    /// (Model only, animation only, or model and animation).
    /// </summary>
    public enum Include
    {
        /// <summary>
        /// Export the model without animation.
        /// </summary>
        Model = 0,
        /// <summary>
        /// Export the animation only.
        /// </summary>
        Anim = 1,
        /// <summary>
        /// Export both the model and animation.
        /// </summary>
        ModelAndAnim = 2
    }

    /// <summary>
    /// Options for the position to use for the root GameObject.
    /// </summary>
    public enum ObjectPosition
    {
        /// <summary>
        /// For a single root, uses the local transform information.
        /// If you select multiple GameObjects for export, the FBX Exporter centers GameObjects
        /// around a shared root while keeping their relative placement unchanged.
        /// </summary>
        LocalCentered = 0,
        /// <summary>
        /// Uses the world position of the GameObjects.
        /// </summary>
        WorldAbsolute = 1,
        /// <summary>
        /// Exports the GameObject to (0,0,0).
        /// For convert to FBX prefab variant only, no UI option.
        /// </summary>
        Reset = 2
    }

    /// <summary>
    /// LODs to export for LOD groups.
    /// </summary>
    /// <remarks>
    /// Notes:
    /// - The FBX Exporter ignores LODs outside of selected hierarchy.
    /// - The FBX Exporter does not filter out objects that are used as LODs and doesn't
    ///   export them if they aren’t direct descendants of their respective LOD Group
    /// </remarks>
    public enum LODExportType
    {
        /// <summary>
        /// Export all LODs.
        /// </summary>
        All = 0,
        /// <summary>
        /// Export only the highest LOD.
        /// </summary>
        Highest = 1,
        /// <summary>
        /// Export only the lowest LOD.
        /// </summary>
        Lowest = 2
    }

    /// <summary>
    /// Exception class for FBX export settings.
    /// </summary>
    [System.Serializable]
    public class FbxExportSettingsException : System.Exception
    {
        internal FbxExportSettingsException() {}

        internal FbxExportSettingsException(string message)
            : base(message) {}

        internal FbxExportSettingsException(string message, System.Exception inner)
            : base(message, inner) {}

        internal FbxExportSettingsException(SerializationInfo info, StreamingContext context)
            : base(info, context) {}
    }

    [CustomEditor(typeof(ExportSettings))]
    internal class ExportSettingsEditor : UnityEditor.Editor
    {
        Vector2 scrollPos = Vector2.zero;
        const float LabelWidth = 180;
        const float SelectableLabelMinWidth = 90;
        const float BrowseButtonWidth = 25;
        const float FieldOffset = 18;
        const float BrowseButtonOffset = 5;

        const float ExportOptionsLabelWidth = 205;
        const float ExportOptionsFieldOffset = 18;

        private bool m_showExportSettingsOptions = true;
        private bool m_showConvertSettingsOptions = true;

        private ExportModelSettingsEditor m_exportModelEditor;
        private ConvertToPrefabSettingsEditor m_convertEditor;

        private void OnEnable()
        {
            ExportSettings exportSettings = (ExportSettings)target;
            m_exportModelEditor = UnityEditor.Editor.CreateEditor(exportSettings.ExportModelSettings) as ExportModelSettingsEditor;
            m_convertEditor = UnityEditor.Editor.CreateEditor(exportSettings.ConvertToPrefabSettings) as ConvertToPrefabSettingsEditor;
        }

        static class Style
        {
            public static GUIContent Application3D = new GUIContent(
                "3D Application",
                "Select the 3D Application for which you would like to install the Unity integration.");
            public static GUIContent KeepOpen = new GUIContent("Keep Open",
                "Keep the selected 3D application open after Unity integration install has completed.");
            public static GUIContent HideNativeMenu = new GUIContent("Hide Native Menu",
                "Replace Maya's native 'Send to Unity' menu with the Unity Integration's menu");
            public static GUIContent InstallIntegrationContent = new GUIContent(
                "Install Unity Integration",
                "Install and configure the Unity integration for the selected 3D application so that you can import and export directly with this project.");
            public static GUIContent RepairMissingScripts = new GUIContent(
                "Run Component Updater",
                "If FBX exporter version 1.3.0f1 or earlier was previously installed, then links to the FbxPrefab component will need updating.\n" +
                "Run this to update all FbxPrefab references in text serialized prefabs and scene files.");
            public static GUIContent DisplayOptionsWindow = new GUIContent(
                "Display Options Window",
                "Show the Convert dialog when converting to an FBX Prefab Variant");
        }

        private void ClearExportWindowSettings<T>(string prefix) where T : ExportOptionsEditorWindow
        {
            string defaultSettings = null;
            if (typeof(T) == typeof(ExportModelEditorWindow))
            {
                defaultSettings = EditorJsonUtility.ToJson(ExportSettings.instance.ExportModelSettings.info);
            }
            else
            {
                defaultSettings = EditorJsonUtility.ToJson(ExportSettings.instance.ConvertToPrefabSettings.info);
            }

            if (EditorWindow.HasOpenInstances<T>())
            {
                // clear settings on the window and update the UI
                var win = EditorWindow.GetWindow<T>(ExportOptionsEditorWindow.DefaultWindowTitle, focus: false);
                win.ResetSessionSettings(defaultSettings);
                win.Repaint();
            }
            else
            {
                // Clear what is stored in the session.
                // Window will update next time it is opened
                ExportOptionsEditorWindow.ResetAllSessionSettings(prefix, defaultSettings);
            }
        }

        private void ShowExportPathUI(string label, string tooltip, string openFolderPanelTitle, bool isSingletonInstance, bool isConvertToPrefabOptions = false)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(ExportOptionsLabelWidth - ExportOptionsFieldOffset));

            var pathLabels = ExportSettings.GetMixedFbxSavePaths();
            if (isConvertToPrefabOptions)
            {
                pathLabels = ExportSettings.GetRelativePrefabSavePaths();
            }

            EditorGUI.BeginChangeCheck();
            if (isConvertToPrefabOptions)
            {
                ExportSettings.instance.SelectedPrefabPath = EditorGUILayout.Popup(ExportSettings.instance.SelectedPrefabPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));
            }
            else
            {
                ExportSettings.instance.SelectedFbxPath = EditorGUILayout.Popup(ExportSettings.instance.SelectedFbxPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));
            }
            if (EditorGUI.EndChangeCheck() && isSingletonInstance)
            {
                if (isConvertToPrefabOptions)
                {
                    ClearExportWindowSettings<ConvertToPrefabEditorWindow>(ConvertToPrefabEditorWindow.k_SessionStoragePrefix);
                }
                else
                {
                    ClearExportWindowSettings<ExportModelEditorWindow>(ExportModelEditorWindow.k_SessionStoragePrefix);
                }
            }

            // Set export setting for exporting outside the project on choosing a path
            if (!isConvertToPrefabOptions)
            {
                // Set export setting for exporting outside the project on choosing a path
                var exportOutsideProject = !pathLabels[ExportSettings.instance.SelectedFbxPath].Substring(0, 6).Equals("Assets");
                m_exportModelEditor.SetExportingOutsideProject(exportOutsideProject);
            }

            EditorGUI.BeginDisabledGroup(!isSingletonInstance);
            var str = isConvertToPrefabOptions ? "save prefab" : "export";
            var buttonTooltip = string.Format("Browse to a new location to {0} to", str);
            if (GUILayout.Button(new GUIContent("...", buttonTooltip), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
            {
                string initialPath = Application.dataPath;

                string fullPath = EditorUtility.SaveFolderPanel(
                    openFolderPanelTitle, initialPath, null
                );

                // Unless the user canceled, save path.
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var relativePath = ExportSettings.ConvertToAssetRelativePath(fullPath);

                    // We're exporting outside Assets folder, so store the absolute path
                    if (string.IsNullOrEmpty(relativePath))
                    {
                        if (isConvertToPrefabOptions)
                        {
                            Debug.LogWarning("Please select a location in the Assets folder");
                        }
                        else
                        {
                            ExportSettings.AddFbxSavePath(fullPath, exportOutsideProject: true);
                            ClearExportWindowSettings<ExportModelEditorWindow>(ExportModelEditorWindow.k_SessionStoragePrefix);
                        }
                    }
                    // Store the relative path to the Assets folder
                    else
                    {
                        if (isConvertToPrefabOptions)
                        {
                            ExportSettings.AddPrefabSavePath(relativePath);
                            ClearExportWindowSettings<ConvertToPrefabEditorWindow>(ConvertToPrefabEditorWindow.k_SessionStoragePrefix);
                        }
                        else
                        {
                            ExportSettings.AddFbxSavePath(relativePath);
                            ClearExportWindowSettings<ExportModelEditorWindow>(ExportModelEditorWindow.k_SessionStoragePrefix);
                        }
                    }
                    // Make sure focus is removed from the selectable label
                    // otherwise it won't update
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                }
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            ExportSettings exportSettings = (ExportSettings)target;
            bool isSingletonInstance = this.targets.Length == 1 && this.target == ExportSettings.instance;

            // Increasing the label width so that none of the text gets cut off
            EditorGUIUtility.labelWidth = LabelWidth;

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            var version = UnityEditor.Formats.Fbx.Exporter.ModelExporter.GetVersionFromReadme();
            if (!string.IsNullOrEmpty(version))
            {
                GUILayout.Label("Version: " + version, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
            }

            GUILayout.BeginVertical();

            EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Style.DisplayOptionsWindow, GUILayout.Width(LabelWidth));
            exportSettings.DisplayOptionsWindow = EditorGUILayout.Toggle(
                exportSettings.DisplayOptionsWindow
            );
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // EXPORT SETTINGS
            m_showExportSettingsOptions = EditorGUILayout.Foldout(m_showExportSettingsOptions, "FBX File Options", EditorStyles.foldoutHeader);
            if (m_showExportSettingsOptions)
            {
                EditorGUI.indentLevel++;
                ShowExportPathUI("Export Path", "Location where the FBX will be saved.", "Select Export Model Path", isSingletonInstance, isConvertToPrefabOptions: false);

                EditorGUI.BeginChangeCheck();
                m_exportModelEditor.LabelWidth = ExportOptionsLabelWidth;
                m_exportModelEditor.FieldOffset = ExportOptionsFieldOffset;
                m_exportModelEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck() && isSingletonInstance)
                {
                    ClearExportWindowSettings<ExportModelEditorWindow>(ExportModelEditorWindow.k_SessionStoragePrefix);
                }
                EditorGUI.indentLevel--;
            }
            // --------------------------

            // CONVERT TO PREFAB SETTINGS
            m_showConvertSettingsOptions = EditorGUILayout.Foldout(m_showConvertSettingsOptions, "Convert to Prefab Options", EditorStyles.foldoutHeader);
            if (m_showConvertSettingsOptions)
            {
                EditorGUI.indentLevel++;
                ShowExportPathUI("Prefab Path", "Relative path for saving FBX Prefab Variants.", "Select FBX Prefab Variant Save Path", isSingletonInstance, isConvertToPrefabOptions: true);

                EditorGUI.BeginChangeCheck();
                m_convertEditor.LabelWidth = ExportOptionsLabelWidth;
                m_convertEditor.FieldOffset = ExportOptionsFieldOffset;
                m_convertEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck() && isSingletonInstance)
                {
                    ClearExportWindowSettings<ConvertToPrefabEditorWindow>(ConvertToPrefabEditorWindow.k_SessionStoragePrefix);
                }
                EditorGUI.indentLevel--;
            }
            // --------------------------

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Integration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Style.Application3D, GUILayout.Width(LabelWidth));

            // dropdown to select Maya version to use
            var options = ExportSettings.GetDCCOptions();

            exportSettings.SelectedDCCApp = EditorGUILayout.Popup(exportSettings.SelectedDCCApp, options);

            EditorGUI.BeginDisabledGroup(!isSingletonInstance);
            if (GUILayout.Button(new GUIContent("...", "Browse to a 3D application in a non-default location"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
            {
                var ext = "";
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        ext = "exe";
                        break;
                    case RuntimePlatform.OSXEditor:
                        ext = "app";
                        break;
                    default:
                        throw new System.NotImplementedException();
                }

                string dccPath = EditorUtility.OpenFilePanel("Select Digital Content Creation Application", ExportSettings.FirstValidVendorLocation, ext);

                // check that the path is valid and references the maya executable
                if (!string.IsNullOrEmpty(dccPath))
                {
                    ExportSettings.DCCType foundDCC = ExportSettings.DCCType.Maya;
                    var foundDCCPath = TryFindDCC(dccPath, ext, ExportSettings.DCCType.Maya);
                    if (foundDCCPath == null && Application.platform == RuntimePlatform.WindowsEditor)
                    {
                        foundDCCPath = TryFindDCC(dccPath, ext, ExportSettings.DCCType.Max);
                        foundDCC = ExportSettings.DCCType.Max;
                    }
                    if (foundDCCPath == null)
                    {
                        Debug.LogError(string.Format("Could not find supported 3D application at: \"{0}\"", Path.GetDirectoryName(dccPath)));
                    }
                    else
                    {
                        dccPath = foundDCCPath;
                        ExportSettings.AddDCCOption(dccPath, foundDCC);
                    }
                    Repaint();
                }
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Style.KeepOpen, GUILayout.Width(LabelWidth));
            exportSettings.LaunchAfterInstallation = EditorGUILayout.Toggle(
                exportSettings.LaunchAfterInstallation
            );
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Style.HideNativeMenu, GUILayout.Width(LabelWidth));
            exportSettings.HideSendToUnityMenuProperty = EditorGUILayout.Toggle(
                exportSettings.HideSendToUnityMenuProperty
            );
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // disable button if no 3D application is available
            EditorGUI.BeginDisabledGroup(!isSingletonInstance || !ExportSettings.CanInstall());
            if (GUILayout.Button(Style.InstallIntegrationContent))
            {
                EditorApplication.delayCall += UnityEditor.Formats.Fbx.Exporter.IntegrationsUI.InstallDCCIntegration;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("FBX Prefab Component Updater", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!isSingletonInstance);
            if (GUILayout.Button(Style.RepairMissingScripts))
            {
                var componentUpdater = new UnityEditor.Formats.Fbx.Exporter.RepairMissingScripts();
                var filesToRepairCount = componentUpdater.AssetsToRepairCount;
                var dialogTitle = "FBX Prefab Component Updater";
                if (filesToRepairCount > 0)
                {
                    bool result = UnityEditor.EditorUtility.DisplayDialog(dialogTitle,
                        string.Format("Found {0} prefab(s) and/or scene(s) with components requiring update.\n\n" +
                            "If you choose 'Go Ahead', the FbxPrefab components in these assets " +
                            "will be automatically updated to work with the latest FBX exporter.\n" +
                            "You should make a backup before proceeding.", filesToRepairCount),
                        "I Made a Backup. Go Ahead!", "No Thanks");
                    if (result)
                    {
                        componentUpdater.ReplaceGUIDInTextAssets();
                    }
                    else
                    {
                        var assetsToRepair = componentUpdater.GetAssetsToRepair();
                        Debug.LogFormat("Failed to update the FbxPrefab components in the following files:\n{0}", string.Join("\n", assetsToRepair));
                    }
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog(dialogTitle,
                        "Couldn't find any prefabs or scenes that require updating", "Ok");
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (GUI.changed)
            {
                // Only save the settings if we are in the Singleton instance.
                // Otherwise, the user is editing a preset and we don't need to save.
                if (isSingletonInstance)
                {
                    exportSettings.Save();
                }
            }
        }

        private static string TryFindDCC(string dccPath, string ext, ExportSettings.DCCType dccType)
        {
            string dccName = "";
            switch (dccType)
            {
                case ExportSettings.DCCType.Maya:
                    dccName = "maya";
                    break;
                case ExportSettings.DCCType.Max:
                    dccName = "3dsmax";
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            if (Path.GetFileNameWithoutExtension(dccPath).ToLower().Equals(dccName))
            {
                return dccPath;
            }

            // clicked on the wrong application, try to see if we can still find
            // a dcc in this directory.
            var dccDir = new DirectoryInfo(Path.GetDirectoryName(dccPath));
            FileSystemInfo[] files = {};
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    files = dccDir.GetDirectories("*." + ext);
                    break;
                case RuntimePlatform.WindowsEditor:
                    files = dccDir.GetFiles("*." + ext);
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            string newDccPath = null;
            foreach (var file in files)
            {
                var filename = Path.GetFileNameWithoutExtension(file.Name).ToLower();
                if (filename.Equals(dccName))
                {
                    newDccPath = file.FullName.Replace("\\", "/");
                    break;
                }
            }
            return newDccPath;
        }

        [SettingsProvider]
        static SettingsProvider CreateFbxExportSettingsProvider()
        {
            ExportSettings.instance.name = "FBX Export Settings";
            ExportSettings.instance.Load();

            var provider = AssetSettingsProvider.CreateProviderFromObject(
                "Project/Fbx Export", ExportSettings.instance, GetSearchKeywordsFromGUIContentProperties(typeof(Style)));
#if UNITY_2019_1_OR_NEWER
            provider.inspectorUpdateHandler += () =>
            {
                if (provider.settingsEditor != null &&
                    provider.settingsEditor.serializedObject.UpdateIfRequiredOrScript())
                {
                    provider.Repaint();
                }
            };
#else
            provider.activateHandler += (searchContext, rootElement) =>
            {
                if (provider.settingsEditor != null &&
                    provider.settingsEditor.serializedObject.UpdateIfRequiredOrScript())
                {
                    provider.Repaint();
                }
            };
#endif // UNITY_2019_1_OR_NEWER
            return provider;
        }

        static IEnumerable<string> GetSearchKeywordsFromGUIContentProperties(Type type)
        {
            return type.GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(field => typeof(GUIContent).IsAssignableFrom(field.FieldType))
                .Select(field => ((GUIContent)field.GetValue(null)).text)
                .Concat(type.GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(prop => typeof(GUIContent).IsAssignableFrom(prop.PropertyType))
                    .Select(prop => ((GUIContent)prop.GetValue(null, null)).text))
                .Where(content => content != null)
                .Select(content => content.ToLowerInvariant())
                .Distinct();
        }
    }

    [FilePath("ProjectSettings/FbxExportSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ExportSettings : ScriptableObject
    {
        internal const string kDefaultSavePath = ".";
        private static List<string> s_PreferenceList = new List<string>() {kMayaOptionName, kMayaLtOptionName, kMaxOptionName};
        //Any additional names require a space after the name
        internal const string kMaxOptionName = "3ds Max ";
        internal const string kMayaOptionName = "Maya ";
        internal const string kMayaLtOptionName = "Maya LT";

        private static ExportSettings s_Instance;
        public static ExportSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = ScriptableObject.CreateInstance<ExportSettings>();
                    s_Instance.Load();
                    // don't save the export settings to the scene,
                    // otherwise they could be deleted on new scene, breaking
                    // the UI if the settings are being inspected.
                    s_Instance.hideFlags = HideFlags.DontSaveInEditor;
                }
                return s_Instance;
            }
        }

        internal ExportSettings()
        {
            if (s_Instance != null)
            {
                // The user has most likely created a preset.
            }
        }

        internal void SaveToFile()
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
            foreach (var attr in typeof(ExportSettings).GetCustomAttributes(true))
            {
                FilePathAttribute filePathAttribute = attr as FilePathAttribute;
                if (filePathAttribute != null)
                {
                    return filePathAttribute.filepath;
                }
            }
            return null;
        }

        // NOTE: using "Verbose" and "VerboseProperty" to handle backwards compatibility with older FbxExportSettings.asset files.
        //       The variable name is used when serializing, so changing the variable name would prevent older FbxExportSettings.asset files
        //       from loading this property.
        [SerializeField]
        private bool Verbose = false;
        internal bool VerboseProperty
        {
            get { return Verbose; }
            set { Verbose = value; }
        }

        private static string DefaultIntegrationSavePath
        {
            get
            {
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
            return NormalizePath(result, false);
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
                        result.Add(NormalizePath(location, false));
                    }
                }
            }
            return result;
        }

        private static HashSet<string> GetDefaultVendorLocations()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                HashSet<string> windowsDefaults = new HashSet<string>() { "C:/Program Files/Autodesk" };
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
        internal static List<string> DCCVendorLocations
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

                return result.ToList<string>();
            }
        }

        [SerializeField]
        private bool launchAfterInstallation = true;
        public bool LaunchAfterInstallation
        {
            get { return launchAfterInstallation; }
            set { launchAfterInstallation = value; }
        }

        [SerializeField]
        private bool HideSendToUnityMenu = true;
        public bool HideSendToUnityMenuProperty
        {
            get { return HideSendToUnityMenu; }
            set { HideSendToUnityMenu = value; }
        }

        [SerializeField]
        private bool BakeAnimation = false;
        internal bool BakeAnimationProperty
        {
            get { return BakeAnimation; }
            set { BakeAnimation = value; }
        }

        [SerializeField]
        private bool showConvertToPrefabDialog = true;
        public bool DisplayOptionsWindow
        {
            get { return showConvertToPrefabDialog; }
            set { showConvertToPrefabDialog = value; }
        }

        [SerializeField]
        private string integrationSavePath;
        internal static string IntegrationSavePath
        {
            get
            {
                //If the save path gets messed up and ends up not being valid, just use the project folder as the default
                if (string.IsNullOrEmpty(instance.integrationSavePath) ||
                    !Directory.Exists(instance.integrationSavePath))
                {
                    //The project folder, above the asset folder
                    instance.integrationSavePath = DefaultIntegrationSavePath;
                }
                return instance.integrationSavePath;
            }
            set
            {
                instance.integrationSavePath = value;
            }
        }

        [SerializeField]
        private int selectedDCCApp = 0;
        internal int SelectedDCCApp
        {
            get { return selectedDCCApp; }
            set { selectedDCCApp = value; }
        }

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
        private List<string> prefabSavePaths = new List<string>();
        internal List<string> GetCopyOfPrefabSavePaths()
        {
            return new List<string>(prefabSavePaths);
        }

        [SerializeField]
        private List<string> fbxSavePaths = new List<string>();
        internal List<String> GetCopyOfFbxSavePaths()
        {
            return new List<string>(fbxSavePaths);
        }

        [SerializeField]
        private int selectedFbxPath = 0;
        public int SelectedFbxPath
        {
            get { return selectedFbxPath; }
            set { selectedFbxPath = value; }
        }

        [SerializeField]
        private int selectedPrefabPath = 0;
        public int SelectedPrefabPath
        {
            get { return selectedPrefabPath; }
            set { selectedPrefabPath = value; }
        }

        private int maxStoredSavePaths = 5;

        // List of names in order that they appear in option list
        [SerializeField]
        private List<string> dccOptionNames = new List<string>();
        // List of paths in order that they appear in the option list
        [SerializeField]
        private List<string> dccOptionPaths;

        // don't serialize as ScriptableObject does not get properly serialized on export
        [System.NonSerialized]
        private ExportModelSettings m_exportModelSettings;
        internal ExportModelSettings ExportModelSettings
        {
            get
            {
                if (!m_exportModelSettings)
                {
                    m_exportModelSettings = ScriptableObject.CreateInstance(typeof(ExportModelSettings)) as ExportModelSettings;
                }
                if (this.exportModelSettingsSerialize != null)
                {
                    m_exportModelSettings.info = this.exportModelSettingsSerialize;
                }
                else
                {
                    this.exportModelSettingsSerialize = m_exportModelSettings.info;
                }
                return m_exportModelSettings;
            }
            set { m_exportModelSettings = value; }
        }

        // store contents of export model settings for serialization
        [SerializeField]
        private ExportModelSettingsSerialize exportModelSettingsSerialize;

        [System.NonSerialized]
        private ConvertToPrefabSettings m_convertToPrefabSettings;
        internal ConvertToPrefabSettings ConvertToPrefabSettings
        {
            get
            {
                if (!m_convertToPrefabSettings)
                {
                    m_convertToPrefabSettings = ScriptableObject.CreateInstance(typeof(ConvertToPrefabSettings)) as ConvertToPrefabSettings;
                }
                if (this.convertToPrefabSettingsSerialize != null)
                {
                    m_convertToPrefabSettings.info = this.convertToPrefabSettingsSerialize;
                }
                else
                {
                    this.convertToPrefabSettingsSerialize = m_convertToPrefabSettings.info;
                }
                return m_convertToPrefabSettings;
            }
            set { m_convertToPrefabSettings = value; }
        }

        [SerializeField]
        private ConvertToPrefabSettingsSerialize convertToPrefabSettingsSerialize;

        internal void LoadDefaults()
        {
            LaunchAfterInstallation = true;
            HideSendToUnityMenuProperty = true;
            prefabSavePaths = new List<string>(){ kDefaultSavePath };
            fbxSavePaths = new List<string>(){ kDefaultSavePath };
            integrationSavePath = DefaultIntegrationSavePath;
            dccOptionPaths = new List<string>();
            dccOptionNames = new List<string>();
            BakeAnimationProperty = false;
            exportModelSettingsSerialize = new ExportModelSettingsSerialize();
            ExportModelSettings.info = exportModelSettingsSerialize;
            DisplayOptionsWindow = true;
            convertToPrefabSettingsSerialize = new ConvertToPrefabSettingsSerialize();
            ConvertToPrefabSettings.info = convertToPrefabSettingsSerialize;
        }

        /// <summary>
        /// Increments the name if there is a duplicate in dccAppOptions.
        /// </summary>
        /// <returns>The unique name.</returns>
        /// <param name="name">Name.</param>
        internal static string GetUniqueDCCOptionName(string name)
        {
            Debug.Assert(instance != null);
            if (name == null)
            {
                return null;
            }
            if (!instance.dccOptionNames.Contains(name))
            {
                return name;
            }
            var format = "{1} ({0})";
            int index = 1;
            // try extracting the current index from the name and incrementing it
            var result = System.Text.RegularExpressions.Regex.Match(name, @"\((?<number>\d+?)\)$");
            if (result != null)
            {
                var number = result.Groups["number"].Value;
                int tempIndex;
                if (int.TryParse(number, out tempIndex))
                {
                    var indexOfNumber = name.LastIndexOf(number);
                    format = name.Remove(indexOfNumber, number.Length).Insert(indexOfNumber, "{0}");
                    index = tempIndex + 1;
                }
            }

            string uniqueName = null;
            do
            {
                uniqueName = string.Format(format, index, name);
                index++;
            }
            while (instance.dccOptionNames.Contains(uniqueName));

            return uniqueName;
        }

        internal void SetDCCOptionNames(List<string> newList)
        {
            dccOptionNames = newList;
        }

        internal void SetDCCOptionPaths(List<string> newList)
        {
            dccOptionPaths = newList;
        }

        internal void ClearDCCOptionNames()
        {
            dccOptionNames.Clear();
        }

        internal void ClearDCCOptions()
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
        internal int PreferredDCCApp
        {
            get
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
                        if (dccOptionNames[i] == "MAYA_LOCATION")
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
        internal static string RemoveSpacesAndNumbers(string s)
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
        private static void FindDCCInstalls()
        {
            var dccOptionNames = instance.dccOptionNames;
            var dccOptionPaths = instance.dccOptionPaths;

            // find dcc installation from vendor locations
            for (int i = 0; i < DCCVendorLocations.Count; i++)
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
                    if (product.StartsWith("maya", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string version = product.Substring("maya".Length);
                        dccOptionPaths.Add(GetMayaExePathFromLocation(productDir.FullName.Replace("\\", "/")));
                        dccOptionNames.Add(GetUniqueDCCOptionName(kMayaOptionName + version));
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

            instance.SelectedDCCApp = instance.PreferredDCCApp;
        }

        /// <summary>
        /// Returns the first valid folder in our list of vendor locations
        /// </summary>
        /// <returns>The first valid vendor location</returns>
        internal static string FirstValidVendorLocation
        {
            get
            {
                List<string> locations = DCCVendorLocations;
                for (int i = 0; i < locations.Count; i++)
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
        }

        /// <summary>
        /// Gets the maya exe at Maya install location.
        /// </summary>
        /// <returns>The maya exe path.</returns>
        /// <param name="location">Location of Maya install.</param>
        private static string GetMayaExePathFromLocation(string location)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return location + "/bin/maya.exe";
                case RuntimePlatform.OSXEditor:
                    // MAYA_LOCATION on mac is set by Autodesk to be the
                    // Contents directory. But let's make it easier on people
                    // and allow just having it be the app bundle or a
                    // directory that holds the app bundle.
                    if (location.EndsWith(".app/Contents"))
                    {
                        return location + "/MacOS/Maya";
                    }
                    else if (location.EndsWith(".app"))
                    {
                        return location + "/Contents/MacOS/Maya";
                    }
                    else
                    {
                        return location + "/Maya.app/Contents/MacOS/Maya";
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        internal static GUIContent[] GetDCCOptions()
        {
            if (instance.dccOptionNames == null ||
                instance.dccOptionNames.Count != instance.dccOptionPaths.Count ||
                instance.dccOptionNames.Count == 0)
            {
                instance.dccOptionPaths = new List<string>();
                instance.dccOptionNames = new List<string>();
                FindDCCInstalls();
            }
            // store the selected app if any
            string prevSelection = SelectedDCCPath;

            // remove options that no longer exist
            List<string> pathsToDelete = new List<string>();
            List<string> namesToDelete = new List<string>();
            for (int i = 0; i < instance.dccOptionPaths.Count; i++)
            {
                var dccPath = instance.dccOptionPaths[i];
                if (!File.Exists(dccPath))
                {
                    namesToDelete.Add(instance.dccOptionNames[i]);
                    pathsToDelete.Add(dccPath);
                }
            }
            foreach (var str in pathsToDelete)
            {
                instance.dccOptionPaths.Remove(str);
            }
            foreach (var str in namesToDelete)
            {
                instance.dccOptionNames.Remove(str);
            }

            // set the selected DCC app to the previous selection
            instance.SelectedDCCApp = instance.dccOptionPaths.IndexOf(prevSelection);
            if (instance.SelectedDCCApp < 0)
            {
                // find preferred app if previous selection no longer exists
                instance.SelectedDCCApp = instance.PreferredDCCApp;
            }

            if (instance.dccOptionPaths.Count <= 0)
            {
                instance.SelectedDCCApp = 0;
                return new GUIContent[]
                {
                    new GUIContent("<No 3D Application found>")
                };
            }

            GUIContent[] optionArray = new GUIContent[instance.dccOptionPaths.Count];
            for (int i = 0; i < instance.dccOptionPaths.Count; i++)
            {
                optionArray[i] = new GUIContent(
                    instance.dccOptionNames[i],
                    instance.dccOptionPaths[i]
                );
            }
            return optionArray;
        }

        internal enum DCCType { Maya, Max };

        internal static void AddDCCOption(string newOption, DCCType dcc)
        {
            if (Application.platform == RuntimePlatform.OSXEditor && dcc == DCCType.Maya)
            {
                // on OSX we get a path ending in .app, which is not quite the exe
                newOption = GetMayaExePathFromLocation(newOption);
            }

            var dccOptionPaths = instance.dccOptionPaths;
            if (dccOptionPaths.Contains(newOption))
            {
                instance.SelectedDCCApp = dccOptionPaths.IndexOf(newOption);
                return;
            }

            string optionName = "";
            switch (dcc)
            {
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
                    optionName = GetMaxOptionName(newOption);
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

            instance.dccOptionNames.Add(optionName);
            dccOptionPaths.Add(newOption);
            instance.SelectedDCCApp = dccOptionPaths.Count - 1;
        }

        /// <summary>
        /// Ask the version number by running maya.
        /// </summary>
        internal static string AskMayaVersion(string exePath)
        {
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
        internal static string GetMaxOptionName(string exePath)
        {
            return GetUniqueDCCOptionName(Path.GetFileName(Path.GetDirectoryName(exePath)));
        }

        internal static bool IsEarlierThanMax2017(string AppName)
        {
            int version = FindDCCVersion(AppName);
            return version != -1 && version < 2017;
        }

        internal static string SelectedDCCPath
        {
            get
            {
                return (instance.dccOptionPaths.Count > 0 &&
                    instance.SelectedDCCApp >= 0 &&
                    instance.SelectedDCCApp < instance.dccOptionPaths.Count) ? instance.dccOptionPaths[instance.SelectedDCCApp] : "";
            }
        }

        internal static string SelectedDCCName
        {
            get
            {
                return (instance.dccOptionNames.Count > 0 &&
                    instance.SelectedDCCApp >= 0 &&
                    instance.SelectedDCCApp < instance.dccOptionNames.Count) ? instance.dccOptionNames[instance.SelectedDCCApp] : "";
            }
        }

        internal static bool CanInstall()
        {
            return instance.dccOptionPaths.Count > 0;
        }

        internal static string GetProjectRelativePath(string fullPath)
        {
            var assetRelativePath = UnityEditor.Formats.Fbx.Exporter.ExportSettings.ConvertToAssetRelativePath(fullPath);
            var projectRelativePath = "Assets/" + assetRelativePath;
            if (string.IsNullOrEmpty(assetRelativePath))
            {
                throw new FbxExportSettingsException("Path " + fullPath + " must be in the Assets folder.");
            }
            return projectRelativePath;
        }

        /// <summary>
        /// The relative save paths for given absolute paths.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// </summary>
        internal static string[] GetRelativeSavePaths(List<string> exportSavePaths)
        {
            if (exportSavePaths == null)
            {
                return null;
            }

            if (exportSavePaths.Count == 0)
            {
                exportSavePaths.Add(kDefaultSavePath);
            }
            string[] relSavePaths = new string[exportSavePaths.Count];
            // use special forward slash unicode char as "/" is a special character
            // that affects the dropdown layout.
            string forwardslash = " \u2044 ";
            for (int i = 0; i < relSavePaths.Length; i++)
            {
                relSavePaths[i] = string.Format("Assets{0}{1}", forwardslash, exportSavePaths[i] == "." ? "" : NormalizePath(exportSavePaths[i], isRelative: true).Replace("/", forwardslash));
            }
            return relSavePaths;
        }

        /// <summary>
        /// Returns the paths for display in the menu.
        /// Paths inside the Assets folder are relative, while those outside are kept absolute.
        /// </summary>
        internal static string[] GetMixedSavePaths(List<string> exportSavePaths)
        {
            string[] displayPaths = new string[exportSavePaths.Count];
            string forwardslash = " \u2044 ";
            for (int i = 0; i < displayPaths.Length; i++)
            {
                // if path is in Assets folder, shorten it
                if (!Path.IsPathRooted(exportSavePaths[i]))
                {
                    displayPaths[i] = string.Format("Assets{0}{1}", forwardslash, exportSavePaths[i] == "." ? "" : NormalizePath(exportSavePaths[i], isRelative: true).Replace("/", forwardslash));
                }
                else
                {
                    displayPaths[i] = exportSavePaths[i].Replace("/", forwardslash);
                }
            }

            return displayPaths;
        }

        /// <summary>
        /// The path where Export model will save the new fbx.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// Only returns the paths within the Assets folder of the project.
        /// </summary>
        internal static string[] GetRelativeFbxSavePaths()
        {
            return GetRelativeFbxSavePaths(instance.fbxSavePaths, ref instance.selectedFbxPath);
        }

        /// <summary>
        /// The path where Export model will save the new fbx.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// Only returns the paths within the Assets folder of the project.
        /// </summary>
        internal static string[] GetRelativeFbxSavePaths(List<string> fbxSavePaths, ref int pathIndex)
        {
            // sort the list of paths, putting project paths first
            fbxSavePaths.Sort((x, y) => Path.IsPathRooted(x).CompareTo(Path.IsPathRooted(y)));
            var relPathCount = fbxSavePaths.FindAll(x => !Path.IsPathRooted(x)).Count;

            // reset selected path if it's out of range
            if (pathIndex > relPathCount - 1)
            {
                pathIndex = 0;
            }

            return GetRelativeSavePaths(fbxSavePaths.GetRange(0, relPathCount));
        }

        /// <summary>
        /// The path where Convert to Prefab will save the new prefab.
        /// This is relative to the Application.dataPath ; it uses '/' as the
        /// separator on all platforms.
        /// </summary>
        internal static string[] GetRelativePrefabSavePaths()
        {
            return GetRelativeSavePaths(instance.prefabSavePaths);
        }

        /// <summary>
        /// The paths formatted for display in the menu.
        /// Paths outside the Assets folder are kept as they are and ones inside are shortened.
        /// </summary>
        internal static string[] GetMixedFbxSavePaths()
        {
            return GetMixedSavePaths(instance.fbxSavePaths);
        }

        /// <summary>
        /// Adds the save path to given save path list.
        /// </summary>
        /// <param name="savePath">Save path.</param>
        /// <param name="exportSavePaths">Export save paths.</param>
        internal static void AddSavePath(string savePath, List<string> exportSavePaths, bool exportOutsideProject = false)
        {
            if (exportSavePaths == null)
            {
                return;
            }

            if (exportOutsideProject)
            {
                savePath = NormalizePath(savePath, isRelative: false);
            }
            else
            {
                savePath = NormalizePath(savePath, isRelative: true);
            }

            if (exportSavePaths.Contains(savePath))
            {
                // move to first place if it isn't already
                if (exportSavePaths[0] == savePath)
                {
                    return;
                }
                exportSavePaths.Remove(savePath);
            }

            if (exportSavePaths.Count >= instance.maxStoredSavePaths)
            {
                // remove last used path
                exportSavePaths.RemoveAt(exportSavePaths.Count - 1);
            }

            exportSavePaths.Insert(0, savePath);
        }

        internal static void AddFbxSavePath(string savePath, bool exportOutsideProject = false)
        {
            AddSavePath(savePath, instance.fbxSavePaths, exportOutsideProject);
            instance.SelectedFbxPath = 0;
        }

        internal static void AddPrefabSavePath(string savePath)
        {
            AddSavePath(savePath, instance.prefabSavePaths);
            instance.SelectedPrefabPath = 0;
        }

        internal static string GetAbsoluteSavePath(string savePath)
        {
            var projectAbsolutePath = Path.Combine(Application.dataPath, savePath);
            projectAbsolutePath = NormalizePath(projectAbsolutePath, isRelative: false, separator: Path.DirectorySeparatorChar);

            // if path is outside Assets folder, it's already absolute so return the original path
            if (string.IsNullOrEmpty(ExportSettings.ConvertToAssetRelativePath(projectAbsolutePath)))
            {
                return savePath;
            }

            return projectAbsolutePath;
        }

        internal static string FbxAbsoluteSavePath
        {
            get
            {
                if (instance.fbxSavePaths.Count <= 0)
                {
                    instance.fbxSavePaths.Add(kDefaultSavePath);
                }
                return GetAbsoluteSavePath(instance.fbxSavePaths[instance.SelectedFbxPath]);
            }
        }

        internal static string PrefabAbsoluteSavePath
        {
            get
            {
                if (instance.prefabSavePaths.Count <= 0)
                {
                    instance.prefabSavePaths.Add(kDefaultSavePath);
                }
                return GetAbsoluteSavePath(instance.prefabSavePaths[instance.SelectedPrefabPath]);
            }
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
        internal static string ConvertToAssetRelativePath(string fullPathInAssets, bool requireSubdirectory = true)
        {
            if (!Path.IsPathRooted(fullPathInAssets))
            {
                fullPathInAssets = Path.GetFullPath(fullPathInAssets);
            }
            var relativePath = GetRelativePath(Application.dataPath, fullPathInAssets);
            if (requireSubdirectory && relativePath.StartsWith(".."))
            {
                if (relativePath.Length == 2 || relativePath[2] == '/')
                {
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
        internal static string GetRelativePath(string fromDir, string toDir,
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
            for (int i = 0, n = System.Math.Min(fromDirs.Length, toDirs.Length); i < n; ++i)
            {
                if (fromDirs[i] != toDirs[i]) { break; }
                lca = i;
            }

            // Step up from the fromDir to the lca, then down from lca to the toDir.
            // If from = /a/b/c/d
            // and to  = /a/b/e/f/g
            // Then we need to go up 2 and down 3.
            var nStepsUp = (fromDirs.Length - 1) - lca;
            var nStepsDown = (toDirs.Length - 1) - lca;
            if (nStepsUp + nStepsDown == 0)
            {
                return ".";
            }

            var relDirs = new string[nStepsUp + nStepsDown];
            for (int i = 0; i < nStepsUp; ++i)
            {
                relDirs[i] = "..";
            }
            for (int i = 0; i < nStepsDown; ++i)
            {
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
        internal static string NormalizePath(string path, bool isRelative,
            char separator = '/')
        {
            if (path == null)
            {
                return null;
            }

            // Use slashes to simplify the code (we're going to clobber them all anyway).
            path = path.Replace('\\', '/');

            // If we're supposed to be an absolute path, but we're actually a
            // relative path, ignore the 'isRelative' flag.
            if (!isRelative && !Path.IsPathRooted(path))
            {
                isRelative = true;
            }

            // Build up a list of directory items.
            var dirs = path.Split('/');

            // Modify dirs in-place, reading from readIndex and remembering
            // what index we've written to.
            int lastWriteIndex = -1;
            for (int readIndex = 0, n = dirs.Length; readIndex < n; ++readIndex)
            {
                var dir = dirs[readIndex];

                // Skip duplicate path separators.
                if (string.IsNullOrEmpty(dir))
                {
                    // Skip if it's not a leading path separator.
                    if (lastWriteIndex >= 0)
                    {
                        continue;
                    }

                    // Also skip if it's leading and we have a relative path.
                    if (isRelative)
                    {
                        continue;
                    }
                }

                // Skip '.'
                if (dir == ".")
                {
                    continue;
                }

                // Erase the previous directory we read on '..'.
                // Exception: we can start with '..'
                // Exception: we can have multiple '..' in a row.
                //
                // Note: this ignores the actual file system and the funny
                // results you see when there are symlinks.
                if (dir == "..")
                {
                    if (lastWriteIndex == -1)
                    {
                        // Leading '..' => handle like a normal directory.
                    }
                    else if (dirs[lastWriteIndex] == "..")
                    {
                        // Multiple ".." => handle like a normal directory.
                    }
                    else
                    {
                        // Usual case: delete the previous directory.
                        lastWriteIndex--;
                        continue;
                    }
                }

                // Copy anything else to the next index.
                ++lastWriteIndex;
                dirs[lastWriteIndex] = dirs[readIndex];
            }

            if (lastWriteIndex == -1 || (lastWriteIndex == 0 && string.IsNullOrEmpty(dirs[lastWriteIndex])))
            {
                // If we didn't keep anything, we have the empty path.
                // For an absolute path that's / ; for a relative path it's .
                if (isRelative)
                {
                    return ".";
                }
                else
                {
                    return "" + separator;
                }
            }
            else
            {
                // Otherwise print out the path with the proper separator.
                return String.Join("" + separator, dirs, 0, lastWriteIndex + 1);
            }
        }

        internal void Load()
        {
            string filePath = GetFilePath();
            if (!System.IO.File.Exists(filePath))
            {
                LoadDefaults();
            }
            else
            {
                try
                {
                    var fileData = System.IO.File.ReadAllText(filePath);
                    EditorJsonUtility.FromJsonOverwrite(fileData, s_Instance);
                }
                catch (Exception xcp)
                {
                    // Quash the exception and take the default settings.
                    Debug.LogException(xcp);
                    LoadDefaults();
                }
            }
        }

        internal void Save()
        {
            exportModelSettingsSerialize = ExportModelSettings.info;
            convertToPrefabSettingsSerialize = ConvertToPrefabSettings.info;
            this.SaveToFile();
        }

        // Called on creation and whenever the reset button is clicked
        internal void Reset()
        {
            // apply default settings
            LoadDefaults();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class FilePathAttribute : Attribute
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
