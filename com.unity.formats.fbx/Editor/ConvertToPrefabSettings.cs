﻿using UnityEngine;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor (typeof(ConvertToPrefabSettings))]
    internal class ConvertToPrefabSettingsEditor : UnityEditor.Editor
    {
        private const float DefaultLabelWidth = 175;
        private const float DefaultFieldOffset = 18;

        public float LabelWidth { get; set; } = DefaultLabelWidth;
        public float FieldOffset { get; set; } = DefaultFieldOffset;

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
            exportSettings.SetExportFormat((ExportSettings.ExportFormat)EditorGUILayout.Popup((int)exportSettings.ExportFormat, exportFormatOptions));
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

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Animated Skinned Mesh"), GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetAnimatedSkinnedMesh(EditorGUILayout.Toggle (exportSettings.AnimateSkinnedMesh));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Compatible Naming",
                    "In Maya some symbols such as spaces and accents get replaced when importing an FBX " +
                    "(e.g. \"foo bar\" becomes \"fooFBXASC032bar\"). " +
                    "On export, convert the names of GameObjects so they are Maya compatible." +
                    (exportSettings.UseMayaCompatibleNames ? "" :
                        "\n\nWARNING: Disabling this feature may result in lost material connections," +
                        " and unexpected character replacements in Maya.")),
                    GUILayout.Width(LabelWidth - FieldOffset));
            exportSettings.SetUseMayaCompatibleNames(EditorGUILayout.Toggle (exportSettings.UseMayaCompatibleNames));
            GUILayout.EndHorizontal();
        }
    }

    internal class ConvertToPrefabSettings : ExportOptionsSettingsBase<ConvertToPrefabSettingsSerialize>
    {}

    [System.Serializable]
    internal class ConvertToPrefabSettingsSerialize : ExportOptionsSettingsSerializeBase
    {
        public override ExportSettings.Include ModelAnimIncludeOption { get { return ExportSettings.Include.ModelAndAnim; } }
        public override ExportSettings.LODExportType LODExportType { get { return ExportSettings.LODExportType.All; } }
        public override ExportSettings.ObjectPosition ObjectPosition { get { return ExportSettings.ObjectPosition.Reset; } }
        public override bool ExportUnrendered { get { return true; } }
        public override bool AllowSceneModification { get { return true; } }
    }
}