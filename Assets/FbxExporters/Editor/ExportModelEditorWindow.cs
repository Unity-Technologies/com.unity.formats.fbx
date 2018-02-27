using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxExporters.EditorTools;

namespace FbxExporters
{
    namespace Editor
    {
        public class ExportModelEditorWindow : EditorWindow
        {

            private const string WindowTitle = "Export Options";
            private const float SelectableLabelMinWidth = 90;
            private const float BrowseButtonWidth = 25;
            private const float LabelWidth = 144;
            private const float FieldOffset = 18;

            public static void Init ()
            {
                ExportModelEditorWindow window = (ExportModelEditorWindow)EditorWindow.GetWindow <ExportModelEditorWindow>(WindowTitle, focus:true);
                window.Show ();

                window.minSize = new Vector2 (SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth, 100);
            }

            void OnGUI ()
            {
                // Increasing the label width so that none of the text gets cut off
                EditorGUIUtility.labelWidth = LabelWidth;

                EditorGUILayout.LabelField("Naming", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Path:",
                    "Relative path for saving Model Prefabs."),GUILayout.Width(LabelWidth - FieldOffset));

                var pathLabel = ExportSettings.GetRelativeSavePath();
                if (pathLabel == ".") { pathLabel = "(Assets root)"; }
                EditorGUILayout.SelectableLabel(pathLabel,
                    EditorStyles.textField,
                    GUILayout.MinWidth(SelectableLabelMinWidth),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));            

                if (GUILayout.Button(new GUIContent("...", "Browse to a new location for saving model prefabs"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
                {
                    string initialPath = ExportSettings.GetAbsoluteSavePath();

                    // if the directory doesn't exist, set it to the default save path
                    // so we don't open somewhere unexpected
                    if (!System.IO.Directory.Exists(initialPath))
                    {
                        initialPath = Application.dataPath;
                    }

                    string fullPath = EditorUtility.OpenFolderPanel(
                        "Select Model Prefabs Path", initialPath, null
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
                            ExportSettings.SetRelativeSavePath(relativePath);

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
}