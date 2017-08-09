using UnityEngine;
using UnityEditor;

namespace FbxExporters.EditorTools {

    [CustomEditor(typeof(FbxPrefab))]
    public class FbxPrefabInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            FbxPrefab fbxPrefab = (FbxPrefab)target;

            fbxPrefab.SetAutoUpdate(EditorGUILayout.Toggle ("Auto-update:", fbxPrefab.WantsAutoUpdate()));
            if (!fbxPrefab.WantsAutoUpdate()) {
                if (GUILayout.Button("Sync prefab to FBX")) {
                    fbxPrefab.SyncPrefab();
                }
            }

            var oldFbxAsset = fbxPrefab.GetFbxAsset();
            var newFbxAsset = EditorGUILayout.ObjectField("Source Fbx Asset", oldFbxAsset,
                    typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (newFbxAsset && !AssetDatabase.GetAssetPath(newFbxAsset).EndsWith(".fbx")) {
                Debug.LogError("FbxPrefab must point to an Fbx asset (or none).");
            } else if (newFbxAsset != oldFbxAsset) {
                fbxPrefab.SetSourceModel(newFbxAsset);
            }

#if FBXEXPORTER_DEBUG
            GUILayout.BeginHorizontal();
            GUILayout.Label ("Debug info:");
            EditorGUILayout.SelectableLabel(fbxPrefab.GetFbxHistory().ToJson());
            GUILayout.EndHorizontal();
#endif
        }
    }
}
