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

        string SerializeFbx()
        {
            Debug.Log("SerializeFbx on " + m_fbxModel.name);
            return "";
        }

        /// <summary>
        /// Sync the prefab to match the newly-imported FBX file.
        /// </summary>
        public void SyncPrefab(GameObject prefab)
        {
            Debug.Log("SyncPrefab on " + m_fbxModel.name + " => " + prefab.name);
            m_fbxHistory = SerializeFbx();
        }
    }
}
