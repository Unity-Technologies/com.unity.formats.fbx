using System.Collections.Generic;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Presets;
#endif
using System.Linq;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.Timeline;

namespace UnityEditor.Formats.Fbx.Exporter
{
    internal abstract class ExportOptionsEditorWindow : EditorWindow
    {
        internal const string DefaultWindowTitle = "Export Options";
        protected const float SelectableLabelMinWidth = 120;
        protected const float BrowseButtonWidth = 25;
        protected const float LabelWidth = 175;
        protected const float FieldOffset = 18;
        protected const float TextFieldAlignOffset = 3;
        protected const float ExportButtonWidth = 100;
        protected const float FbxExtOffset = -7;
        protected virtual float MinWindowHeight { get { return 300; } }

        protected virtual string ExportButtonName { get { return "Export"; } }

        protected virtual GUIContent WindowTitle { get { return new GUIContent(DefaultWindowTitle); } }

        private string m_exportFileName = "";
        protected string ExportFileName
        {
            get { return m_exportFileName; }
            set { m_exportFileName = value; }
        }

        private UnityEditor.Editor m_innerEditor;
        protected UnityEditor.Editor InnerEditor
        {
            get { return m_innerEditor; }
            set { m_innerEditor = value; }
        }
#if UNITY_2018_1_OR_NEWER && !UNITY_2023_1_OR_NEWER
        private FbxExportPresetSelectorReceiver m_receiver;
        protected FbxExportPresetSelectorReceiver Receiver
        {
            get { return m_receiver; }
            set { m_receiver = value; }
        }
#endif
        private static GUIContent presetIcon { get { return EditorGUIUtility.IconContent("Preset.Context"); } }
        private static GUIStyle presetIconButton { get { return new GUIStyle("IconButton"); } }

        private bool m_showOptions;

        private GUIStyle m_nameTextFieldStyle;
        protected GUIStyle NameTextFieldStyle
        {
            get
            {
                if (m_nameTextFieldStyle == null)
                {
                    m_nameTextFieldStyle = new GUIStyle(GUIStyle.none);
                    m_nameTextFieldStyle.alignment = TextAnchor.MiddleCenter;
                    m_nameTextFieldStyle.clipping = TextClipping.Clip;
                    m_nameTextFieldStyle.normal.textColor = EditorStyles.textField.normal.textColor;
                }
                return m_nameTextFieldStyle;
            }
            set { m_nameTextFieldStyle = value; }
        }

        private GUIStyle m_fbxExtLabelStyle;
        protected GUIStyle FbxExtLabelStyle
        {
            get
            {
                if (m_fbxExtLabelStyle == null)
                {
                    m_fbxExtLabelStyle = new GUIStyle(GUIStyle.none);
                    m_fbxExtLabelStyle.alignment = TextAnchor.MiddleLeft;
                    m_fbxExtLabelStyle.richText = true;
                    m_fbxExtLabelStyle.contentOffset = new Vector2(FbxExtOffset, 0);
                }
                return m_fbxExtLabelStyle;
            }
            set { m_fbxExtLabelStyle = value; }
        }

        private float m_fbxExtLabelWidth = -1;
        protected float FbxExtLabelWidth
        {
            get
            {
                if (m_fbxExtLabelWidth < 0)
                {
                    m_fbxExtLabelWidth = FbxExtLabelStyle.CalcSize(new GUIContent(".fbx")).x;
                }
                return m_fbxExtLabelWidth;
            }
            set { m_fbxExtLabelWidth = value; }
        }

        protected abstract bool DisableTransferAnim { get; }
        protected abstract bool DisableNameSelection { get; }

        protected abstract ExportOptionsSettingsSerializeBase SettingsObject { get; }

        // Helper functions for persisting the Export Settings for the session
        protected abstract string SessionStoragePrefix { get; }

