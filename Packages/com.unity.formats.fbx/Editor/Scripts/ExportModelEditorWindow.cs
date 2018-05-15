using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxExporters.EditorTools;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Presets;
#endif
using System.Linq;

namespace FbxExporters
{
    namespace Editor
    {
        public abstract class ExportOptionsEditorWindow : EditorWindow
        {
            protected const string DefaultWindowTitle = "Export Options";
            protected const float SelectableLabelMinWidth = 90;
            protected const float BrowseButtonWidth = 25;
            protected const float LabelWidth = 175;
            protected const float FieldOffset = 18;
            protected const float TextFieldAlignOffset = 3;
            protected const float ExportButtonWidth = 100;
            protected const float FbxExtOffset = -7;
            protected virtual float MinWindowHeight { get { return 300; } }

            protected virtual string ExportButtonName { get { return "Export"; } }

            protected virtual GUIContent WindowTitle { get { return new GUIContent (DefaultWindowTitle); } }

            protected string m_exportFileName = "";

            protected UnityEditor.Editor m_innerEditor;
            #if UNITY_2018_1_OR_NEWER
            protected FbxExportPresetSelectorReceiver m_receiver;
            #endif
            private static GUIContent presetIcon { get { return EditorGUIUtility.IconContent ("Preset.Context"); }}
            private static GUIStyle presetIconButton { get { return new GUIStyle("IconButton"); }}

            private bool m_showOptions;

            protected GUIStyle m_nameTextFieldStyle;
            protected GUIStyle m_fbxExtLabelStyle;
            protected float m_fbxExtLabelWidth;

            protected abstract bool DisableTransferAnim { get; }
            protected abstract bool DisableNameSelection { get; }

            protected abstract EditorTools.ExportOptionsSettingsSerializeBase SettingsObject { get; }

            private UnityEngine.Object[] m_toExport;
            protected UnityEngine.Object[] ToExport {
                get { return m_toExport; }
                set { m_toExport = value; }
            }

            protected virtual void OnEnable(){
                #if UNITY_2018_1_OR_NEWER
                InitializeReceiver ();
                #endif
                m_showOptions = true;
                this.minSize = new Vector2 (SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth, MinWindowHeight);

                m_nameTextFieldStyle = new GUIStyle(GUIStyle.none);
                m_nameTextFieldStyle.alignment = TextAnchor.LowerCenter;
                m_nameTextFieldStyle.clipping = TextClipping.Clip;
                m_nameTextFieldStyle.normal.textColor = EditorStyles.textField.normal.textColor;

                m_fbxExtLabelStyle = new GUIStyle (GUIStyle.none);
                m_fbxExtLabelStyle.alignment = TextAnchor.MiddleLeft;
                m_fbxExtLabelStyle.richText = true;
                m_fbxExtLabelStyle.contentOffset = new Vector2 (FbxExtOffset, 0);

                m_fbxExtLabelWidth = m_fbxExtLabelStyle.CalcSize (new GUIContent (".fbx")).x;
            }

            protected static T CreateWindow<T>() where T : EditorWindow {
                return (T)EditorWindow.GetWindow <T>(DefaultWindowTitle, focus:true);
            }

            protected virtual void InitializeWindow(string filename = ""){
                this.titleContent = WindowTitle;
                this.SetFilename (filename);
            }

            #if UNITY_2018_1_OR_NEWER
            protected void InitializeReceiver(){
                if (!m_receiver) {
                    m_receiver = ScriptableObject.CreateInstance<FbxExportPresetSelectorReceiver> () as FbxExportPresetSelectorReceiver;
                    m_receiver.SelectionChanged -= OnPresetSelectionChanged;
                    m_receiver.SelectionChanged += OnPresetSelectionChanged;
                    m_receiver.DialogClosed -= SaveExportSettings;
                    m_receiver.DialogClosed += SaveExportSettings;
                }
            }
            #endif

            public void SetFilename(string filename){
                // remove .fbx from end of filename
                int extIndex = filename.LastIndexOf(".fbx");
                if (extIndex < 0) {
                    m_exportFileName = filename;
                    return;
                }
                m_exportFileName = filename.Remove(extIndex);
            }

            public void SaveExportSettings()
            {
                // save once preset selection is finished
                EditorUtility.SetDirty (ExportSettings.instance);
                ExportSettings.instance.Save ();
            }

            public void OnPresetSelectionChanged()
            {
                this.Repaint ();
            }

            protected abstract bool Export ();

            /// <summary>
            /// Function to be used by derived classes to add custom UI between the file path selector and export options.
            /// </summary>
            protected virtual void CreateCustomUI(){}

