using System.Collections.Generic;
using UnityEngine;

namespace FbxExporters
{
    /// <summary>
    /// This component is applied to a prefab. It keeps the prefab sync'd up
    /// with an FBX file.
    ///
    /// On its own it does nothing; you also need the FbxPostImporterPrefabUpdater.
    /// </summary>
    [System.Serializable]
    public class FbxSource : MonoBehaviour
    {
        //////////////////////////////////////////////////////////////////////
        // TODO: Fields included in editor must be included in player, or it doesn't
        // build.

        /// <summary>
        /// Representation of the FBX file as it was when the prefab was
        /// last saved. This lets us update the prefab when the FBX changes.
        /// </summary>
        [SerializeField] // [HideInInspector]
        string m_fbxHistory;

        /// <summary>
        /// Which FBX file does this refer to?
        /// </summary>
        [SerializeField]
        GameObject m_fbxModel;

        //////////////////////////////////////////////////////////////////////
        // None of the code should be included in the build, because this
        // component is really only about the editor.
#if UNITY_EDITOR
        string SerializeFbx()
        {
            Debug.Log("SerializeFbx on " + m_fbxModel.name);
            return "";
        }

        /// <summary>
        /// Returns whether the model we're tracking is the same asset as the
        /// path passed in.
        /// </summary>
        public bool MatchesFbxFile(string pathname)
        {
            Debug.Log("trying to match " + pathname + " to " + m_fbxModel.name);
            if (!m_fbxModel) { return false; }
            var fbxpath = UnityEditor.AssetDatabase.GetAssetPath(m_fbxModel);
            return fbxpath == pathname;
        }

        /// <summary>
        /// Sync the prefab to match the newly-imported FBX file.
        /// </summary>
        public void SyncPrefab()
        {
            Debug.Log("SyncPrefab on " + m_fbxModel.name + " => " + this.name);
            m_fbxHistory = SerializeFbx();
        }
#endif
    }
}
