using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxExporters.EditorTools;
using UnityEditor.Presets;
using System.Linq;

namespace FbxExporters
{
    namespace Editor
    {
        public class ConvertToPrefabEditorWindow : ExportOptionsEditorWindow
        {
            protected override GUIContent WindowTitle { get { return new GUIContent ("Convert Options"); }}
            protected override float MinWindowHeight { get { return 280; } } // determined by trial and error
            protected override string ExportButtonName { get { return "Convert"; } }
            private GameObject[] m_toConvert;
            private string m_prefabFileName = "";

            private float m_prefabExtLabelWidth;

            public static void Init (IEnumerable<GameObject> toConvert)
            {
                ConvertToPrefabEditorWindow window = CreateWindow<ConvertToPrefabEditorWindow> ();
                window.InitializeWindow ();
                window.SetGameObjectsToConvert (toConvert);
                window.Show ();
            }

            protected void SetGameObjectsToConvert(IEnumerable<GameObject> toConvert){
                m_toConvert = toConvert.OrderBy (go => go.name).ToArray ();

                if (m_toConvert.Length == 1) {
                    var go = m_toConvert [0];
                    // check if the GameObject is a model instance, use as default filename and path if it is
                    PrefabType unityPrefabType = PrefabUtility.GetPrefabType(go);
                    if (unityPrefabType == PrefabType.ModelPrefabInstance && go.Equals (PrefabUtility.FindPrefabRoot (go))) {
                        var mainAsset = PrefabUtility.GetPrefabParent (go) as GameObject;
                        var mainAssetRelPath = AssetDatabase.GetAssetPath (mainAsset);
                        // remove Assets/ from beginning of path
                        mainAssetRelPath = mainAssetRelPath.Substring ("Assets".Length);

                        m_prefabFileName = System.IO.Path.GetFileNameWithoutExtension (mainAssetRelPath);
                        ExportSettings.AddFbxSavePath (System.IO.Path.GetDirectoryName (mainAssetRelPath));
                    } else {
                        m_prefabFileName = go.name;
                    }
                } else if (m_toConvert.Length > 1) {
                    m_prefabFileName = "(automatic)";
                }
                this.SetFilename (m_prefabFileName);
            }

            protected override void OnEnable ()
            {
                base.OnEnable ();
                if (!m_innerEditor) {
                    m_innerEditor = UnityEditor.Editor.CreateEditor (ExportSettings.instance.convertToPrefabSettings);
                }
                m_prefabExtLabelWidth = m_fbxExtLabelStyle.CalcSize (new GUIContent (".prefab")).x;
            }

            protected override void Export ()
            {
                var fbxDirPath = ExportSettings.GetFbxAbsoluteSavePath ();
                var fbxPath = System.IO.Path.Combine (fbxDirPath, m_exportFileName + ".fbx");

                var prefabDirPath = ExportSettings.GetPrefabAbsoluteSavePath ();
                var prefabPath = System.IO.Path.Combine (prefabDirPath, m_prefabFileName + ".prefab");

                if (m_toConvert == null) {
                    Debug.LogError ("FbxExporter: missing object for conversion");
                    return;
                }

                if (m_toConvert.Length == 1) {
                    var go = m_toConvert [0];

                    if (!OverwriteExistingFile (prefabPath)) {
                        return;
                    }

                    // Only create the prefab (no FBX export) if we have selected the root of a model prefab instance.
                    // Children of model prefab instances will also have "model prefab instance"
                    // as their prefab type, so it is important that it is the root that is selected.
                    //
                    // e.g. If I have the following hierarchy: 
                    //      Cube
                    //      -- Sphere
                    //
                    // Both the Cube and Sphere will have ModelPrefabInstance as their prefab type.
                    // However, when selecting the Sphere to convert, we don't want to connect it to the
                    // existing FBX but create a new FBX containing just the sphere.
                    PrefabType unityPrefabType = PrefabUtility.GetPrefabType(go);
                    if (unityPrefabType == PrefabType.ModelPrefabInstance && go.Equals(PrefabUtility.FindPrefabRoot(go))) {
                        // don't re-export fbx
                        // create prefab out of model instance in scene, link to existing fbx
                        var mainAsset = PrefabUtility.GetPrefabParent(go) as GameObject;
                        var mainAssetRelPath = AssetDatabase.GetAssetPath(mainAsset);
                        var mainAssetAbsPath = System.IO.Directory.GetParent(Application.dataPath) + "/" + mainAssetRelPath;
                        var relPrefabPath = ExportSettings.GetProjectRelativePath (prefabPath);

                        if (string.Equals(System.IO.Path.GetFullPath(fbxPath), System.IO.Path.GetFullPath(mainAssetAbsPath))) {
                            ConvertToModel.SetupFbxPrefab(go, mainAsset, relPrefabPath, mainAssetAbsPath);
                            return;
                        }
                    }

                    // check if file already exists, give a warning if it does
                    if (!OverwriteExistingFile (fbxPath)) {
                        return;
                    }

                    ConvertToModel.Convert (
                        go, fbxFullPath: fbxPath, prefabFullPath: prefabPath, exportOptions: ExportSettings.instance.convertToPrefabSettings.info
                    );
                    return;
                }

                foreach (var go in m_toConvert) {
                    ConvertToModel.Convert (
                        go, fbxDirectoryFullPath: fbxDirPath, prefabDirectoryFullPath: prefabDirPath, exportOptions: ExportSettings.instance.convertToPrefabSettings.info
                    );
                }
            }

            protected override bool DisableNameSelection ()
            {
                return m_toConvert.Length > 1;
            }

            protected override void ShowPresetReceiver ()
            {
                ShowPresetReceiver (ExportSettings.instance.convertToPrefabSettings);
            }

            protected override void CreateCustomUI ()
            {
                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Prefab Name:",
                    "Filename to save prefab to."),GUILayout.Width(LabelWidth-TextFieldAlignOffset));

                EditorGUI.BeginDisabledGroup (DisableNameSelection());
                // Show the export name with an uneditable ".prefab" at the end
                //-------------------------------------
                EditorGUILayout.BeginVertical ();
                EditorGUILayout.BeginHorizontal(EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUI.indentLevel--;
                // continually resize to contents
                var textFieldSize = m_nameTextFieldStyle.CalcSize (new GUIContent(m_prefabFileName));
                m_prefabFileName = EditorGUILayout.TextField (m_prefabFileName, m_nameTextFieldStyle, GUILayout.Width(textFieldSize.x + 5), GUILayout.MinWidth(5));
                m_prefabFileName = ModelExporter.ConvertToValidFilename (m_prefabFileName);

                EditorGUILayout.LabelField ("<color=#808080ff>.prefab</color>", m_fbxExtLabelStyle, GUILayout.Width(m_prefabExtLabelWidth));
                EditorGUI.indentLevel++;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical ();
                //-----------------------------------
                EditorGUI.EndDisabledGroup ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "Prefab Path:",
                    "Relative path for saving Linked Prefabs."),GUILayout.Width(LabelWidth - FieldOffset));

                var pathLabels = ExportSettings.GetRelativePrefabSavePaths();

                ExportSettings.instance.selectedPrefabPath = EditorGUILayout.Popup (ExportSettings.instance.selectedPrefabPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

                if (GUILayout.Button(new GUIContent("...", "Browse to a new location to save prefab to"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
                {
                    string initialPath = Application.dataPath;

                    string fullPath = EditorUtility.OpenFolderPanel(
                        "Select Linked Prefab Save Path", initialPath, null
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
                            ExportSettings.AddPrefabSavePath(relativePath);

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