            #if UNITY_2018_1_OR_NEWER  
            protected abstract void ShowPresetReceiver ();

            protected void ShowPresetReceiver(UnityEngine.Object target){
                InitializeReceiver ();
                m_receiver.SetTarget(target);
                m_receiver.SetInitialValue (new Preset (target));
                UnityEditor.Presets.PresetSelector.ShowSelector(target, null, true, m_receiver);
            }
            #endif

            protected Transform TransferAnimationSource {
                get {
                    return SettingsObject.AnimationSource;
                }
                set {
                    if (!TransferAnimationSourceIsValid (value)) {
                        return;
                    }
                    SettingsObject.SetAnimationSource (value);
                }
            }

            protected Transform TransferAnimationDest {
                get {
                    return SettingsObject.AnimationDest;
                }
                set {
                    if (!TransferAnimationDestIsValid (value)) {
                        return;
                    }
                    SettingsObject.SetAnimationDest (value);
                }
            }

            //-------Helper functions for determining if Animation source and dest are valid---------

            /// <summary>
            /// Determines whether p is an ancestor to t.
            /// </summary>
            /// <returns><c>true</c> if p is ancestor to t; otherwise, <c>false</c>.</returns>
            /// <param name="p">P.</param>
            /// <param name="t">T.</param>
            protected bool IsAncestor(Transform p, Transform t){
                var curr = t;
                while (curr != null) {
                    if (curr == p) {
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
            protected bool IsInSameHierarchy(Transform t1, Transform t2){
                return (IsAncestor (t1, t2) || IsAncestor (t2, t1));
            }

            protected virtual GameObject GetGameObject()
            {
                return ModelExporter.GetGameObject ( ToExport [0] );
            }

            protected bool TransferAnimationSourceIsValid(Transform newValue){
                if (!newValue) {
                    return true;
                }

                if (ToExport == null || ToExport.Length <= 0) {
                    Debug.LogWarning ("FbxExportSettings: no Objects selected for export, can't transfer animation");
                    return false;
                }

                var selectedGO = GetGameObject();

                // source must be ancestor to dest
                if (TransferAnimationDest && !IsAncestor(newValue, TransferAnimationDest)) {
                    Debug.LogWarningFormat("FbxExportSettings: Source {0} must be an ancestor of {1}", newValue.name, TransferAnimationDest.name);
                    return false;
                }
                // must be in same hierarchy as selected GO
                if (!selectedGO || !IsInSameHierarchy(newValue, selectedGO.transform)) {
                    Debug.LogWarningFormat("FbxExportSettings: Source {0} must be in the same hierarchy as {1}", newValue.name, selectedGO? selectedGO.name : "the selected object");
                    return false;
                }
                return true;
            }

            protected bool TransferAnimationDestIsValid(Transform newValue){
                if (!newValue) {
                    return true;
                }

                if (ToExport == null || ToExport.Length <= 0) {
                    Debug.LogWarning ("FbxExportSettings: no Objects selected for export, can't transfer animation");
                    return false;
                }

                var selectedGO = GetGameObject();

                // source must be ancestor to dest
                if (TransferAnimationSource && !IsAncestor(TransferAnimationSource, newValue)) {
                    Debug.LogWarningFormat("FbxExportSettings: Destination {0} must be a descendant of {1}", newValue.name, TransferAnimationSource.name);
                    return false;
                }
                // must be in same hierarchy as selected GO
                if (!selectedGO || !IsInSameHierarchy(newValue, selectedGO.transform)) {
                    Debug.LogWarningFormat("FbxExportSettings: Destination {0} must be in the same hierarchy as {1}", newValue.name, selectedGO? selectedGO.name : "the selected object");
                    return false;
                }
                return true;
            }

            // -------------------------------------------------------------------------------------

            protected void OnGUI ()
            {
                // Increasing the label width so that none of the text gets cut off
                EditorGUIUtility.labelWidth = LabelWidth;

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                
                #if UNITY_2018_1_OR_NEWER  
                if(EditorGUILayout.DropdownButton(presetIcon, FocusType.Keyboard, presetIconButton)){
                    ShowPresetReceiver ();
                }
                #endif

                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Naming");
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Name",
                    "Filename to save model to."),GUILayout.Width(LabelWidth-TextFieldAlignOffset));

                EditorGUI.BeginDisabledGroup (DisableNameSelection);
                // Show the export name with an uneditable ".fbx" at the end
                //-------------------------------------
                EditorGUILayout.BeginVertical ();
                EditorGUILayout.BeginHorizontal(EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUI.indentLevel--;
                // continually resize to contents
                var textFieldSize = m_nameTextFieldStyle.CalcSize (new GUIContent(m_exportFileName));
                m_exportFileName = EditorGUILayout.TextField (m_exportFileName, m_nameTextFieldStyle, GUILayout.Width(textFieldSize.x + 5), GUILayout.MinWidth(5));
                m_exportFileName = ModelExporter.ConvertToValidFilename (m_exportFileName);

                EditorGUILayout.LabelField ("<color=#808080ff>.fbx</color>", m_fbxExtLabelStyle, GUILayout.Width(m_fbxExtLabelWidth));
                EditorGUI.indentLevel++;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical ();
                //-----------------------------------
                EditorGUI.EndDisabledGroup ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Path",
                    "Relative path for saving Model Prefabs."),GUILayout.Width(LabelWidth - FieldOffset));

                var pathLabels = ExportSettings.GetRelativeFbxSavePaths();

                ExportSettings.instance.selectedFbxPath = EditorGUILayout.Popup (ExportSettings.instance.selectedFbxPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

                if (GUILayout.Button(new GUIContent("...", "Browse to a new location to export to"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
                {
                    string initialPath = Application.dataPath;

                    string fullPath = EditorUtility.OpenFolderPanel(
                        "Select Export Model Path", initialPath, null
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
                            ExportSettings.AddFbxSavePath(relativePath);

                            // Make sure focus is removed from the selectable label
                            // otherwise it won't update
                            GUIUtility.hotControl = 0;
                            GUIUtility.keyboardControl = 0;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                CreateCustomUI();

                EditorGUILayout.Space ();

                EditorGUI.BeginDisabledGroup (DisableTransferAnim);
                EditorGUI.indentLevel--;
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "Transfer Animation",
                    "Transfer transform animation from source to destination. Animation on objects between source and destination will also be transferred to destination."
                ), GUILayout.Width(LabelWidth - FieldOffset));
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel++;
                TransferAnimationSource = EditorGUILayout.ObjectField ("Source", TransferAnimationSource, typeof(Transform), allowSceneObjects: true) as Transform;
                TransferAnimationDest = EditorGUILayout.ObjectField ("Destination", TransferAnimationDest, typeof(Transform), allowSceneObjects: true) as Transform;
                EditorGUILayout.Space ();
                EditorGUI.EndDisabledGroup ();

                EditorGUI.indentLevel--;
                m_showOptions = EditorGUILayout.Foldout (m_showOptions, "Options");
                EditorGUI.indentLevel++;
                if (m_showOptions) {
                    m_innerEditor.OnInspectorGUI ();
                }

                GUILayout.FlexibleSpace ();

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Cancel", GUILayout.Width(ExportButtonWidth))) {
                    this.Close ();
                }

                if (GUILayout.Button (ExportButtonName, GUILayout.Width(ExportButtonWidth))) {
                    if (Export ()) {
                        this.Close ();
                    }
                }
                GUILayout.EndHorizontal ();

                if (GUI.changed) {
                    SaveExportSettings ();
                }
            }

            /// <summary>
            /// Checks whether the file exists and if it does then asks if it should be overwritten.
            /// </summary>
            /// <returns><c>true</c>, if file should be overwritten, <c>false</c> otherwise.</returns>
            /// <param name="filePath">File path.</param>
            protected bool OverwriteExistingFile(string filePath){
                // check if file already exists, give a warning if it does
                if (System.IO.File.Exists (filePath)) {
                    bool overwrite = UnityEditor.EditorUtility.DisplayDialog (
                        string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), 
                        string.Format("File {0} already exists.", filePath), 
                        "Overwrite", "Cancel");
                    if (!overwrite) {
                        if (GUI.changed) {
                            SaveExportSettings ();
                        }
                        return false;
                    }
                }
                return true;
            }
        }

        public class ExportModelEditorWindow : ExportOptionsEditorWindow
        {
            protected override float MinWindowHeight { get { return 310; } } // determined by trial and error
            protected override bool DisableNameSelection {
                get {
                    return false;
                }
            }

            protected override GameObject GetGameObject()
            {
                return (IsTimelineAnim)
                    ? ModelExporter.AnimationOnlyExportData.GetGameObjectAndAnimationClip(ToExport [0]).Key
                    :  ModelExporter.GetGameObject ( ToExport [0] );
            }

            protected override bool DisableTransferAnim {
                get {
                    // don't transfer animation if we are exporting more than one hierarchy, the timeline clips from
                    // a playable director, or if only the model is being exported
                    // if we are on the timeline then export length can be more than 1
                    return ToExport == null || ToExport.Length == 0 || (!IsTimelineAnim && ToExport.Length > 1) || SettingsObject.ModelAnimIncludeOption == ExportSettings.Include.Model;
                }
            }

            private bool m_isTimelineAnim = false;
            protected bool IsTimelineAnim {
                get { return m_isTimelineAnim; }
                set{
                    m_isTimelineAnim = value;
                    if (m_isTimelineAnim) {
                        m_previousInclude = ExportSettings.instance.exportModelSettings.info.ModelAnimIncludeOption;
                        ExportSettings.instance.exportModelSettings.info.SetModelAnimIncludeOption(ExportSettings.Include.Anim);
                    }
                    if (m_innerEditor) {
                        var exportModelSettingsEditor = m_innerEditor as ExportModelSettingsEditor;
                        if (exportModelSettingsEditor) {
                            exportModelSettingsEditor.DisableIncludeDropdown(m_isTimelineAnim);
                        }
                    }
                }
            }

            private bool m_singleHierarchyExport = true;
            protected bool SingleHierarchyExport {
                get { return m_singleHierarchyExport; }
                set {
                    m_singleHierarchyExport = value;

                    if (m_innerEditor) {
                        var exportModelSettingsEditor = m_innerEditor as ExportModelSettingsEditor;
                        if (exportModelSettingsEditor) {
                            exportModelSettingsEditor.SetIsSingleHierarchy (m_singleHierarchyExport);
                        }
                    }
                }
            }

            protected override ExportOptionsSettingsSerializeBase SettingsObject
            {
                get { return ExportSettings.instance.exportModelSettings.info; }
            }

            private ExportSettings.Include m_previousInclude = ExportSettings.Include.ModelAndAnim;

            public static void Init (IEnumerable<UnityEngine.Object> toExport, string filename = "", bool isTimelineAnim = false)
            {
                ExportModelEditorWindow window = CreateWindow<ExportModelEditorWindow> ();
                window.IsTimelineAnim = isTimelineAnim;

                int numObjects = window.SetGameObjectsToExport (toExport);
                if (string.IsNullOrEmpty (filename)) {
                    filename = window.GetFilenameFromObjects ();
                }
                window.InitializeWindow (filename);
                window.SingleHierarchyExport = (numObjects == 1);
                window.Show ();
            }

            protected int SetGameObjectsToExport(IEnumerable<UnityEngine.Object> toExport){
                ToExport = toExport.ToArray ();
                if (ToExport.Length==0) return 0;

                TransferAnimationSource = null;
                TransferAnimationDest = null;

                // if only one object selected, set transfer source/dest to this object
                GameObject go = GetGameObject();                

                if (go) {
                    TransferAnimationSource = go.transform;
                    TransferAnimationDest = go.transform;
                }

                return 1;
            }

            /// <summary>
            /// Gets the filename from objects to export.
            /// </summary>
            /// <returns>The object's name if one object selected, "Untitled" if multiple
            /// objects selected for export.</returns>
            protected string GetFilenameFromObjects(){
                var filename = "";
                if (ToExport.Length == 1) {
                    filename = ToExport [0].name;
                } else {
                    filename = "Untitled";
                }
                return filename;
            }

            protected override void OnEnable ()
            {
                base.OnEnable ();
                if (!m_innerEditor) {
                    m_innerEditor = UnityEditor.Editor.CreateEditor (ExportSettings.instance.exportModelSettings);
                    this.SingleHierarchyExport = m_singleHierarchyExport;
                    this.IsTimelineAnim = m_isTimelineAnim;
                }
            }

            protected void OnDisable()
            {
                RestoreSettings ();
            }

            /// <summary>
            /// Restore changed export settings after export
            /// </summary>
            protected virtual void RestoreSettings()
            {
                if (IsTimelineAnim) {
                    ExportSettings.instance.exportModelSettings.info.SetModelAnimIncludeOption(m_previousInclude);
                    SaveExportSettings ();
                }
            }


            protected override bool Export(){
                if (string.IsNullOrEmpty (m_exportFileName)) {
                    Debug.LogError ("FbxExporter: Please specify an fbx filename");
                    return false;
                }
                var folderPath = ExportSettings.GetFbxAbsoluteSavePath ();
                var filePath = System.IO.Path.Combine (folderPath, m_exportFileName + ".fbx");

                if (!OverwriteExistingFile (filePath)) {
                    return false;
                }

                if (ModelExporter.ExportObjects (filePath, ToExport, SettingsObject) != null) {
                    // refresh the asset database so that the file appears in the
                    // asset folder view.
                    AssetDatabase.Refresh ();
                }
                return true;
            }

            #if UNITY_2018_1_OR_NEWER  
            protected override void ShowPresetReceiver ()
            {
                ShowPresetReceiver (ExportSettings.instance.exportModelSettings);
            }
            #endif
        }
    }
}