        protected const string k_SessionSettingsName = "Settings";
        protected const string k_SessionFbxPathsName = "FbxSavePath";
        protected const string k_SessionSelectedFbxPathName = "SelectedFbxPath";
        protected const string k_SessionPrefabPathsName = "PrefabSavePath";
        protected const string k_SessionSelectedPrefabPathName = "SelectedPrefabPath";

        protected void StorePathsInSession(string varName, List<string> paths)
        {
            if (paths == null)
            {
                return;
            }

            var n = paths.Count;
            SessionState.SetInt(string.Format(SessionStoragePrefix, varName), n);
            for (int i = 0; i < n; i++)
            {
                SessionState.SetString(string.Format(SessionStoragePrefix + "_{1}", varName, i), paths[i]);
            }
        }

        protected void RestorePathsFromSession(string varName, List<string> defaultsPaths, out List<string> paths)
        {
            var n = SessionState.GetInt(string.Format(SessionStoragePrefix, varName), 0);
            if (n <= 0)
            {
                paths = defaultsPaths;
                return;
            }

            paths = new List<string>();
            for (int i = 0; i < n; i++)
            {
                var path = SessionState.GetString(string.Format(SessionStoragePrefix + "_{1}", varName, i), null);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }
        }

        protected static void ClearPathsFromSession(string varName, string prefix)
        {
            var n = SessionState.GetInt(string.Format(prefix, varName), 0);
            SessionState.EraseInt(string.Format(prefix, varName));
            for (int i = 0; i < n; i++)
            {
                SessionState.EraseString(string.Format(prefix + "_{1}", varName, i));
            }
        }

        protected virtual void StoreSettingsInSession()
        {
            var settings = SettingsObject;
            var json = EditorJsonUtility.ToJson(settings);
            SessionState.SetString(string.Format(SessionStoragePrefix, k_SessionSettingsName), json);

            StorePathsInSession(k_SessionFbxPathsName, m_fbxSavePaths);
            SessionState.SetInt(string.Format(SessionStoragePrefix, k_SessionSelectedFbxPathName), SelectedFbxPath);
        }

        protected virtual void RestoreSettingsFromSession(ExportOptionsSettingsSerializeBase defaults)
        {
            var settings = SettingsObject;
            var json = SessionState.GetString(string.Format(SessionStoragePrefix, k_SessionSettingsName), EditorJsonUtility.ToJson(defaults));
            if (!string.IsNullOrEmpty(json))
            {
                EditorJsonUtility.FromJsonOverwrite(json, settings);
            }
        }

        public static void ResetAllSessionSettings(string prefix, string settingsDefaults = null)
        {
            SessionState.EraseString(string.Format(prefix, k_SessionSettingsName));
            // Set the defaults of the settings.
            // If there exists a Default Preset for the Convert/Export settings, then if the project settings are modified,
            // the Default Preset will be reloaded instead of the project settings. Therefore, set them explicitely if projects settings desired.
            if (!string.IsNullOrEmpty(settingsDefaults))
            {
                SessionState.SetString(string.Format(prefix, k_SessionSettingsName), settingsDefaults);
            }

            ClearPathsFromSession(k_SessionFbxPathsName, prefix);
            SessionState.EraseInt(string.Format(prefix, k_SessionSelectedFbxPathName));

            ClearPathsFromSession(k_SessionPrefabPathsName, prefix);
            SessionState.EraseInt(string.Format(prefix, k_SessionSelectedPrefabPathName));
        }

        public virtual void ResetSessionSettings(string settingsDefaults = null)
        {
            ResetAllSessionSettings(SessionStoragePrefix, settingsDefaults);
            m_fbxSavePaths = null;
            SelectedFbxPath = 0;
        }

        private List<string> m_fbxSavePaths;
        internal List<string> FbxSavePaths
        {
            get
            {
                if (m_fbxSavePaths == null)
                {
                    // Try to restore from session, fall back to FBX Export Settings
                    RestorePathsFromSession(k_SessionFbxPathsName, ExportSettings.instance.GetCopyOfFbxSavePaths(), out m_fbxSavePaths);
                    SelectedFbxPath = SessionState.GetInt(string.Format(SessionStoragePrefix, k_SessionSelectedFbxPathName), ExportSettings.instance.SelectedFbxPath);
                }
                return m_fbxSavePaths;
            }
        }

