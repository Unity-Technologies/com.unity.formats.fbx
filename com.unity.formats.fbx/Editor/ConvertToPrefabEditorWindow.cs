using System.Collections.Generic;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Presets;
#endif
using System.Linq;
using System.Security.Permissions;

namespace UnityEditor.Formats.Fbx.Exporter
{
    internal class ConvertToPrefabEditorWindow : ExportOptionsEditorWindow
    {
        protected override GUIContent WindowTitle { get { return new GUIContent("Convert Options"); } }
        protected override float MinWindowHeight { get { return 350; } } // determined by trial and error
        protected override string ExportButtonName { get { return "Convert"; } }
        private string m_prefabFileName = "";

        private float m_prefabExtLabelWidth;

        protected override bool DisableNameSelection
        {
            get
            {
                return (ToExport != null && ToExport.Length > 1);
            }
        }
        protected override bool DisableTransferAnim
        {
            get
            {
                return ToExport == null || ToExport.Length > 1;
            }
        }

        public static ConvertToPrefabEditorWindow Init(IEnumerable<GameObject> toConvert)
        {
            ConvertToPrefabEditorWindow window = CreateWindow<ConvertToPrefabEditorWindow>();
            window.InitializeWindow();
            window.SetGameObjectsToConvert(toConvert);
            window.Show();
            return window;
        }

        protected void SetGameObjectsToConvert(IEnumerable<GameObject> toConvert)
        {
            ToExport = toConvert.OrderBy(go => go.name).ToArray();

            TransferAnimationSource = null;
            TransferAnimationDest = null;

            string fbxFileName = null;
            if (ToExport.Length == 1)
            {
                var go = ModelExporter.GetGameObject(ToExport[0]);
                // check if the GameObject is a model instance, use as default filename and path if it is
                GameObject mainAsset = ConvertToNestedPrefab.GetFbxAssetOrNull(go);
                if (!mainAsset)
                {
                    // Use the game object's name
                    m_prefabFileName = go.name;
                }
                else
                {
                    // Use the asset's name
                    var mainAssetRelPath = AssetDatabase.GetAssetPath(mainAsset);
                    // remove Assets/ from beginning of path
                    mainAssetRelPath = mainAssetRelPath.Substring("Assets".Length);

                    m_prefabFileName = System.IO.Path.GetFileNameWithoutExtension(mainAssetRelPath);
                    ExportSettings.AddFbxSavePath(System.IO.Path.GetDirectoryName(mainAssetRelPath));

                    fbxFileName = m_prefabFileName;
                }

                var fullPrefabPath = System.IO.Path.Combine(ExportSettings.PrefabAbsoluteSavePath, m_prefabFileName + ".prefab");
                if (System.IO.File.Exists(fullPrefabPath))
                {
                    m_prefabFileName = System.IO.Path.GetFileNameWithoutExtension(ConvertToNestedPrefab.IncrementFileName(ExportSettings.PrefabAbsoluteSavePath, m_prefabFileName + ".prefab"));
                }

                // if only one object selected, set transfer source/dest to this object
                if (go)
                {
                    TransferAnimationSource = go.transform;
                    TransferAnimationDest = go.transform;
                }
            }
            else if (ToExport.Length > 1)
            {
                m_prefabFileName = "(automatic)";
            }

            // if there is an existing fbx file then use its name, otherwise use the same name as for the prefab
            this.SetFilename(fbxFileName != null? fbxFileName : m_prefabFileName);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!InnerEditor)
            {
                InnerEditor = UnityEditor.Editor.CreateEditor(ConvertToPrefabSettingsInstance);
            }
            m_prefabExtLabelWidth = FbxExtLabelStyle.CalcSize(new GUIContent(".prefab")).x;
        }

        /// <summary>
        /// Return the number of objects in the selection that contain RectTransforms.
        /// </summary>
        protected int GetUIElementsInExportSetCount()
        {
            int count = 0;
            foreach (var obj in ToExport)
            {
                var go = ModelExporter.GetGameObject(obj);
                var rectTransforms = go.GetComponentsInChildren<RectTransform>();
                count += rectTransforms.Length;
            }
            return count;
        }

        protected bool ExportSetContainsAnimation()
        {
            foreach (var obj in ToExport)
            {
                var go = ModelExporter.GetGameObject(obj);
                if (go.GetComponentInChildren<Animation>() || go.GetComponentInChildren<Animator>())
                {
                    return true;
                }
            }
            return false;
        }

