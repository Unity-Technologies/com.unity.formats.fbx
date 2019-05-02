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
                return (GetToExport() != null && GetToExport().Length > 1);
            }
        }
        protected override bool DisableTransferAnim
        {
            get
            {
                return GetToExport() == null || GetToExport().Length > 1;
            }
        }

        public static void Init(IEnumerable<GameObject> toConvert)
        {
            ConvertToPrefabEditorWindow window = CreateWindow<ConvertToPrefabEditorWindow>();
            window.InitializeWindow();
            window.SetGameObjectsToConvert(toConvert);
            window.Show();
        }

        protected void SetGameObjectsToConvert(IEnumerable<GameObject> toConvert)
        {
            SetToExport(toConvert.OrderBy(go => go.name).ToArray());

            TransferAnimationSource = null;
            TransferAnimationDest = null;

            string fbxFileName = null;
            if (GetToExport().Length == 1)
            {
                var go = ModelExporter.GetGameObject(GetToExport()[0]);
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
            else if (GetToExport().Length > 1)
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
                InnerEditor = UnityEditor.Editor.CreateEditor(ExportSettings.instance.ConvertToPrefabSettings);
            }
            m_prefabExtLabelWidth = FbxExtLabelStyle.CalcSize(new GUIContent(".prefab")).x;
        }

        /// <summary>
        /// Get a list of all the export set objects that contain
        /// RectTransforms or have children with RectTransforms.
        /// </summary>
        /// <param name="uiObjectNames">names of objects in set which contain RectTransforms</param>
        /// <returns>Whethere there are any UI elements in the export set</returns>
        protected bool GetUIElementsInExportSet(out List<string> uiObjectNames)
        {
            uiObjectNames = new List<string>();
            foreach (var obj in GetToExport())
            {
                var go = ModelExporter.GetGameObject(obj);
                if (go.GetComponentInChildren<RectTransform>())
                {
                    uiObjectNames.Add(go.name);
                }
            }
            return uiObjectNames.Count > 0;
        }

        protected bool ExportSetContainsAnimation()
        {
            foreach (var obj in GetToExport())
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

            var fbxDirPath = ExportSettings.FbxAbsoluteSavePath;
            var fbxPath = System.IO.Path.Combine(fbxDirPath, ExportFileName + ".fbx");

            var prefabDirPath = ExportSettings.PrefabAbsoluteSavePath;
            var prefabPath = System.IO.Path.Combine(prefabDirPath, m_prefabFileName + ".prefab");

            if (GetToExport() == null)
            {
                Debug.LogError("FbxExporter: missing object for conversion");
                return false;
            }

            List<string> hierarchiesWithUI;
            if (GetUIElementsInExportSet(out hierarchiesWithUI))
            {
                // Warn that UI elements will break if converted
                string warning = string.Format("RectTransform and other UI components will be lost if the following GameObject hierarchies are converted:\n\n{0}\n",
                    string.Join("\n", hierarchiesWithUI));
                bool result = UnityEditor.EditorUtility.DisplayDialog(
                    string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), warning, "Convert and lose UI", "Cancel");

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

            if (GetToExport().Length == 1)
            {
                var go = ModelExporter.GetGameObject(GetToExport()[0]);

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
                    go, fbxFullPath: fbxPath, prefabFullPath: prefabPath, exportOptions: ExportSettings.instance.ConvertToPrefabSettings.info
                );
                return true;
            }

            bool onlyPrefabAssets = ConvertToNestedPrefab.SetContainsOnlyPrefabAssets(GetToExport());
            int groupIndex = -1;
            // no need to undo if we aren't converting anything that's in the scene
            if (!onlyPrefabAssets)
            {
                Undo.IncrementCurrentGroup();
                groupIndex = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(ConvertToNestedPrefab.UndoConversionCreateObject);
            }
            foreach (var obj in GetToExport())
            {
                // Convert, automatically choosing a file path that won't clobber any existing files.
                var go = ModelExporter.GetGameObject(obj);
                ConvertToNestedPrefab.Convert(
                    go, fbxDirectoryFullPath: fbxDirPath, prefabDirectoryFullPath: prefabDirPath, exportOptions: ExportSettings.instance.ConvertToPrefabSettings.info
                );
            }
            if (!onlyPrefabAssets && groupIndex >= 0)
            {
                Undo.CollapseUndoOperations(groupIndex);
                Undo.IncrementCurrentGroup();
            }
            return true;
        }

        protected override ExportOptionsSettingsSerializeBase SettingsObject
        {
            get { return ExportSettings.instance.ConvertToPrefabSettings.info; }
        }
#if UNITY_2018_1_OR_NEWER
        protected override void ShowPresetReceiver()
        {
            ShowPresetReceiver(ExportSettings.instance.ConvertToPrefabSettings);
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
                "Relative path for saving Linked Prefabs."), GUILayout.Width(LabelWidth - FieldOffset));

            var pathLabels = ExportSettings.GetRelativePrefabSavePaths();

            ExportSettings.instance.SelectedPrefabPath = EditorGUILayout.Popup(ExportSettings.instance.SelectedPrefabPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

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

        protected override void DoNotShowDialogUI()
        {
            EditorGUI.indentLevel--;
            ExportSettings.instance.ShowConvertToPrefabDialog = !EditorGUILayout.Toggle(
                new GUIContent("Don't ask me again", "Don't ask me again, use the last used paths and options instead"),
                !ExportSettings.instance.ShowConvertToPrefabDialog
            );
        }
    }
}