        [SerializeField]
        private int m_selectedFbxPath = 0;
        internal int SelectedFbxPath
        {
            get { return m_selectedFbxPath; }
            set { m_selectedFbxPath = value; }
        }

        /// <summary>
        /// Caches the result of SelectionContainsPrefabInstanceWithAddedObjects() as it
        /// only needs to be updated when ToExport is modified.
        /// </summary>
        private bool m_exportSetContainsPrefabInstanceWithAddedObjects;

        private Object[] m_toExport;
        protected Object[] ToExport
        {
            get
            {
                return m_toExport;
            }
            set
            {
                m_toExport = value;
                m_exportSetContainsPrefabInstanceWithAddedObjects = SelectionContainsPrefabInstanceWithAddedObjects();
            }
        }

        protected virtual void OnEnable()
        {
            #if UNITY_2018_1_OR_NEWER && !UNITY_2023_1_OR_NEWER
            InitializeReceiver();
            #endif
            m_showOptions = true;
            this.minSize = new Vector2(SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth + ExportButtonWidth, MinWindowHeight);
        }

        protected static T CreateWindow<T>() where T : EditorWindow
        {
            return (T)EditorWindow.GetWindow<T>(DefaultWindowTitle, focus: true);
        }

        protected virtual void InitializeWindow(string filename = "")
        {
            this.titleContent = WindowTitle;
            this.SetFilename(filename);
        }

        #if UNITY_2018_1_OR_NEWER && !UNITY_2023_1_OR_NEWER
        protected void InitializeReceiver()
        {
            if (!Receiver)
            {
                Receiver = ScriptableObject.CreateInstance<FbxExportPresetSelectorReceiver>() as FbxExportPresetSelectorReceiver;
                Receiver.SelectionChanged -= OnPresetSelectionChanged;
                Receiver.SelectionChanged += OnPresetSelectionChanged;
                Receiver.DialogClosed -= SaveExportSettings;
                Receiver.DialogClosed += SaveExportSettings;
            }
        }

        #endif

        internal void SetFilename(string filename)
        {
            // remove .fbx from end of filename
            int extIndex = filename.LastIndexOf(".fbx");
            if (extIndex < 0)
            {
                ExportFileName = filename;
                return;
            }
            ExportFileName = filename.Remove(extIndex);
        }

        public abstract void SaveExportSettings();

        public void OnPresetSelectionChanged()
        {
            this.Repaint();
        }

        protected bool SelectionContainsPrefabInstanceWithAddedObjects()
        {
            var exportSet = ToExport;
            // FBX-60 (fogbug 1307749):
            // On Linux OnGUI() sometimes gets called a few times before
            // the export set is set and window.show() is called.
            // This leads to this function being called from OnGUI() with a
            // null or empty export set, and an ArgumentNullException when
            // creating the stack.
            // Check that the set exists and has values before creating the stack.
            if (exportSet == null || exportSet.Length <= 0)
            {
                return false;
            }

            Stack<Object> stack = new Stack<Object>(exportSet);
            while (stack.Count > 0)
            {
                var go = ModelExporter.GetGameObject(stack.Pop());
                if (!go)
                {
                    continue;
                }

                if (PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabUtility.GetAddedGameObjects(go).Count > 0)
                {
                    return true;
                }

                foreach (Transform child in go.transform)
                {
                    stack.Push(child.gameObject);
                }
            }
            return false;
        }

        protected abstract bool Export();

        /// <summary>
        /// Function to be used by derived classes to add custom UI between the file path selector and export options.
        /// </summary>
        protected virtual void CreateCustomUI() {}

        #if UNITY_2023_1_OR_NEWER
        protected abstract void ShowPresetReceiver();

