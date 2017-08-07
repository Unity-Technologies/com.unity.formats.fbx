using UnityEngine;
using UnityEditor;

namespace FbxExporters.EditorTools {

    [CustomEditor(typeof(FbxSource))]
    public class FbxSourceInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            FbxSource fbxSource = (FbxSource)target;

            fbxSource.SetAutoUpdate(EditorGUILayout.Toggle ("Auto-update:", fbxSource.WantsAutoUpdate()));
            if (!fbxSource.WantsAutoUpdate()) {
                if (GUILayout.Button("Sync prefab to FBX")) {
                    fbxSource.SyncPrefab();
                }
            }

            var oldFbxAsset = fbxSource.GetFbxAsset();
            var newFbxAsset = EditorGUILayout.ObjectField("Source Fbx Asset", oldFbxAsset,
                    typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (newFbxAsset && !AssetDatabase.GetAssetPath(newFbxAsset).EndsWith(".fbx")) {
                Debug.LogError("FbxSource must point to an Fbx asset (or none).");
            } else if (newFbxAsset != oldFbxAsset) {
                fbxSource.SetSourceModel(newFbxAsset);
            }

#if FBXEXPORTER_DEBUG
            GUILayout.BeginHorizontal();
            GUILayout.Label ("Debug info:");
            EditorGUILayout.SelectableLabel(fbxSource.GetFbxHistory().ToJson());
            GUILayout.EndHorizontal();
#endif
        }
    }
}
