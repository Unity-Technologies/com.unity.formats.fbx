using UnityEditor;
using UnityEngine;

namespace FbxExporters.EditorTools
{
    [CustomEditor (typeof(ExportModelSettings))]
    public class ExportModelSettingsEditor : UnityEditor.Editor
    {
        private const float LabelWidth = 144;
        private const float FieldOffset = 18;

        public override void OnInspectorGUI ()
        {
            var exportSettings = ((ExportModelSettings)target).info;

            //exportSettings.info.test = EditorGUILayout.TextField (exportSettings.info.test);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format:", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.exportFormat = (ExportModelSettingsSerialize.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, new string[]{ "ASCII", "Binary" });
            GUILayout.EndHorizontal();
        }
    }

    public class ExportModelSettings : ScriptableObject
    {
        public ExportModelSettingsSerialize info;

        public ExportModelSettings ()
        {
            info = new ExportModelSettingsSerialize ();
        }
    }

    [System.Serializable]
    public class ExportModelSettingsSerialize
    {
        public enum ExportFormat { ASCII = 0, Binary = 1}

        public enum Include { Model = 0, Anim = 1, ModelAndAnim = 2 }

        public enum ObjectPosition { LocalCentered = 0, WorldAbsolute = 1, LocalPivot = 2 }

        public ExportFormat exportFormat = ExportFormat.ASCII;
        public Include include = Include.ModelAndAnim;
        public ExportSettings.LODExportType lodLevel = ExportSettings.LODExportType.All;
        public ObjectPosition objectPosition = ObjectPosition.LocalCentered;
        public string rootMotionTransfer = "";
        public bool animatedSkinnedMesh = true;
    }
}