using UnityEngine;
using UnityEditor;

namespace FbxExporters.EditorTools {

    [CustomEditor(typeof(FbxPrefab))]
    public class FbxPrefabInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {

            SerializedProperty m_GameObjectProp = serializedObject.FindProperty("m_nameMapping");

            FbxPrefab fbxPrefab = (FbxPrefab)target;

            // We can only change these settings when applied to a prefab.
            bool isDisabled = AssetDatabase.GetAssetPath(fbxPrefab) == "";
            if (isDisabled) {
                EditorGUILayout.HelpBox("Please select a prefab. You can't edit an instance in the scene.",
                        MessageType.Info);
            }
            EditorGUI.BeginDisabledGroup(isDisabled);

            var fbxPrefabUtility = new FbxPrefabAutoUpdater.FbxPrefabUtility (fbxPrefab);
            var oldFbxAsset = fbxPrefabUtility.GetFbxAsset();
            var newFbxAsset = EditorGUILayout.ObjectField(new GUIContent("Source Fbx Asset", "The FBX file that is linked to this Prefab"), oldFbxAsset,
                    typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (newFbxAsset && !AssetDatabase.GetAssetPath(newFbxAsset).EndsWith(".fbx")) {
                Debug.LogError("FbxPrefab must point to an Fbx asset (or none).");
            } else if (newFbxAsset != oldFbxAsset) {
                fbxPrefabUtility.SetSourceModel(newFbxAsset);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_GameObjectProp, true);

            if (GUILayout.Button("Update prefab manually..."))
            {
                // Get existing open window or if none, make a new one:
                ManualUpdateEditorWindow window = (ManualUpdateEditorWindow)EditorWindow.GetWindow(typeof(ManualUpdateEditorWindow));
                window.Show();
            }


#if FBXEXPORTER_DEBUG
    EditorGUILayout.LabelField ("Debug info:");
            try {
                fbxPrefabUtility.GetFbxHistory().ToJson();
            } catch(System.Exception xcp) {
                Debug.LogException(xcp);
            }
            EditorGUILayout.SelectableLabel(fbxPrefabUtility.GetFbxHistoryString());
#endif
            serializedObject.ApplyModifiedProperties();
        }
    }
}