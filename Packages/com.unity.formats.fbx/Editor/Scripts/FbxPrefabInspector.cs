using UnityEngine;
using UnityEditor;
using UnityEngine.Formats.Fbx.Exporter;
using System.Security.Permissions;

namespace UnityEditor.Formats.Fbx.Exporter {

    [CustomEditor(typeof(FbxPrefab))]
    internal class FbxPrefabInspector : UnityEditor.Editor {
        [SecurityPermission(SecurityAction.LinkDemand)]
        public override void OnInspectorGUI() {

            SerializedProperty m_GameObjectProp = serializedObject.FindProperty("m_nameMapping");

            FbxPrefab fbxPrefab = (FbxPrefab)target;

            // We can only change these settings when applied to a prefab.
            bool isDisabled = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fbxPrefab));
            if (isDisabled) {
                EditorGUILayout.HelpBox("Please select a prefab. You can't edit an instance in the scene.",
                        MessageType.Info);
            }

            EditorGUI.BeginDisabledGroup(isDisabled);
            FbxPrefabUtility fbxPrefabUtility = new FbxPrefabUtility (fbxPrefab);
            var oldFbxAsset = fbxPrefabUtility.FbxAsset;
            var newFbxAsset = EditorGUILayout.ObjectField(new GUIContent("Source FBX Asset", "The FBX file that is linked to this Prefab"), oldFbxAsset,
                    typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (newFbxAsset && !FbxPrefabAutoUpdater.IsFbxAsset(UnityEditor.AssetDatabase.GetAssetPath(newFbxAsset))) {
                Debug.LogError("FbxPrefab must point to an FBX asset (or none).");
            } else if (newFbxAsset != oldFbxAsset) {
                fbxPrefabUtility.SetSourceModel(newFbxAsset);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(m_GameObjectProp, true);

#if FBXEXPORTER_DEBUG
            if (GUILayout.Button("Update prefab manually..."))
            {
                // Get existing open window or if none, make a new one:
                ManualUpdateEditorWindow window = (ManualUpdateEditorWindow)EditorWindow.GetWindow(typeof(ManualUpdateEditorWindow));
                window.Init(fbxPrefabUtility, fbxPrefab);
                window.Show();
            }

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