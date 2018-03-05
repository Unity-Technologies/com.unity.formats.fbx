using UnityEditor;
using UnityEngine;

namespace FbxExporters.EditorTools
{
    [CustomEditor (typeof(ConvertToPrefabSettings))]
    public class ConvertToPrefabSettingsEditor : UnityEditor.Editor
    {
        private const float LabelWidth = 175;
        private const float FieldOffset = 18;

        private string[] exportFormatOptions = new string[]{ "ASCII", "Binary" };
        private string[] includeOptions = new string[]{"Model(s) + Animation"};
        private string[] lodOptions = new string[]{"All Levels"};

        private string[] objPositionOptions { get { return new string[]{"Local Pivot"}; }}

        public override void OnInspectorGUI ()
        {
            var exportSettings = ((ConvertToPrefabSettings)target).info;

            EditorGUIUtility.labelWidth = LabelWidth;

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Export Format", "Export the FBX file in the standard binary format." +
                " Select ASCII to export the FBX file in ASCII format."), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.exportFormat = (ExportModelSettingsSerialize.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, exportFormatOptions);
            GUILayout.EndHorizontal();

            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUILayout.Popup(0, includeOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUILayout.Popup(0, lodOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUILayout.Popup(0, objPositionOptions);
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup ();

            // TODO: add implementation for these options, grey out in the meantime
            EditorGUI.BeginDisabledGroup (true);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Transfer Root Motion To", "Select bone to transfer root motion animation to."), GUILayout.Width(LabelWidth - FieldOffset));
            EditorGUILayout.Popup(0, new string[]{"<None>"});
            GUILayout.EndHorizontal();

            exportSettings.animatedSkinnedMesh = EditorGUILayout.Toggle ("Animated Skinned Mesh", exportSettings.animatedSkinnedMesh);
            EditorGUI.EndDisabledGroup ();

            exportSettings.mayaCompatibleNaming = EditorGUILayout.Toggle (
                new GUIContent ("Compatible Naming:",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.mayaCompatibleNaming ? "" :
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                        " and unexpected character replacements in Maya.")
                ),
                exportSettings.mayaCompatibleNaming);
        }
    }

    public class ConvertToPrefabSettings : ScriptableObject
    {
        public ConvertToPrefabSettingsSerialize info;

        public ConvertToPrefabSettings ()
        {
            info = new ConvertToPrefabSettingsSerialize ();
        }
    }

    [System.Serializable]
    public class ConvertToPrefabSettingsSerialize
    {
        public ExportModelSettingsSerialize.ExportFormat exportFormat = ExportModelSettingsSerialize.ExportFormat.ASCII;
        public string rootMotionTransfer = "";
        public bool animatedSkinnedMesh = true;
        public bool mayaCompatibleNaming = true;
    }
}