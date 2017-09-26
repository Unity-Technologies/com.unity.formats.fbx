using UnityEngine;
using UnityEditor;

namespace FbxExporters.EditorTools {

    [CustomEditor(typeof(FbxPrefab))]
    public class FbxPrefabInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
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
            var newFbxAsset = EditorGUILayout.ObjectField("Source Fbx Asset", oldFbxAsset,
                    typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (newFbxAsset && !AssetDatabase.GetAssetPath(newFbxAsset).EndsWith(".fbx")) {
                Debug.LogError("FbxPrefab must point to an Fbx asset (or none).");
            } else if (newFbxAsset != oldFbxAsset) {
                fbxPrefabUtility.SetSourceModel(newFbxAsset);
            }

            EditorGUI.EndDisabledGroup();

#if FBXEXPORTER_DEBUG
            GUILayout.BeginHorizontal();
            GUILayout.Label ("Debug info:");
            try {
                fbxPrefab.GetFbxHistory().ToJson();
            } catch(System.Exception xcp) {
                Debug.LogException(xcp);
            }
            EditorGUILayout.SelectableLabel(fbxPrefab.GetFbxHistoryString());
            GUILayout.EndHorizontal();
#endif
        }
    }
}
