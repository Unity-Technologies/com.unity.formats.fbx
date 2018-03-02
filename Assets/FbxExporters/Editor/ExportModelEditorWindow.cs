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
            private string m_exportFileName = "";
            private ModelExporter.AnimationExportType m_animExportType = ModelExporter.AnimationExportType.all;

            private UnityEditor.Editor innerEditor;
            private static FbxExportPresetSelectorReceiver receiver;

            private static GUIContent presetIcon { get { return EditorGUIUtility.IconContent ("Preset.Context"); }}
            private static GUIStyle presetIconButton { get { return new GUIStyle("IconButton"); }}

            private bool showOptions;

            void OnEnable(){
                InitializeReceiver ();
                showOptions = true;
                this.minSize = new Vector2 (SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth, 220);

                if (!innerEditor) {
                    var ms = ExportSettings.instance.exportModelSettings;
                    if (!ms) {
                        ExportSettings.LoadSettings ();
                        ms = ExportSettings.instance.exportModelSettings;
                    }
                    innerEditor = UnityEditor.Editor.CreateEditor (ms, editorType: typeof(ExportModelSettingsEditor));
                }
            }

            public static void Init (string filename = "", ModelExporter.AnimationExportType exportType = ModelExporter.AnimationExportType.all)
            {
                ExportModelEditorWindow window = (ExportModelEditorWindow)EditorWindow.GetWindow <ExportModelEditorWindow>(WindowTitle, focus:true);
                window.SetFilename (filename);
                window.SetAnimationExportType (exportType);
                window.Show ();
            }

            private void InitializeReceiver(){
                if (!receiver) {
                    receiver = ScriptableObject.CreateInstance<FbxExportPresetSelectorReceiver> () as FbxExportPresetSelectorReceiver;
                    receiver.SelectionChanged -= OnPresetSelectionChanged;
                    receiver.SelectionChanged += OnPresetSelectionChanged;
                    receiver.DialogClosed -= OnPresetDialogClosed;
                    receiver.DialogClosed += OnPresetDialogClosed;
                }
            }

            public void SetFilename(string filename){
                m_exportFileName = filename;
            }

            public void SetAnimationExportType(ModelExporter.AnimationExportType exportType){
                m_animExportType = exportType;
            }

            public void OnPresetDialogClosed()
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
                    receiver.SetTarget(ExportSettings.instance.exportModelSettings);
                    receiver.SetInitialValue (new Preset (ExportSettings.instance.exportModelSettings));
                    UnityEditor.Presets.PresetSelector.ShowSelector(ExportSettings.instance.exportModelSettings, null, true, receiver);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Naming");
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Name:",
                    "Filename to save model to."),GUILayout.Width(LabelWidth-3));

                // Show the export name with an uneditable ".fbx" at the end
                EditorGUILayout.BeginVertical ();
                var style = new GUIStyle (EditorStyles.textField);
                EditorGUILayout.BeginHorizontal(style, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUI.indentLevel--;

                var textFieldStyle = new GUIStyle(GUIStyle.none);
                textFieldStyle.alignment = TextAnchor.LowerCenter;
                textFieldStyle.margin = new RectOffset (0, 0, 0, 0);
                var padding = textFieldStyle.padding;
                padding.left = 0;
                padding.right = 0;
                textFieldStyle.padding = padding;
                textFieldStyle.clipping = TextClipping.Clip;

                var textFieldSize = textFieldStyle.CalcSize (new GUIContent(m_exportFileName));
                m_exportFileName = EditorGUILayout.TextField (m_exportFileName, textFieldStyle, GUILayout.Width(textFieldSize.x + 5), GUILayout.MinWidth(5));
                m_exportFileName = ModelExporter.ConvertToValidFilename (m_exportFileName);

                var labelStyle = new GUIStyle (GUIStyle.none);
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.richText = true;
                labelStyle.margin = new RectOffset (0, 0, 0, 0);
                labelStyle.padding = new RectOffset (0, 0, 0, 0);
                labelStyle.contentOffset = new Vector2 (-7, 0);
                var fbxLabelWidth = labelStyle.CalcSize (new GUIContent (".fbx")).x;
                EditorGUILayout.LabelField ("<color=#808080ff>.fbx</color>", labelStyle, GUILayout.Width(fbxLabelWidth));
                EditorGUI.indentLevel++;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical ();
                /*m_exportFileName = EditorGUILayout.TextField (m_exportFileName);
                if (!m_exportFileName.EndsWith (".fbx")) {
                    m_exportFileName += ".fbx";
                }
                m_exportFileName = ModelExporter.ConvertToValidFilename(m_exportFileName);*/
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
                showOptions = EditorGUILayout.Foldout (showOptions, "Options");
                EditorGUI.indentLevel++;
                if (showOptions) {
                    innerEditor.OnInspectorGUI ();
                }

                GUILayout.FlexibleSpace ();

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Cancel", GUILayout.Width(100))) {
                    this.Close ();
                }

                if (GUILayout.Button ("Export", GUILayout.Width(100))) {
                    var filePath = ExportSettings.GetExportModelAbsoluteSavePath ();

                    filePath = System.IO.Path.Combine (filePath, m_exportFileName);

                    // check if file already exists, give a warning if it does
                    if (System.IO.File.Exists (filePath)) {
                        bool overwrite = UnityEditor.EditorUtility.DisplayDialog (
                                        string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), 
                                        string.Format("File {0} already exists.", filePath), 
                                        "Overwrite", "Cancel");
                        if (!overwrite) {
                            this.Close ();

                            if (GUI.changed) {
                                EditorUtility.SetDirty (ExportSettings.instance);
                                ExportSettings.instance.Save ();
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
                    EditorUtility.SetDirty (ExportSettings.instance);
                    ExportSettings.instance.Save ();
                }
            }
        }
    }
}