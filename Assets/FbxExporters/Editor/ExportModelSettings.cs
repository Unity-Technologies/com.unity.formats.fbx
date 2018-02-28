using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExportModelSettings))]
public class ExportModelSettingsEditor : UnityEditor.Editor {

    public override void OnInspectorGUI() {
        var exportSettings = (ExportModelSettings)target;

        exportSettings.info.test = EditorGUILayout.TextField (exportSettings.info.test);
    }
}

public class ExportModelSettings : ScriptableObject {
    public ExportModelSettingsSerialize info;

    public ExportModelSettings(){
        info = new ExportModelSettingsSerialize ();
    }
}

[System.Serializable]
public class ExportModelSettingsSerialize {
    public string test = "hi";

    public ExportModelSettingsSerialize(){
        test = "hello";
    }
}