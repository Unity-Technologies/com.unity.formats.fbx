using UnityEditor;
using UnityEngine;

namespace FbxExporters.EditorTools
{
    [CustomEditor (typeof(ExportModelSettings))]
    public class ExportModelSettingsEditor : UnityEditor.Editor
    {
        private const float LabelWidth = 175;
        private const float FieldOffset = 18;

        public override void OnInspectorGUI ()
        {
            var exportSettings = ((ExportModelSettings)target).info;

            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.exportFormat = (ExportModelSettingsSerialize.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, new string[]{ "ASCII", "Binary" });
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.include = (ExportModelSettingsSerialize.Include)EditorGUILayout.Popup((int)exportSettings.include, new string[]{"Model(s) Only", "Animation Only", "Model(s) + Animation"});
            GUILayout.EndHorizontal();

            // greyed out if animation only
            EditorGUI.BeginDisabledGroup(exportSettings.include == ExportModelSettingsSerialize.Include.Anim);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.lodLevel = (ExportSettings.LODExportType)EditorGUILayout.Popup((int)exportSettings.lodLevel, new string[]{"All", "Highest", "Lowest"});
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.objectPosition = (ExportModelSettingsSerialize.ObjectPosition)EditorGUILayout.Popup((int)exportSettings.objectPosition, new string[]{"Local Centered", "World Absolute", "Local Pivot"});
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup ();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Transfer Root Motion To", "Select bone to transfer root motion animation to."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUILayout.Popup(0, new string[]{"<None>"});
            GUILayout.EndHorizontal();

            exportSettings.animatedSkinnedMesh = EditorGUILayout.Toggle ("Animated Skinned Mesh", exportSettings.animatedSkinnedMesh);
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