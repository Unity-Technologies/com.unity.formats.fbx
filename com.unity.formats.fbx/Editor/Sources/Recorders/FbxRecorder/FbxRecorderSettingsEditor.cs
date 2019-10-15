using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(AnimationRecorderSettings))]
    class FbxRecorderSettingsEditor : RecorderEditor
    {
        protected override void FileTypeAndFormatGUI()
        {
            EditorGUILayout.LabelField("Format", "Fbx");
        }
    }
}