        protected void ShowPresetReceiver(UnityEngine.Object target)
        {
            PresetSelector.ShowSelector(new[] {target}, null, true);
        }

        #elif UNITY_2018_1_OR_NEWER
        protected abstract void ShowPresetReceiver();

        protected void ShowPresetReceiver(UnityEngine.Object target)
        {
            InitializeReceiver();
            Receiver.SetTarget(target);
            Receiver.SetInitialValue(new Preset(target));
            UnityEditor.Presets.PresetSelector.ShowSelector(target, null, true, Receiver);
        }

        #endif

        protected Transform TransferAnimationSource
        {
            get
            {
                return SettingsObject.AnimationSource;
            }
            set
            {
                if (!TransferAnimationSourceIsValid(value))
                {
                    return;
                }
                SettingsObject.SetAnimationSource(value);
            }
        }

        protected Transform TransferAnimationDest
        {
            get
            {
                return SettingsObject.AnimationDest;
            }
            set
            {
                if (!TransferAnimationDestIsValid(value))
                {
                    return;
                }
                SettingsObject.SetAnimationDest(value);
            }
        }

        //-------Helper functions for determining if Animation source and dest are valid---------

        /// <summary>
        /// Determines whether p is an ancestor to t.
        /// </summary>
        /// <returns><c>true</c> if p is ancestor to t; otherwise, <c>false</c>.</returns>
        /// <param name="p">P.</param>
        /// <param name="t">T.</param>
        protected bool IsAncestor(Transform p, Transform t)
        {
            var curr = t;
            while (curr != null)
            {
                if (curr == p)
                {
                    return true;
                }
                curr = curr.parent;
            }
            return false;
        }

        /// <summary>
        /// Determines whether t1 and t2 are in the same hierarchy.
        /// </summary>
        /// <returns><c>true</c> if t1 is in same hierarchy as t2; otherwise, <c>false</c>.</returns>
        /// <param name="t1">T1.</param>
        /// <param name="t2">T2.</param>
        protected bool IsInSameHierarchy(Transform t1, Transform t2)
        {
            return (IsAncestor(t1, t2) || IsAncestor(t2, t1));
        }

        protected GameObject m_firstGameObjectToExport;
        protected virtual GameObject FirstGameObjectToExport
        {
            get
            {
                if (!m_firstGameObjectToExport)
                {
                    if (ToExport == null || ToExport.Length == 0)
                    {
                        return null;
                    }
                    m_firstGameObjectToExport = ModelExporter.GetGameObject(ToExport[0]);
                }
                return m_firstGameObjectToExport;
            }
        }

        protected bool TransferAnimationSourceIsValid(Transform newValue)
        {
            if (!newValue)
            {
                return true;
            }

            var selectedGO = FirstGameObjectToExport;
            if (!selectedGO)
            {
                Debug.LogWarning("FbxExportSettings: no Objects selected for export, can't transfer animation");
                return false;
            }

            // source must be ancestor to dest
            if (TransferAnimationDest && !IsAncestor(newValue, TransferAnimationDest))
            {
                Debug.LogWarningFormat("FbxExportSettings: Source {0} must be an ancestor of {1}", newValue.name, TransferAnimationDest.name);
                return false;
            }
            // must be in same hierarchy as selected GO
            if (!selectedGO || !IsInSameHierarchy(newValue, selectedGO.transform))
            {
                Debug.LogWarningFormat("FbxExportSettings: Source {0} must be in the same hierarchy as {1}", newValue.name, selectedGO ? selectedGO.name : "the selected object");
                return false;
            }
            return true;
        }

