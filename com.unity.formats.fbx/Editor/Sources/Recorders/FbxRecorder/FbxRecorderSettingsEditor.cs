using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor;

namespace UnityEditor.Formats.Fbx.Exporter
{
    [CustomEditor(typeof(FbxRecorderSettings))]
    class FbxRecorderSettingsEditor : RecorderEditor
    {
        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.LabelField("Format", "FBX");

            FbxRecorderSettings settings = target as FbxRecorderSettings;

            settings.ExportGeometry = EditorGUILayout.Toggle("Export Geometry", settings.ExportGeometry);
        }
    }
}
