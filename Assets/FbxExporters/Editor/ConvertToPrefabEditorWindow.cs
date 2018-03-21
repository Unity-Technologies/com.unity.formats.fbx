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
        public class ConvertToPrefabEditorWindow : ExportOptionsEditorWindow
        {
            protected override GUIContent WindowTitle { get { return new GUIContent ("Convert Options"); }}
            protected override float MinWindowHeight { get { return 350; } } // determined by trial and error
            protected override string ExportButtonName { get { return "Convert"; } }
            private string m_prefabFileName = "";

            private float m_prefabExtLabelWidth;

            protected override bool DisableNameSelection {
                get {
                    return (ToExport != null && ToExport.Length > 1);
                }
            }
            protected override bool DisableTransferAnim {
                get {
                    return ToExport == null || ToExport.Length > 1;
                }
            }

            protected override bool GameObjectIsModel {
                get {
                    return ConvertToModel.IsModel(ModelExporter.GetGameObject (ToExport [0]));
                }
            }

            public static void Init (IEnumerable<GameObject> toConvert)
            {
                ConvertToPrefabEditorWindow window = CreateWindow<ConvertToPrefabEditorWindow> ();
                window.InitializeWindow ();
                window.SetGameObjectsToConvert (toConvert);
                window.Show ();
            }

            protected void SetGameObjectsToConvert(IEnumerable<GameObject> toConvert){
                ToExport = toConvert.OrderBy (go => go.name).ToArray ();

                TransferAnimationSource = null;
                TransferAnimationDest = null;
                if (ToExport.Length == 1) {
                    var go = ModelExporter.GetGameObject (ToExport [0]);
                    // check if the GameObject is a model instance, use as default filename and path if it is
                    if(ConvertToModel.IsModelInstance(go)) {
                        var mainAsset = PrefabUtility.GetPrefabParent (go) as GameObject;
                        var mainAssetRelPath = AssetDatabase.GetAssetPath (mainAsset);
                        // remove Assets/ from beginning of path
                        mainAssetRelPath = mainAssetRelPath.Substring ("Assets".Length);

                        m_prefabFileName = System.IO.Path.GetFileNameWithoutExtension (mainAssetRelPath);
                        ExportSettings.AddFbxSavePath (System.IO.Path.GetDirectoryName (mainAssetRelPath));
                    }
                    else{
                        m_prefabFileName = ToExport [0].name;
                    }

                    // if only one object selected, set transfer source/dest to this object
                    if (go) {
                        TransferAnimationSource = go.transform;
                        TransferAnimationDest = go.transform;
                    }
                } else if (ToExport.Length > 1) {
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

            protected override bool Export ()
            {
                if (string.IsNullOrEmpty (m_exportFileName)) {
                    Debug.LogError ("FbxExporter: Please specify an fbx filename");
                    return false;
                }

                if (string.IsNullOrEmpty (m_prefabFileName)) {
                    Debug.LogError ("FbxExporter: Please specify a prefab filename");
                    return false;
                }

                var fbxDirPath = ExportSettings.GetFbxAbsoluteSavePath ();
                var fbxPath = System.IO.Path.Combine (fbxDirPath, m_exportFileName + ".fbx");

                var prefabDirPath = ExportSettings.GetPrefabAbsoluteSavePath ();
                var prefabPath = System.IO.Path.Combine (prefabDirPath, m_prefabFileName + ".prefab");

                if (ToExport == null) {
                    Debug.LogError ("FbxExporter: missing object for conversion");
                    return false;
                }

                if (ToExport.Length == 1) {
                    var go = ModelExporter.GetGameObject (ToExport [0]);

                    if (!OverwriteExistingFile (prefabPath)) {
                        return false;
                    }

                    // Only create the prefab (no FBX export) if we have selected the root of a model prefab instance.
                    if(ConvertToModel.IsModelInstance(go)) {
                        // don't re-export fbx
                        // create prefab out of model instance in scene, link to existing fbx
                        var mainAsset = PrefabUtility.GetPrefabParent(go) as GameObject;
                        var mainAssetRelPath = AssetDatabase.GetAssetPath(mainAsset);
                        var mainAssetAbsPath = System.IO.Directory.GetParent(Application.dataPath) + "/" + mainAssetRelPath;
                        var relPrefabPath = ExportSettings.GetProjectRelativePath (prefabPath);

                        if (string.Equals(System.IO.Path.GetFullPath(fbxPath), System.IO.Path.GetFullPath(mainAssetAbsPath))) {
                            ConvertToModel.SetupFbxPrefabOnGameObject(go, mainAsset, relPrefabPath, mainAssetAbsPath);
                            return true;
                        }
                    }

                    // check if file already exists, give a warning if it does
                    if (!OverwriteExistingFile (fbxPath)) {
                        return false;
                    }

                    ConvertToModel.Convert (
                        go, fbxFullPath: fbxPath, prefabFullPath: prefabPath, exportOptions: ExportSettings.instance.convertToPrefabSettings.info
                    );
                    return true;
                }

                foreach (var obj in ToExport) {
                    var go = ModelExporter.GetGameObject (obj);
                    ConvertToModel.Convert (
                        go, fbxDirectoryFullPath: fbxDirPath, prefabDirectoryFullPath: prefabDirPath, exportOptions: ExportSettings.instance.convertToPrefabSettings.info
                    );
                }
                return true;
            }

            protected override ExportOptionsSettingsSerializeBase SettingsObject
            {
                get { return ExportSettings.instance.convertToPrefabSettings.info; }
            }
            #if UNITY_2018_1_OR_NEWER
            protected override void ShowPresetReceiver ()
            {
                ShowPresetReceiver (ExportSettings.instance.convertToPrefabSettings);
            }
            #endif
            protected override void CreateCustomUI ()
            {
                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Prefab Name",
                    "Filename to save prefab to."),GUILayout.Width(LabelWidth-TextFieldAlignOffset));

                EditorGUI.BeginDisabledGroup (DisableNameSelection);
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
                    "Prefab Path",
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