        protected bool TransferAnimationDestIsValid(Transform newValue)
        {
            if (!newValue)
            {
                return true;
            }

            var selectedGO = FirstGameObjectToExport;
            if (!selectedGO)
            {
                Debug.LogWarning("FbxExportSettings: no Objects selected for export, can't transfer animation");
                return false;
            }

            // source must be ancestor to dest
            if (TransferAnimationSource && !IsAncestor(TransferAnimationSource, newValue))
            {
                Debug.LogWarningFormat("FbxExportSettings: Destination {0} must be a descendant of {1}", newValue.name, TransferAnimationSource.name);
                return false;
            }
            // must be in same hierarchy as selected GO
            if (!selectedGO || !IsInSameHierarchy(newValue, selectedGO.transform))
            {
                Debug.LogWarningFormat("FbxExportSettings: Destination {0} must be in the same hierarchy as {1}", newValue.name, selectedGO ? selectedGO.name : "the selected object");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Add UI to turn the dialog off next time the user exports
        /// </summary>
        protected virtual void DoNotShowDialogUI()
        {
            EditorGUI.indentLevel--;
            ExportSettings.instance.DisplayOptionsWindow = !EditorGUILayout.Toggle(
                new GUIContent("Don't ask me again", "Don't ask me again, use the last used paths and options instead"),
                !ExportSettings.instance.DisplayOptionsWindow
            );
        }

        // -------------------------------------------------------------------------------------

        protected void OnGUI()
        {
            // Increasing the label width so that none of the text gets cut off
            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            #if UNITY_2018_1_OR_NEWER
            if (EditorGUILayout.DropdownButton(presetIcon, FocusType.Keyboard, presetIconButton))
            {
                ShowPresetReceiver();
            }
            #endif

            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Naming");
            EditorGUI.indentLevel++;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(
                "Export Name",
                "Filename to save model to."), GUILayout.Width(LabelWidth - TextFieldAlignOffset));

            EditorGUI.BeginDisabledGroup(DisableNameSelection);
            // Show the export name with an uneditable ".fbx" at the end
            //-------------------------------------
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUI.indentLevel--;
            // continually resize to contents
            var textFieldSize = NameTextFieldStyle.CalcSize(new GUIContent(ExportFileName));
            ExportFileName = EditorGUILayout.TextField(ExportFileName, NameTextFieldStyle, GUILayout.Width(textFieldSize.x + 5), GUILayout.MinWidth(5));
            ExportFileName = ModelExporter.ConvertToValidFilename(ExportFileName);

            EditorGUILayout.LabelField("<color=#808080ff>.fbx</color>", FbxExtLabelStyle, GUILayout.Width(FbxExtLabelWidth));
            EditorGUI.indentLevel++;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            //-----------------------------------
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(
                "Export Path",
                "Location where the FBX will be saved."), GUILayout.Width(LabelWidth - FieldOffset));

            var pathLabels = ExportSettings.GetMixedSavePaths(FbxSavePaths);

            if (this is ConvertToPrefabEditorWindow)
            {
                pathLabels = ExportSettings.GetRelativeFbxSavePaths(FbxSavePaths, ref m_selectedFbxPath);
            }

            SelectedFbxPath = EditorGUILayout.Popup(SelectedFbxPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

            if (!(this is ConvertToPrefabEditorWindow))
            {
                var exportSettingsEditor = InnerEditor as ExportModelSettingsEditor;
                // Set export setting for exporting outside the project on choosing a path
                var exportOutsideProject = !pathLabels[SelectedFbxPath].Substring(0, 6).Equals("Assets");
                exportSettingsEditor.SetExportingOutsideProject(exportOutsideProject);
            }

            if (GUILayout.Button(new GUIContent("...", "Browse to a new location to export to"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
            {
                string initialPath = Application.dataPath;

                string fullPath = EditorUtility.SaveFolderPanel(
                    "Select Export Model Path", initialPath, null
                );

                // Unless the user canceled, save path.
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var relativePath = ExportSettings.ConvertToAssetRelativePath(fullPath);

                    // If exporting an fbx for a prefab, not allowed to export outside the Assets folder
                    if (this is ConvertToPrefabEditorWindow && string.IsNullOrEmpty(relativePath))
                    {
                        Debug.LogWarning("Please select a location in the Assets folder");
                    }
                    // We're exporting outside Assets folder, so store the absolute path
                    else if (string.IsNullOrEmpty(relativePath))
                    {
                        ExportSettings.AddSavePath(fullPath, FbxSavePaths, exportOutsideProject: true);
                        SelectedFbxPath = 0;
                    }
                    // Store the relative path to the Assets folder
                    else
                    {
                        ExportSettings.AddSavePath(relativePath, FbxSavePaths, exportOutsideProject: false);
                        SelectedFbxPath = 0;
                    }
                    // Make sure focus is removed from the selectable label
                    // otherwise it won't update
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                }
            }
            GUILayout.EndHorizontal();

            CreateCustomUI();

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(DisableTransferAnim);
            EditorGUI.indentLevel--;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(
                "Transfer Animation",
                "Transfer transform animation from source to destination. Animation on objects between source and destination will also be transferred to destination."
                ), GUILayout.Width(LabelWidth - FieldOffset));
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel++;
            TransferAnimationSource = EditorGUILayout.ObjectField("Source", TransferAnimationSource, typeof(Transform), allowSceneObjects: true) as Transform;
            TransferAnimationDest = EditorGUILayout.ObjectField("Destination", TransferAnimationDest, typeof(Transform), allowSceneObjects: true) as Transform;
            EditorGUILayout.Space();
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            m_showOptions = EditorGUILayout.Foldout(m_showOptions, "Options");
            EditorGUI.indentLevel++;
            if (m_showOptions)
            {
                InnerEditor.OnInspectorGUI();
            }

            // if we are exporting or converting a prefab with overrides, then show a warning
            if (m_exportSetContainsPrefabInstanceWithAddedObjects)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Prefab instance overrides will be exported", MessageType.Warning, true);
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            DoNotShowDialogUI();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(ExportButtonWidth)))
            {
                this.Close();
            }

            if (GUILayout.Button(ExportButtonName, GUILayout.Width(ExportButtonWidth)))
            {
                if (Export())
                {
                    this.Close();
                }
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(); // adding a space at bottom of dialog so buttons aren't right at the edge

            if (GUI.changed)
            {
                SaveExportSettings();
            }
        }

        /// <summary>
        /// Checks whether the file exists and if it does then asks if it should be overwritten.
        /// </summary>
        /// <returns><c>true</c>, if file should be overwritten, <c>false</c> otherwise.</returns>
        /// <param name="filePath">File path.</param>
        protected bool OverwriteExistingFile(string filePath)
        {
            // check if file already exists, give a warning if it does
            if (System.IO.File.Exists(filePath))
            {
                bool overwrite = UnityEditor.EditorUtility.DisplayDialog(
                    string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME),
                    string.Format("File {0} already exists.\nOverwrite cannot be undone.", filePath),
                    "Overwrite", "Cancel");
                if (!overwrite)
                {
                    if (GUI.changed)
                    {
                        SaveExportSettings();
                    }
                    return false;
                }
            }
            return true;
        }
    }

