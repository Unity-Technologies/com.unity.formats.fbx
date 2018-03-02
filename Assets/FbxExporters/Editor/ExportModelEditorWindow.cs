using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxExporters.EditorTools;
using UnityEditor.Presets;

namespace FbxExporters
{
    namespace Editor
    {
        public class ExportModelEditorWindow : EditorWindow
        {

            private const string WindowTitle = "Export Options";
            private const float SelectableLabelMinWidth = 90;
            private const float BrowseButtonWidth = 25;
            private const float LabelWidth = 175;
            private const float FieldOffset = 18;
            private const float TextFieldAlignOffset = 3;
            private const float ExportButtonWidth = 100;
            private const float FbxExtOffset = -7;

            private string m_exportFileName = "";
            private ModelExporter.AnimationExportType m_animExportType = ModelExporter.AnimationExportType.all;
            private bool m_singleHierarchyExport = true;

            private ExportModelSettingsEditor m_innerEditor;
            private static FbxExportPresetSelectorReceiver m_receiver;

            private static GUIContent presetIcon { get { return EditorGUIUtility.IconContent ("Preset.Context"); }}
            private static GUIStyle presetIconButton { get { return new GUIStyle("IconButton"); }}

            private bool m_showOptions;

            private GUIStyle m_nameTextFieldStyle;
            private GUIStyle m_fbxExtLabelStyle;
            private float m_fbxExtLabelWidth;

            void OnEnable(){
                InitializeReceiver ();
                m_showOptions = true;
                this.minSize = new Vector2 (SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth, 220);

                if (!m_innerEditor) {
                    var ms = ExportSettings.instance.exportModelSettings;
                    if (!ms) {
                        ExportSettings.LoadSettings ();
                        ms = ExportSettings.instance.exportModelSettings;
                    }
                    m_innerEditor = UnityEditor.Editor.CreateEditor (ms) as ExportModelSettingsEditor;
                    m_innerEditor.SetIsSingleHierarchy (m_singleHierarchyExport);
                }

                m_nameTextFieldStyle = new GUIStyle(GUIStyle.none);
                m_nameTextFieldStyle.alignment = TextAnchor.LowerCenter;
                m_nameTextFieldStyle.clipping = TextClipping.Clip;

                m_fbxExtLabelStyle = new GUIStyle (GUIStyle.none);
                m_fbxExtLabelStyle.alignment = TextAnchor.MiddleLeft;
                m_fbxExtLabelStyle.richText = true;
                m_fbxExtLabelStyle.contentOffset = new Vector2 (FbxExtOffset, 0);

                m_fbxExtLabelWidth = m_fbxExtLabelStyle.CalcSize (new GUIContent (".fbx")).x;
            }

            public static void Init (string filename = "", bool singleHierarchyExport = true, ModelExporter.AnimationExportType exportType = ModelExporter.AnimationExportType.all)
            {
                ExportModelEditorWindow window = (ExportModelEditorWindow)EditorWindow.GetWindow <ExportModelEditorWindow>(WindowTitle, focus:true);
                window.SetFilename (filename);
                window.SetAnimationExportType (exportType);
                window.SetSingleHierarchyExport (singleHierarchyExport);
                window.Show ();
            }

            private void InitializeReceiver(){
                if (!m_receiver) {
                    m_receiver = ScriptableObject.CreateInstance<FbxExportPresetSelectorReceiver> () as FbxExportPresetSelectorReceiver;
                    m_receiver.SelectionChanged -= OnPresetSelectionChanged;
                    m_receiver.SelectionChanged += OnPresetSelectionChanged;
                    m_receiver.DialogClosed -= SaveExportSettings;
                    m_receiver.DialogClosed += SaveExportSettings;
                }
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

            public void SetAnimationExportType(ModelExporter.AnimationExportType exportType){
                m_animExportType = exportType;
            }

            public void SetSingleHierarchyExport(bool singleHierarchy){
                m_singleHierarchyExport = singleHierarchy;

                if (m_innerEditor) {
                    m_innerEditor.SetIsSingleHierarchy (m_singleHierarchyExport);
                }
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

            void OnGUI ()
            {
                // Increasing the label width so that none of the text gets cut off
                EditorGUIUtility.labelWidth = LabelWidth;

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                if(EditorGUILayout.DropdownButton(presetIcon, FocusType.Keyboard, presetIconButton)){
                    InitializeReceiver ();
                    m_receiver.SetTarget(ExportSettings.instance.exportModelSettings);
                    m_receiver.SetInitialValue (new Preset (ExportSettings.instance.exportModelSettings));
                    UnityEditor.Presets.PresetSelector.ShowSelector(ExportSettings.instance.exportModelSettings, null, true, m_receiver);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Naming");
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Name:",
                    "Filename to save model to."),GUILayout.Width(LabelWidth-TextFieldAlignOffset));

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
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Path:",
                    "Relative path for saving Model Prefabs."),GUILayout.Width(LabelWidth - FieldOffset));

                var pathLabels = ExportSettings.GetRelativeSavePaths();

                ExportSettings.instance.selectedExportModelPath = EditorGUILayout.Popup (ExportSettings.instance.selectedExportModelPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

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
                            ExportSettings.AddExportModelSavePath(relativePath);

                            // Make sure focus is removed from the selectable label
                            // otherwise it won't update
                            GUIUtility.hotControl = 0;
                            GUIUtility.keyboardControl = 0;
                        }
                    }
                }
                GUILayout.EndHorizontal();

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

                if (GUILayout.Button ("Export", GUILayout.Width(ExportButtonWidth))) {
                    var filePath = ExportSettings.GetExportModelAbsoluteSavePath ();

                    filePath = System.IO.Path.Combine (filePath, m_exportFileName + ".fbx");

                    // check if file already exists, give a warning if it does
                    if (System.IO.File.Exists (filePath)) {
                        bool overwrite = UnityEditor.EditorUtility.DisplayDialog (
                                        string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), 
                                        string.Format("File {0} already exists.", filePath), 
                                        "Overwrite", "Cancel");
                        if (!overwrite) {
                            this.Close ();

                            if (GUI.changed) {
                                SaveExportSettings ();
                            }
                            return;
                        }
                    }

                    if (ModelExporter.ExportObjects (filePath, exportType: m_animExportType, lodExportType: ExportSettings.GetLODExportType()) != null) {
                        // refresh the asset database so that the file appears in the
                        // asset folder view.
                        AssetDatabase.Refresh ();
                    }
                    this.Close ();
                }
                GUILayout.EndHorizontal ();

                if (GUI.changed) {
                    SaveExportSettings ();
                }
            }
        }
    }
}