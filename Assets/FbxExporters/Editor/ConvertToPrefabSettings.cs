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
            exportSettings.exportFormat = (ExportSettings.ExportFormat)EditorGUILayout.Popup((int)exportSettings.exportFormat, exportFormatOptions);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Include", "Select whether to export models, animation or both."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, includeOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("LOD level", "Select which LOD to export."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, lodOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Object(s) Position", "Select an option for exporting object's transform."), GUILayout.Width(LabelWidth - FieldOffset));
            // always greyed out, show only to let user know what will happen
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Popup(0, objPositionOptions);
            EditorGUI.EndDisabledGroup ();
            GUILayout.EndHorizontal();

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

    public class ConvertToPrefabSettings : ExportOptionsSettingsBase<ConvertToPrefabSettingsSerialize>
    {}

    [System.Serializable]
    public class ConvertToPrefabSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        public override ExportSettings.Include ModelAnimIncludeOption { get { return ExportSettings.Include.ModelAndAnim; } set { } }
        public override ExportSettings.LODExportType LODExportType { get { return ExportSettings.LODExportType.All; } set { } }
        public override ExportSettings.ObjectPosition ObjectPosition { get { return ExportSettings.ObjectPosition.Reset; } set { } }
        public override bool ExportUnrendered { get { return true; } set { } }
    }
}