    internal class ExportModelEditorWindow : ExportOptionsEditorWindow
    {
        public const string k_SessionStoragePrefix = "FbxExporterOptions_{0}";
        protected override string SessionStoragePrefix { get { return k_SessionStoragePrefix; } }

        protected override float MinWindowHeight { get { return 310; } } // determined by trial and error
        protected override bool DisableNameSelection
        {
            get
            {
                return false;
            }
        }

        protected override GameObject FirstGameObjectToExport
        {
            get
            {
                if (!m_firstGameObjectToExport)
                {
                    if (IsTimelineAnim)
                    {
                        m_firstGameObjectToExport = AnimationOnlyExportData.GetGameObjectAndAnimationClip(TimelineClipToExport).Key;
                    }
                    else if (ToExport != null && ToExport.Length > 0)
                    {
                        m_firstGameObjectToExport = ModelExporter.GetGameObject(ToExport[0]);
                    }
                }
                return m_firstGameObjectToExport;
            }
        }

        protected override bool DisableTransferAnim
        {
            get
            {
                // don't transfer animation if we are exporting more than one hierarchy, the timeline clips from
                // a playable director, or if only the model is being exported
                // if we are on the timeline then export length can be more than 1
                return SettingsObject.ModelAnimIncludeOption == Include.Model || (!IsTimelineAnim && (ToExport == null || ToExport.Length != 1));
            }
        }

