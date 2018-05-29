using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Formats.Fbx.Exporter
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

        [System.Serializable]
        public struct StringPair { public string FBXObjectName; public string UnityObjectName; }

        [SerializeField]
        List<StringPair> m_nameMapping = new List<StringPair>();

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

        public List<StringPair> NameMapping
        {
            get
            {
                return m_nameMapping;
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

        //////////////////////////////////////////////////////////////////////////
        // Event handling for updates.

        /// <summary>
        /// Handler for an OnUpdate event.
        ///
        /// The update is performed on a temporary instance, which, shortly after
        /// this handler is invoked, will be applied to the prefab.
        ///
        /// The event handler can make changes to any objects in the hierarchy rooted
        /// by the updatedInstance. Those changes will be applied to the prefab.
        ///
        /// The updatedObjects include all objects in the temporary instance
        /// that were:
        /// - created, or
        /// - changed parent, or
        /// - had a component that was created, destroyed, or updated.
        /// There is no notification for entire objects that were destroyed.
        /// </summary>
        public delegate void HandleUpdate(FbxPrefab updatedInstance, IEnumerable<GameObject> updatedObjects);

        /// <summary>
        /// OnUpdate is raised once when an FbxPrefab gets updated, after all the changes
        /// have been done.
        /// </summary>
        public static event HandleUpdate OnUpdate;

        /// <summary>
        /// Notify listeners that they're free to make adjustments. 
        /// This will be called after the FbxPrefab auto updater has completed it's work.
        /// </summary>
        /// <param name="instance">Updated FbxPrefab instance.</param>
        /// <param name="updatedObjects">Updated objects.</param>
        public static void CallOnUpdate(FbxPrefab instance, IEnumerable<GameObject> updatedObjects){
            if (OnUpdate != null) {
                OnUpdate (instance, updatedObjects);
            }
        }
    }
}
