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
        }
    }
}
#endif
