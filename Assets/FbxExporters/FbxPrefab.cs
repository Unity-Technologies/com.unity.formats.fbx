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

        //////////////////////////////////////////////////////////////////////
        // None of the code should be included in the build, because this
        // component is really only about the editor.
#if UNITY_EDITOR
        public class FbxRepresentation
        {
            public Dictionary<string, FbxRepresentation> c;

            public static FbxRepresentation FromTransform(Transform xfo) {
                var c = new Dictionary<string, FbxRepresentation>();
                foreach(Transform child in xfo) {
                    c.Add(child.name, FromTransform(child));
                }
                var fbxrep = new FbxRepresentation();
                fbxrep.c = c;
                return fbxrep;
            }

            // todo: use a real json parser
            static bool Consume(char expected, string json, ref int index, bool required = true) {
                while (true) { // loop breaks if index == json.Length
                    switch(json[index]) {
                        case ' ':
                        case '\t':
                        case '\n':
                            index++;
                            continue;
                    }
                    if (json[index] == expected) {
                        index++;
                        return true;
                    } else if (required) {
                        throw new System.Exception("expected " + expected + " at index " + index);
                    } else {
                        return false;
                    }
                }
            }

            static FbxRepresentation FromJsonHelper(string json, ref int index) {
                Consume('{', json, ref index);
                var fbxrep = new FbxRepresentation();
                if (Consume('}', json, ref index, required: false)) {
                    // this is a leaf; we're done.
                } else {
                    // this is a parent of one or more objects; store them.
                    fbxrep.c = new Dictionary<string, FbxRepresentation>();
                    do {
                        Consume('"', json, ref index);
                        int nameStart = index;
                        while (json[index] != '"') { index++; }
                        string name = json.Substring(nameStart, index - nameStart);
                        index++;
                        Consume(':', json, ref index);
                        var subrep = FromJsonHelper(json, ref index);
                        fbxrep.c.Add(name, subrep);
                    } while(Consume(',', json, ref index, required: false));
                    Consume('}', json, ref index);
                }
                return fbxrep;
            }

            public static FbxRepresentation FromJson(string json) {
                if (json.Length == 0) { return null; }
                int index = 0;
                return FromJsonHelper(json, ref index);
            }

            void ToJsonHelper(System.Text.StringBuilder builder) {
                builder.Append("{");
                if (c != null) {
                    bool first = true;
                    foreach(var kvp in c.OrderBy(kvp => kvp.Key)) {
                        if (!first) { builder.Append(','); }
                        else { first = false; }

                        builder.Append('"');
                        builder.Append(kvp.Key); // the name
                        builder.Append('"');
                        builder.Append(':');
                        kvp.Value.ToJsonHelper(builder);
                    }
                }
                builder.Append("}");
            }

            public string ToJson() {
                var builder = new System.Text.StringBuilder();
                ToJsonHelper(builder);
                return builder.ToString();
            }

            public static bool IsLeaf(FbxRepresentation rep) {
                return rep == null || rep.c == null;
            }

            public static HashSet<string> CopyKeySet(FbxRepresentation rep) {
                if (IsLeaf(rep)) {
                    return new HashSet<string>();
                } else {
                    return new HashSet<string>(rep.c.Keys);
                }
            }

            public static FbxRepresentation Find(FbxRepresentation rep, string key) {
                if (IsLeaf(rep)) { return null; }

                FbxRepresentation child;
                if (rep.c.TryGetValue(key, out child)) {
                    return child;
                }
                return null;
            }
        }

        /// <summary>
        /// This class is responsible for figuring out what work needs to be done
        /// during an update. It's also responsible for actually doing the work.
        ///
        /// Key assumption: upon importing, the names in the fbx model are unique.
        ///
        /// TODO: handle conflicts. For now, we just clobber the prefab.
        /// </summary>
        public class UpdateList
        {

            // We build up a flat list of names for the nodes of the old fbx,
            // the new fbx, and the prefab. We also figure out the parents.
            class Data {
                Dictionary<string, string> m_parents;

                public Data() {
                    m_parents = new Dictionary<string, string>();
                }

                public void AddNode(string name, string parent) {
                    m_parents.Add(name, parent);
                }

                public bool HasNode(string name) {
                    return m_parents.ContainsKey(name);
                }

                public string GetParent(string name) {
                    string parent;
                    if (m_parents.TryGetValue(name, out parent)) {
                        return parent;
                    } else {
                        return null;
                    }
                }

                public static HashSet<string> GetAllNames(params Data [] data) {
                    var names = new HashSet<string>();
                    foreach(var d in data) {
                        foreach(var k in d.m_parents.Keys) {
                            names.Add(k);
                        }
                    }
                    return names;
                }
            }

            /// <summary>
            /// Data for the hierarchy of the old fbx file, the new fbx file, and the prefab.
            /// </summary>
            Data m_old, m_new, m_prefab;

            /// <summary>
            /// Names of all nodes -- old, new, or prefab.
            /// </summary>
            HashSet<string> m_allNames = new HashSet<string>();

            /// <summary>
            /// Names of the new nodes to create in step 1.
            /// </summary>
            HashSet<string> m_nodesToCreate = new HashSet<string>();

            /// <summary>
            /// Information about changes in parenting for step 2.
            /// Map from name of node in the prefab to name of node in prefab or newNodes.
            /// This is all the nodes in the prefab that need to be reparented.
            /// </summary>
            Dictionary<string,string> m_reparentings = new Dictionary<string, string>();

            /// <summary>
            /// Names of the nodes to destroy in step 3.
            /// Actually calculated early.
            /// </summary>
            HashSet<string> m_nodesToDestroy = new HashSet<string>();

            /// <summary>
            /// Components to destroy in step 4a.
            /// The string is the name in the prefab; we destroy the first
            /// component that matches the type.
            /// </summary>
            //Dictionary<string, List<System.Type>> m_componentsToDestroy = new Dictionary<string, List<System.Type>>();

            /// <summary>
            /// List of components to update or create in steps 4b and 4c.
            /// The string is the name in the prefab.
            /// The component is a pointer to the component in the FBX.
            /// If the component doesn't exist in the prefab, we create it.
            /// If the component exists in the prefab, we update it.
            /// TODO: handle conflicts!
            /// </summary>
            //Dictionary<string, List<Component>> m_componentsToUpdate = new Dictionary<string, List<Component>>();

            void SetupOld(FbxRepresentation oldFbx, string parent = null) {
                if (m_old == null) { m_old = new Data(); }
                foreach(var name in FbxRepresentation.CopyKeySet(oldFbx)) {
                    m_old.AddNode(name, parent);
                    SetupOld(FbxRepresentation.Find(oldFbx, name), name);
                }
            }

            static void SetupFromTransform(Data data, Transform xfo, string parent) {
                foreach(Transform child in xfo) {
                    data.AddNode(child.name, parent);
                    SetupFromTransform(data, child, child.name);
                }
            }

            void SetupNew(Transform newFbx) {
                m_new = new Data();
                SetupFromTransform(m_new, newFbx, null);
            }

            void SetupPrefab(FbxPrefab prefab) {
                m_prefab = new Data();
                SetupFromTransform(m_prefab, prefab.transform, null);
            }

            void ClassifyDestroyCreateNodes()
            {
                foreach(var name in m_allNames) {
                    var isOld = m_old.HasNode(name);
                    var isNew = m_new.HasNode(name);
                    var isPrefab = m_prefab.HasNode(name);
                    if (isOld && !isNew && isPrefab) {
                        // This node got deleted in the DCC, so delete it.
                        m_nodesToDestroy.Add(name);
                    } else if (!isOld && isNew && !isPrefab) {
                        // This node was created in the DCC but not in Unity, so create it.
                        m_nodesToCreate.Add(name);
                    }
                }
            }
            bool ShouldDestroy(string name) {
                return m_nodesToDestroy.Contains(name);
            }
            bool ShouldCreate(string name) {
                return m_nodesToCreate.Contains(name);
            }

            /// <summary>
            /// Discover what needs to happen.
            ///
            /// Four-step program:
            /// 1. Figure out what nodes exist (we have that data, just need to
            ///    make it more convenient to query).
            /// 2. Figure out what nodes we need to create, and what nodes we
            ///    need to destroy.
            /// 3. Figure out what nodes we need to reparent.
            /// todo 4. Figure out what nodes we need to update or add components to.
            /// </summary>
            public UpdateList(
                    FbxRepresentation oldFbx,
                    Transform newFbx,
                    FbxPrefab prefab)
            {
                SetupOld(oldFbx);
                SetupNew(newFbx);
                SetupPrefab(prefab);
                m_allNames = Data.GetAllNames(m_old, m_new, m_prefab);

                // Set up m_nodesToDestroy and m_nodesToCreate.
                ClassifyDestroyCreateNodes();

                // Among prefab nodes we're not destroying, see if we need to change their parent.
                // Cases for the parent:
                //   old  new  prefab
                //    a    a     a   => no action
                //    a    x     a   => doesn't matter (a is being destroyed)
                //    a    b     a   => switch to b
                //    a    b     c   => conflict! switch to b for now (todo!)
                //    x    a     x   => create, and parent to a. This is the second loop below.
                //    x    a     a   => no action
                //    x    a     b   => conflict! switch to a for now (todo!)
                //    x    x     a   => no action. Todo: what if a is being destroyed? conflict!
                foreach(var name in Data.GetAllNames(m_prefab)) {
                    var prefabParent = m_prefab.GetParent(name);
                    var oldParent = m_old.GetParent(name);
                    var newParent = m_new.GetParent(name);

                    if (oldParent != newParent && prefabParent != newParent) {
                        // Conflict in this case, we'll need to resolve it:
                        // if (oldParent != prefabParent && !ShouldDestroy(prefabParent))

                        // But unconditionally switch to the new parent for
                        // now, unless we're already there.
                        m_reparentings.Add(name, newParent);
                    }
                }
                // All new nodes need to be reparented (we didn't loop over them because we
                // only looped over the stuff in the prefab now).
                foreach(var name in m_nodesToCreate) {
                    m_reparentings.Add(name, m_new.GetParent(name));
                }
            }

            public bool NeedsUpdates() {
                return m_nodesToDestroy.Count > 0
                    || m_nodesToCreate.Count > 0
                    || m_reparentings.Count > 0
                    // || m_componentsToDestroy.Count > 0
                    // || m_comopnentsToUpdate.Count > 0
                    ;
            }

            /// <summary>
            /// Then we act -- in a slightly different order:
            /// 1. Create all the new nodes we need to create.
            /// 2. Reparent as needed.
            /// 3. Delete the nodes that are no longer needed.
            /// todo 4. Update the components:
            ///    4a. delete components no longer used
            ///    4b. create new components
            ///    4c. update component values
            ///    (A) and (B) are largely about meshfilter/meshrenderer,
            ///    (C) is about transforms (and materials?)
            /// </summary>
            public void ImplementUpdates(FbxPrefab prefabInstance)
            {
                // Gather up all the nodes in the prefab.
                var prefabNodes = new Dictionary<string, Transform>();
                foreach(var node in prefabInstance.GetComponentsInChildren<Transform>()) {
                    prefabNodes.Add(node.name, node);
                }

                // Create new nodes.
                foreach(var name in m_nodesToCreate) {
                    prefabNodes.Add(name, new GameObject(name).transform);
                    Debug.Log("Created " + name);
                }

                // Implement the reparenting in two phases to avoid making loops, e.g.
                // if we're flipping from a -> b to b -> a, we don't want to
                // have a->b->a in the intermediate stage.

                // First set the parents to null.
                foreach(var kvp in m_reparentings) {
                    var name = kvp.Key;
                    prefabNodes[name].parent = null;
                }

                // Then set the parents to the intended value.
                foreach(var kvp in m_reparentings) {
                    var name = kvp.Key;
                    var parent = kvp.Value;
                    Transform parentNode;
                    if (parent == null) {
                        parentNode = prefabInstance.transform;
                    } else {
                        parentNode = prefabNodes[parent];
                    }
                    prefabNodes[name].parent = parentNode;
                    Debug.Log("Parented " + name + " under " + parentNode.name);
                }

                // Destroy the old nodes.
                foreach(var toDestroy in m_nodesToDestroy) {
                    GameObject.DestroyImmediate(prefabNodes[toDestroy].gameObject);
                }

                // TODO: update components!
            }
        }

        /// <summary>
        /// Return whether this FbxPrefab component requests automatic updates.
        /// </summary>
        public bool WantsAutoUpdate() {
            return m_autoUpdate;
        }

        /// <summary>
        /// Set whether this FbxPrefab component requests automatic updates.
        /// </summary>
        public void SetAutoUpdate(bool autoUpdate) {
            if (!m_autoUpdate && autoUpdate) {
                // We just turned autoupdate on, so update now!
                CompareAndUpdate();
            }
            m_autoUpdate = autoUpdate;
        }

        /// <summary>
        /// Compare the old and new, and update the old according to the rules.
        /// </summary>
        void CompareAndUpdate()
        {
            // If we're not tracking anything, stop updating now.
            // (Typically this is due to a manual update.)
            if (!m_fbxModel) {
                return;
            }

            // First write down what we want to do.
            var updates = new UpdateList(GetFbxHistory(), m_fbxModel.transform, this);

            // If we don't need to do anything, jump out now.
            if (!updates.NeedsUpdates()) {
                return;
            }

            // We want to do something, so instantiate the prefab, work on the instance, then copy back.
            var prefabInstance = UnityEditor.PrefabUtility.InstantiatePrefab(this.gameObject) as GameObject;
            if (!prefabInstance) {
                throw new System.Exception(string.Format("Failed to instantiate {0}; is it really a prefab?",
                            this.gameObject));
            }
            var fbxPrefab = prefabInstance.GetComponent<FbxPrefab>();

            updates.ImplementUpdates(fbxPrefab);

            // Update the representation of the history to match the new fbx.
            var newFbxRep = FbxRepresentation.FromTransform(m_fbxModel.transform);
            var newFbxRepString = newFbxRep.ToJson();
            fbxPrefab.m_fbxHistory = newFbxRepString;

            // Save the changes back to the prefab.
            UnityEditor.PrefabUtility.ReplacePrefab(prefabInstance, this.transform);

            // Destroy the prefabInstance.
            GameObject.DestroyImmediate(prefabInstance);
        }

        /// <summary>
        /// Returns the fbx model we're tracking.
        /// </summary>
        public GameObject GetFbxAsset()
        {
            return m_fbxModel;
        }

        /// <summary>
        /// Returns the asset path of the fbx model we're tracking.
        /// </summary>
        public string GetFbxAssetPath()
        {
            if (!m_fbxModel) { return ""; }
            return UnityEditor.AssetDatabase.GetAssetPath(m_fbxModel);
        }

        /// <summary>
        /// Returns the tree representation of the fbx file as it was last time we sync'd.
        /// </summary>
        public FbxRepresentation GetFbxHistory()
        {
            return FbxRepresentation.FromJson(m_fbxHistory);
        }

        /// <summary>
        /// Sync the prefab to match the FBX file.
        /// </summary>
        public void SyncPrefab()
        {
            CompareAndUpdate();
        }

        /// <summary>
        /// Set up the FBX file that this prefab should track.
        ///
        /// Set to null to stop tracking in a way that we can
        /// still restart tracking later.
        /// </summary>
        public void SetSourceModel(GameObject fbxModel) {
            // Null is OK. But otherwise, fbxModel must be an fbx.
            if (fbxModel && !UnityEditor.AssetDatabase.GetAssetPath(fbxModel).EndsWith(".fbx")) {
                throw new System.ArgumentException("FbxPrefab source model must be an fbx asset");
            }

            m_fbxModel = fbxModel;

            // Case 0: fbxModel is null and we have no history
            //          => not normal data flow, but doing nothing is
            //             non-surprising
            // Case 1: fbxModel is null and we have history
            //          => user wants to stop auto-update. Remember history for
            //             when they want to reconnect.
            // Case 2: fbxModel is not null and we have no history
            //          => normal case in ConvertToModel when we just added the
            //             component
            // Case 3: fbxModel is not null and we have history
            //          => normal case when user wants to reconnect or change
            //             the model. Keep the old history and update
            //             immediately.
            if (!m_fbxModel) {
                // Case 0 or 1
                return;
            }

            if (string.IsNullOrEmpty(m_fbxHistory)) {
                // Case 2.
                // This is the first time we've seen the FBX file. Assume that
                // it's the original FBX. Further assume that the user is happy
                // with the prefab as it is now, so don't update it to match the FBX.
                m_fbxHistory = FbxRepresentation.FromTransform(m_fbxModel.transform).ToJson();
            } else {
                // Case 3.
                // User wants to reconnect or change the connection.
                // Update immediately.
                CompareAndUpdate();
            }
        }
#endif
    }
}
