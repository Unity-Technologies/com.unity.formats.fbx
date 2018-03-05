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
        public class ConvertToPrefabEditorWindow : ExportOptionsEditorWindow
        {
            protected override GUIContent m_windowTitle { get { return new GUIContent ("Convert Options"); }}
            private GameObject m_toConvert;

            public static void Init (GameObject toConvert)
            {
                ConvertToPrefabEditorWindow window = CreateWindow<ConvertToPrefabEditorWindow> ();
                window.InitializeWindow (filename: toConvert.name, singleHierarchyExport: true, exportType: ModelExporter.AnimationExportType.all);
                window.SetGameObjectToConvert (toConvert);
                window.Show ();
            }

            public void SetGameObjectToConvert(GameObject toConvert){
                m_toConvert = toConvert;
            }

            protected override void OnEnable ()
            {
                base.OnEnable ();

                if (!m_innerEditor) {
                    var ms = ExportSettings.instance.convertToPrefabSettings;
                    if (!ms) {
                        ExportSettings.LoadSettings ();
                        ms = ExportSettings.instance.convertToPrefabSettings;
                    }
                    m_innerEditor = UnityEditor.Editor.CreateEditor (ms);
                }
            }

            protected override void Export ()
            {
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

                if (!m_toConvert) {
                    Debug.LogError ("FbxExporter: missing object for conversion");
                }
                ConvertToModel.Convert (m_toConvert, fbxFullPath: filePath);
            }

            protected override void CreateCustomUI ()
            {
                base.CreateCustomUI ();
            }
        }	
	}
}