        protected TimelineClip TimelineClipToExport { get; set; }
        protected PlayableDirector PlayableDirector { get; set; }

        private bool m_isTimelineAnim = false;
        protected bool IsTimelineAnim
        {
            get { return m_isTimelineAnim; }
            set
            {
                m_isTimelineAnim = value;
                if (m_isTimelineAnim)
                {
                    m_previousInclude = ExportModelSettingsInstance.info.ModelAnimIncludeOption;
                    ExportModelSettingsInstance.info.SetModelAnimIncludeOption(Include.Anim);
                }
                if (InnerEditor)
                {
                    var exportModelSettingsEditor = InnerEditor as ExportModelSettingsEditor;
                    if (exportModelSettingsEditor)
                    {
                        exportModelSettingsEditor.DisableIncludeDropdown(m_isTimelineAnim);
                    }
                }
            }
        }

        private bool m_singleHierarchyExport = true;
        protected bool SingleHierarchyExport
        {
            get { return m_singleHierarchyExport; }
            set
            {
                m_singleHierarchyExport = value;

                if (InnerEditor)
                {
                    var exportModelSettingsEditor = InnerEditor as ExportModelSettingsEditor;
                    if (exportModelSettingsEditor)
                    {
                        exportModelSettingsEditor.SetIsSingleHierarchy(m_singleHierarchyExport);
                    }
                }
            }
        }

        public override void ResetSessionSettings(string defaultSettings = null)
        {
            base.ResetSessionSettings(defaultSettings);

            // save the source and dest as these are not serialized
            var source = m_exportModelSettingsInstance.info.AnimationSource;
            var dest = m_exportModelSettingsInstance.info.AnimationDest;

            m_exportModelSettingsInstance = null;
            ExportModelSettingsInstance.info.SetAnimationSource(source);
            ExportModelSettingsInstance.info.SetAnimationDest(dest);
            InnerEditor = Editor.CreateEditor(ExportModelSettingsInstance);
        }

        private ExportModelSettings m_exportModelSettingsInstance;
        public ExportModelSettings ExportModelSettingsInstance
        {
            get
            {
                if (m_exportModelSettingsInstance == null)
                {
                    // make a copy of the settings
                    m_exportModelSettingsInstance = ScriptableObject.CreateInstance(typeof(ExportModelSettings)) as ExportModelSettings;
                    // load settings stored in Unity session, default to DefaultPreset, if none then Export Settings
                    var defaultPresets = Preset.GetDefaultPresetsForObject(m_exportModelSettingsInstance);
                    if (defaultPresets.Length <= 0)
                    {
                        RestoreSettingsFromSession(ExportSettings.instance.ExportModelSettings.info);
                    }
                    else
                    {
                        // apply the first default preset
                        // TODO: figure out what it means to have multiple default presets, when would they be applied?
                        defaultPresets[0].ApplyTo(m_exportModelSettingsInstance);
                        RestoreSettingsFromSession(m_exportModelSettingsInstance.info);
                    }
                }
                return m_exportModelSettingsInstance;
            }
        }

        public override void SaveExportSettings()
        {
            // check if the settings are different from what is in the Project Settings and only store
            // if they are. Otherwise we want to keep them updated with changes to the Project Settings.
            bool settingsChanged = !(ExportModelSettingsInstance.Equals(ExportSettings.instance.ExportModelSettings));
            var projectSettingsPaths = ExportSettings.instance.GetCopyOfFbxSavePaths();
            settingsChanged |= !projectSettingsPaths.SequenceEqual(FbxSavePaths);
            settingsChanged |= SelectedFbxPath != ExportSettings.instance.SelectedFbxPath;

            if (settingsChanged)
            {
                StoreSettingsInSession();
            }
        }

