using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace FbxExporters
{
    /// <summary>
    /// This component is applied to a prefab. It keeps the prefab sync'd up
    /// with an FBX file.
    ///
    /// Other parts of the ecosystem:
    ///         FbxPrefabInspector
    ///         FbxPrefabAutoUpdater
    /// </summary>
    public class FbxPrefab : MonoBehaviour
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
        [Tooltip("Which FBX file does this refer to?")]
        GameObject m_fbxModel;

        /// <summary>
        /// Should we auto-update this prefab when the FBX file is updated?
        /// <summary>
        [Tooltip("Should we auto-update this prefab when the FBX file is updated?")]
        [SerializeField]
        bool m_autoUpdate = true;

        public string FbxHistory {
            get{
                return m_fbxHistory;
            }
            set{
                m_fbxHistory = value;
            }
        }

        public GameObject FbxModel {
            get{
                return m_fbxModel;
            }
            set{
                m_fbxModel = value;
            }
        }

        public bool AutoUpdate {
            get{
                return m_autoUpdate;
            }
            set{
                m_autoUpdate = value;
            }
        }
    }
}