        [SecurityPermission(SecurityAction.LinkDemand)]
        protected override bool Export()
        {
            if (string.IsNullOrEmpty(ExportFileName))
            {
                Debug.LogError("FbxExporter: Please specify an fbx filename");
                return false;
            }

            if (string.IsNullOrEmpty(m_prefabFileName))
            {
                Debug.LogError("FbxExporter: Please specify a prefab filename");
                return false;
            }

            var fbxDirPath = ExportSettings.GetAbsoluteSavePath(FbxSavePaths[SelectedFbxPath]); ;
            var fbxPath = System.IO.Path.Combine(fbxDirPath, ExportFileName + ".fbx");

            var prefabDirPath = ExportSettings.GetAbsoluteSavePath(PrefabSavePaths[SelectedPrefabPath]);
            var prefabPath = System.IO.Path.Combine(prefabDirPath, m_prefabFileName + ".prefab");

            if (ToExport == null)
            {
                Debug.LogError("FbxExporter: missing object for conversion");
                return false;
            }

            int rectTransformCount = GetUIElementsInExportSetCount();
            if (rectTransformCount > 0)
            {
                // Warn that UI elements will break if converted
                string warning = string.Format("Warning: UI Components (ie, RectTransform) are not saved when converting to FBX.\n{0} item(s) in the selection will lose their UI components.",
                    rectTransformCount);
                bool result = UnityEditor.EditorUtility.DisplayDialog(
                    string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), warning, "Convert and Lose UI", "Cancel");

                if (!result)
                {
                    return false;
                }
            }

            if (SettingsObject.UseMayaCompatibleNames && SettingsObject.AllowSceneModification)
            {
                string warning = "Names of objects in the hierarchy may change with the Compatible Naming option turned on";
                if (ExportSetContainsAnimation())
                {
                    warning = "Compatible Naming option turned on. Names of objects in hierarchy may change and break animations.";
                }

                // give a warning dialog that indicates that names in the scene may change
                int result = UnityEditor.EditorUtility.DisplayDialogComplex(
                                string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), warning, "OK", "Turn off and continue", "Cancel"
                            );
                if (result == 1)
                {
                    // turn compatible naming off
                    SettingsObject.SetUseMayaCompatibleNames(false);
                }
                else if (result == 2)
                {
                    return false;
                }
            }

            if (ToExport.Length == 1)
            {
                var go = ModelExporter.GetGameObject(ToExport[0]);

                // Check if we'll be clobbering files. If so, warn the user
                // first and let them cancel out.
                if (!OverwriteExistingFile(prefabPath))
                {
                    return false;
                }

                if (ConvertToNestedPrefab.WillExportFbx(go))
                {
                    if (!OverwriteExistingFile(fbxPath))
                    {
                        return false;
                    }
                }

                ConvertToNestedPrefab.Convert(
                    go, fbxFullPath: fbxPath, prefabFullPath: prefabPath, exportOptions: ConvertToPrefabSettingsInstance.info
                );
                return true;
            }

            bool onlyPrefabAssets = ConvertToNestedPrefab.SetContainsOnlyPrefabAssets(ToExport);
            int groupIndex = -1;
            // no need to undo if we aren't converting anything that's in the scene
            if (!onlyPrefabAssets)
            {
                Undo.IncrementCurrentGroup();
                groupIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(ConvertToNestedPrefab.UndoConversionCreateObject);
            }
            foreach (var obj in ToExport)
            {
                // Convert, automatically choosing a file path that won't clobber any existing files.
                var go = ModelExporter.GetGameObject(obj);
                ConvertToNestedPrefab.Convert(
                    go, fbxDirectoryFullPath: fbxDirPath, prefabDirectoryFullPath: prefabDirPath, exportOptions: ConvertToPrefabSettingsInstance.info
                );
            }
            if (!onlyPrefabAssets && groupIndex >= 0)
            {
                Undo.CollapseUndoOperations(groupIndex);
                Undo.IncrementCurrentGroup();
            }
            return true;
        }
        
        public const string k_SessionStoragePrefix = "FbxExporterConvertOptions_{0}";
        protected override string SessionStoragePrefix { get { return k_SessionStoragePrefix; } }

        public override void ResetSessionSettings(string defaultSettings = null)
        {
            base.ResetSessionSettings(defaultSettings);

            // save the source and dest as these are not serialized
            var source = m_convertToPrefabSettingsInstance.info.AnimationSource;
            var dest = m_convertToPrefabSettingsInstance.info.AnimationDest;

            m_convertToPrefabSettingsInstance = null;
            ConvertToPrefabSettingsInstance.info.SetAnimationSource(source);
            ConvertToPrefabSettingsInstance.info.SetAnimationDest(dest);
            
            m_prefabSavePaths = null;
            SelectedPrefabPath = 0;
            InnerEditor = Editor.CreateEditor(ConvertToPrefabSettingsInstance);
        }

        protected override void StoreSettingsInSession()
        {
            base.StoreSettingsInSession();

            // store Prefab Save Paths
            StorePathsInSession(k_SessionPrefabPathsName, m_prefabSavePaths);
            SessionState.SetInt(string.Format(SessionStoragePrefix, k_SessionSelectedPrefabPathName), SelectedPrefabPath);
        }


        private List<string> m_prefabSavePaths;
        internal List<string> PrefabSavePaths
        {
            get
            {
                if (m_prefabSavePaths == null)
                {
                    // Try to restore from session, fall back to Fbx Export Settings
                    RestorePathsFromSession(k_SessionPrefabPathsName, ExportSettings.instance.GetCopyOfPrefabSavePaths(), out m_prefabSavePaths);
                    SelectedPrefabPath = SessionState.GetInt(string.Format(SessionStoragePrefix, k_SessionSelectedPrefabPathName), ExportSettings.instance.SelectedPrefabPath);
                }
                return m_prefabSavePaths;
            }
        }

        [SerializeField]
        private int m_selectedPrefabPath = 0;
        internal int SelectedPrefabPath
        {
            get { return m_selectedPrefabPath; }
            set { m_selectedPrefabPath = value; }
        }

        private ConvertToPrefabSettings m_convertToPrefabSettingsInstance;
        public ConvertToPrefabSettings ConvertToPrefabSettingsInstance
        {
            get
            {
                if (m_convertToPrefabSettingsInstance == null)
                {
                    // make a copy of the settings
                    m_convertToPrefabSettingsInstance = ScriptableObject.CreateInstance(typeof(ConvertToPrefabSettings)) as ConvertToPrefabSettings;
                    // load settings stored in Unity session, default to DefaultPreset, if none then Export Settings
                    var defaultPresets = Preset.GetDefaultPresetsForObject(m_convertToPrefabSettingsInstance);
                    if (defaultPresets.Length <= 0)
                    {
                        RestoreSettingsFromSession(ExportSettings.instance.ConvertToPrefabSettings.info);
                    }
                    else
                    {
                        // apply the first default preset
                        // TODO: figure out what it means to have multiple default presets, when would they be applied?
                        defaultPresets[0].ApplyTo(m_convertToPrefabSettingsInstance);
                        RestoreSettingsFromSession(m_convertToPrefabSettingsInstance.info);
                    }
                }
                return m_convertToPrefabSettingsInstance;
            }
        }

        public override void SaveExportSettings()
        {
            // check if the settings are different from what is in the Project Settings and only store
            // if they are. Otherwise we want to keep them updated with changes to the Project Settings.
            bool settingsChanged = !(ConvertToPrefabSettingsInstance.Equals(ExportSettings.instance.ConvertToPrefabSettings));

            var projectSettingsFbxPaths = ExportSettings.instance.GetCopyOfFbxSavePaths();
            settingsChanged |= !projectSettingsFbxPaths.SequenceEqual(FbxSavePaths);
            var projectSettingsPrefabPaths = ExportSettings.instance.GetCopyOfPrefabSavePaths();
            settingsChanged |= !projectSettingsPrefabPaths.SequenceEqual(PrefabSavePaths);

            settingsChanged |= SelectedPrefabPath != ExportSettings.instance.SelectedPrefabPath;
            settingsChanged |= SelectedFbxPath != ExportSettings.instance.SelectedFbxPath;

            if (settingsChanged)
            {
                StoreSettingsInSession();
            }
        }

        protected override ExportOptionsSettingsSerializeBase SettingsObject
        {
            get { return ConvertToPrefabSettingsInstance.info; }
        }
