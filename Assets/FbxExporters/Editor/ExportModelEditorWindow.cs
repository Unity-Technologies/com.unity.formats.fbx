using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxExporters.EditorTools;
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
            protected virtual float MinWindowHeight { get { return 250; } }

            protected virtual string ExportButtonName { get { return "Export"; } }

            protected virtual GUIContent WindowTitle { get { return new GUIContent (DefaultWindowTitle); } }

            protected string m_exportFileName = "";

            protected UnityEditor.Editor m_innerEditor;

            private bool m_showOptions;

            protected GUIStyle m_nameTextFieldStyle;
            protected GUIStyle m_fbxExtLabelStyle;
            protected float m_fbxExtLabelWidth;

            protected virtual void OnEnable(){
                m_showOptions = true;
                this.minSize = new Vector2 (SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth, MinWindowHeight);

                m_nameTextFieldStyle = new GUIStyle(GUIStyle.none);
                m_nameTextFieldStyle.alignment = TextAnchor.LowerCenter;
                m_nameTextFieldStyle.clipping = TextClipping.Clip;

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

            protected abstract void Export ();

            /// <summary>
            /// Function to be used by derived classes to add custom UI between the file path selector and export options.
            /// </summary>
            protected virtual void CreateCustomUI(){}

            protected virtual bool DisableNameSelection(){
                return false;
            }

            protected void OnGUI ()
            {
                // Increasing the label width so that none of the text gets cut off
                EditorGUIUtility.labelWidth = LabelWidth;

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Naming");
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Name:",
                    "Filename to save model to."),GUILayout.Width(LabelWidth-TextFieldAlignOffset));

                EditorGUI.BeginDisabledGroup (DisableNameSelection());
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
                    "Export Path:",
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
                    Export ();
                    this.Close ();
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
            protected override float MinWindowHeight { get { return 260; } }
            private UnityEngine.Object[] m_toExport;

            private bool m_isTimelineAnim = false;
            private bool m_singleHierarchyExport = true;
            private bool m_isPlayableDirector = false;

            public static void Init (IEnumerable<UnityEngine.Object> toExport, string filename = "", bool isTimelineAnim = false, bool isPlayableDirector = false)
            {
                ExportModelEditorWindow window = CreateWindow<ExportModelEditorWindow> ();
                int numObjects = window.SetGameObjectsToExport (toExport);
                if (string.IsNullOrEmpty (filename)) {
                    filename = window.GetFilenameFromObjects ();
                }
                window.InitializeWindow (filename);
                window.SetAnimationExportType (isTimelineAnim);
                window.SetSingleHierarchyExport (numObjects == 1);
                window.SetIsPlayableDirector (isPlayableDirector);
                window.Show ();
            }

            protected int SetGameObjectsToExport(IEnumerable<UnityEngine.Object> toExport){
                m_toExport = toExport.ToArray ();
                return m_toExport.Length;
            }

            private void SetAnimationExportType(bool isTimelineAnim){
                m_isTimelineAnim = isTimelineAnim;
                if (m_isTimelineAnim) {
                    ExportSettings.instance.exportModelSettings.info.SetModelAnimIncludeOption(ExportSettings.Include.Anim);
                }
                if (m_innerEditor) {
                    var exportModelSettingsEditor = m_innerEditor as ExportModelSettingsEditor;
                    if (exportModelSettingsEditor) {
                        exportModelSettingsEditor.DisableIncludeDropdown(m_isTimelineAnim);
                    }
                }
            }

            private void SetSingleHierarchyExport(bool singleHierarchy){
                m_singleHierarchyExport = singleHierarchy;

                if (m_innerEditor) {
                    var exportModelSettingsEditor = m_innerEditor as ExportModelSettingsEditor;
                    if (exportModelSettingsEditor) {
                        exportModelSettingsEditor.SetIsSingleHierarchy (m_singleHierarchyExport);
                    }
                }
            }

            private void SetIsPlayableDirector(bool isPlayableDirector){
                m_isPlayableDirector = isPlayableDirector;
            }

            /// <summary>
            /// Gets the filename from objects to export.
            /// </summary>
            /// <returns>The object's name if one object selected, "Untitled" if multiple
            /// objects selected for export.</returns>
            protected string GetFilenameFromObjects(){
                var filename = "";
                if (m_toExport.Length == 1) {
                    filename = m_toExport [0].name;
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
                    this.SetSingleHierarchyExport (m_singleHierarchyExport);
                    this.SetAnimationExportType (m_isTimelineAnim);
                }
            }

            protected override bool DisableNameSelection ()
            {
                return m_isPlayableDirector;
            }

            protected override void Export(){
                var folderPath = ExportSettings.GetFbxAbsoluteSavePath ();
                var filePath = System.IO.Path.Combine (folderPath, m_exportFileName + ".fbx");

                if (!OverwriteExistingFile (filePath)) {
                    return;
                }

                if (m_isPlayableDirector) {
                    foreach (var obj in m_toExport) {
                        var go = ModelExporter.GetGameObject (obj);
                        if (!go) {
                            continue;
                        }
                        ModelExporter.ExportAllTimelineClips (go, folderPath, ExportSettings.instance.exportModelSettings.info);
                    }
                    // refresh the asset database so that the file appears in the
                    // asset folder view.
                    AssetDatabase.Refresh ();
                    return;
                }

                if (ModelExporter.ExportObjects (filePath, m_toExport, ExportSettings.instance.exportModelSettings.info, timelineAnim: m_isTimelineAnim) != null) {
                    // refresh the asset database so that the file appears in the
                    // asset folder view.
                    AssetDatabase.Refresh ();
                }
            }
        }
    }
}