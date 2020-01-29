#if COM_UNITY_RECORDER
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

        protected override void OnEncodingGui()
        {
            base.OnEncodingGui();

            DrawSeparator();

            EditorGUILayout.LabelField(new GUIContent(
                "Transfer Animation",
                "Transfer transform animation from source to destination. Animation on objects between source and destination will also be transferred to destination."
            ));

            FbxRecorderSettings settings = target as FbxRecorderSettings;

            settings.TransferAnimationSource = EditorGUILayout.ObjectField("Source", settings.TransferAnimationSource, typeof(Transform), allowSceneObjects: true) as Transform;
            settings.TransferAnimationDest = EditorGUILayout.ObjectField("Destination", settings.TransferAnimationDest, typeof(Transform), allowSceneObjects: true) as Transform;
        }
    }
}
#endif