        protected override ExportOptionsSettingsSerializeBase SettingsObject
        {
            get { return ExportModelSettingsInstance.info; }
        }

        private Include m_previousInclude = Include.ModelAndAnim;

        public static ExportModelEditorWindow Init(IEnumerable<UnityEngine.Object> toExport, string filename = "", TimelineClip timelineClip = null, PlayableDirector director = null)
        {
            ExportModelEditorWindow window = CreateWindow<ExportModelEditorWindow>();
            window.IsTimelineAnim = (timelineClip != null);
            window.TimelineClipToExport = timelineClip;
            window.PlayableDirector = director ? director : TimelineEditor.inspectedDirector;


            int numObjects = window.SetGameObjectsToExport(toExport);
            if (string.IsNullOrEmpty(filename))
            {
                filename = window.DefaultFilename;
            }
            window.InitializeWindow(filename);
            window.SingleHierarchyExport = (numObjects == 1);
            window.Show();
            return window;
        }

        protected int SetGameObjectsToExport(IEnumerable<UnityEngine.Object> toExport)
        {
            ToExport = toExport?.ToArray();
            if (!IsTimelineAnim && (ToExport == null || ToExport.Length == 0)) return 0;

            TransferAnimationSource = null;
            TransferAnimationDest = null;

            // if only one object selected, set transfer source/dest to this object
            if (IsTimelineAnim || (ToExport != null && ToExport.Length == 1))
            {
                GameObject go = FirstGameObjectToExport;
                if (go)
                {
                    TransferAnimationSource = go.transform;
                    TransferAnimationDest = go.transform;
                }
            }

            return IsTimelineAnim ? 1 : ToExport.Length;
        }

        /// <summary>
        /// Gets the filename from objects to export.
        /// </summary>
        /// <returns>The object's name if one object selected, "Untitled" if multiple
        /// objects selected for export.</returns>
        protected string DefaultFilename
        {
            get
            {
                string filename;
                if (ToExport.Length == 1)
                {
                    filename = ToExport[0].name;
                }
                else
                {
                    filename = "Untitled";
                }
                return filename;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!InnerEditor)
            {
                InnerEditor = UnityEditor.Editor.CreateEditor(ExportModelSettingsInstance);
                this.SingleHierarchyExport = m_singleHierarchyExport;
                this.IsTimelineAnim = m_isTimelineAnim;
            }
        }

        protected void OnDisable()
        {
            RestoreSettings();
        }

        /// <summary>
        /// Restore changed export settings after export
        /// </summary>
        protected virtual void RestoreSettings()
        {
            if (IsTimelineAnim)
            {
                ExportModelSettingsInstance.info.SetModelAnimIncludeOption(m_previousInclude);
            }
        }

        protected override bool Export()
        {
            if (string.IsNullOrEmpty(ExportFileName))
            {
                Debug.LogError("FbxExporter: Please specify an fbx filename");
                return false;
            }
            var folderPath = ExportSettings.GetAbsoluteSavePath(FbxSavePaths[SelectedFbxPath]);
            var filePath = System.IO.Path.Combine(folderPath, ExportFileName + ".fbx");

            if (!OverwriteExistingFile(filePath))
            {
                return false;
            }

            string exportResult;
            if (IsTimelineAnim)
            {
                exportResult = ModelExporter.ExportTimelineClip(filePath, TimelineClipToExport, PlayableDirector, SettingsObject);
            }
            else
            {
                exportResult = ModelExporter.ExportObjects(filePath, ToExport, SettingsObject);
            }

            if (!string.IsNullOrEmpty(exportResult))
            {
                // refresh the asset database so that the file appears in the
                // asset folder view.
                AssetDatabase.Refresh();
            }
            return true;
        }

        #if UNITY_2018_1_OR_NEWER
        protected override void ShowPresetReceiver()
        {
            ShowPresetReceiver(ExportModelSettingsInstance);
        }

        #endif
    }
}