#if UNITY_2018_1_OR_NEWER
        protected override void ShowPresetReceiver()
        {
            ShowPresetReceiver(ConvertToPrefabSettingsInstance);
        }
#endif
        protected override void CreateCustomUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(
                "Prefab Name",
                "Filename to save prefab to."), GUILayout.Width(LabelWidth - TextFieldAlignOffset));

            EditorGUI.BeginDisabledGroup(DisableNameSelection);
            // Show the export name with an uneditable ".prefab" at the end
            //-------------------------------------
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUI.indentLevel--;
            // continually resize to contents
            var textFieldSize = NameTextFieldStyle.CalcSize(new GUIContent(m_prefabFileName));
            m_prefabFileName = EditorGUILayout.TextField(m_prefabFileName, NameTextFieldStyle, GUILayout.Width(textFieldSize.x + 5), GUILayout.MinWidth(5));
            m_prefabFileName = ModelExporter.ConvertToValidFilename(m_prefabFileName);

            EditorGUILayout.LabelField("<color=#808080ff>.prefab</color>", FbxExtLabelStyle, GUILayout.Width(m_prefabExtLabelWidth));
            EditorGUI.indentLevel++;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            //-----------------------------------
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(
                "Prefab Path",
                "Relative path for saving FBX Prefab Variants."), GUILayout.Width(LabelWidth - FieldOffset));

            var pathLabels = ExportSettings.GetRelativeSavePaths(PrefabSavePaths);

            SelectedPrefabPath = EditorGUILayout.Popup(SelectedPrefabPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

            if (GUILayout.Button(new GUIContent("...", "Browse to a new location to save prefab to"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
            {
                string initialPath = Application.dataPath;

                string fullPath = EditorUtility.SaveFolderPanel(
                    "Select FBX Prefab Variant Save Path", initialPath, null
                );

                // Unless the user canceled, make sure they chose something in the Assets folder.
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var relativePath = ExportSettings.ConvertToAssetRelativePath(fullPath);
                    if (string.IsNullOrEmpty(relativePath))
                    {
                        Debug.LogWarning("Please select a location in the Assets folder");
                    }
                    else
                    {
                        ExportSettings.AddSavePath(relativePath, PrefabSavePaths);
                        SelectedPrefabPath = 0;

                        // Make sure focus is removed from the selectable label
                        // otherwise it won't update
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}