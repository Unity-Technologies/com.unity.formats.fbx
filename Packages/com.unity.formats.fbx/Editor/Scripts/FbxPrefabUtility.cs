using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Runtime.Serialization;
using UnityEngine.Formats.Fbx.Exporter;
using System.Security.Permissions;

namespace UnityEditor.Formats.Fbx.Exporter
{   
    public class FbxPrefabUtility
    {

        private FbxPrefab m_fbxPrefab;

        public FbxPrefabUtility(FbxPrefab fbxPrefab)
        {
            m_fbxPrefab = fbxPrefab;
        }

        /// <summary>
        /// Utility function: check if the user has specified a matching Unity name in the inspector
        /// </summary>
        public string GetUnityObjectName(string fbxObjectName)
        {
            string newNameInUnity = fbxObjectName;
            if (!String.IsNullOrEmpty(fbxObjectName) && m_fbxPrefab.NameMapping != null)
            {
                foreach (var nameMapping in m_fbxPrefab.NameMapping)
                {
                    if (fbxObjectName == nameMapping.FBXObjectName)
                    {
                        newNameInUnity = nameMapping.UnityObjectName;
                        break;
                    }
                }
            }
            return newNameInUnity;
        }

        /// <summary>
        /// Utility function: remove the name mapping matching the unity name specified
        /// </summary>
        public void RemoveMappingFBXObjectName(string fbxObjectName)
        {
            StringPair stockNameMapping = new StringPair();
            if (!String.IsNullOrEmpty(fbxObjectName) && m_fbxPrefab.NameMapping != null)
            {
                bool foundMapping = false;
                foreach (StringPair nameMapping in m_fbxPrefab.NameMapping)
                {
                    if (fbxObjectName == nameMapping.FBXObjectName)
                    {
                        stockNameMapping = nameMapping;
                        foundMapping = true;
                        break;
                    }
                }
                if (foundMapping)
                {
                    m_fbxPrefab.NameMapping.Remove(stockNameMapping);
                }
            }
        }

        /// <summary>
        /// Utility function: remove the name mapping matching the FBX name specified
        /// </summary>
        public void RemoveMappingUnityObjectName(string unityObjectName)
        {
            StringPair stockNameMapping = new StringPair();
            if (!String.IsNullOrEmpty(unityObjectName) && m_fbxPrefab.NameMapping != null)
            {
                bool foundMapping = false;
                foreach (StringPair nameMapping in m_fbxPrefab.NameMapping)
                {
                    if (unityObjectName == nameMapping.UnityObjectName)
                    {
                        stockNameMapping = nameMapping;
                        foundMapping = true;
                        break;
                    }
                }
                if (foundMapping)
                {
                    m_fbxPrefab.NameMapping.Remove(stockNameMapping);
                }
            }
        }

        /// <summary>
        /// Utility function: check if the user has specified a matching FBX name in the inspector
        /// </summary> 
        public string GetFBXObjectName(string unityObjectName)
        {
            string oldNameInFBX = unityObjectName;
            if (!String.IsNullOrEmpty(unityObjectName) && m_fbxPrefab.NameMapping != null)
            {
                foreach (var nameMapping in m_fbxPrefab.NameMapping)
                {
                    if (unityObjectName == nameMapping.UnityObjectName)
                    {
                        oldNameInFBX = nameMapping.FBXObjectName;
                        break;
                    }
                }
            }
            return oldNameInFBX;
        }

        /// <summary>
        /// Utility function: log a message, make clear it's the prefab update.
        /// </summary>
        [System.Diagnostics.Conditional("FBXEXPORTER_DEBUG")]
        public static void Log(string message)
        {
            Debug.Log("Fbx prefab update: " + message);
        }

        [System.Diagnostics.Conditional("FBXEXPORTER_DEBUG")]
        public static void Log(string format, params object[] p)
        {
            Log(string.Format(format, p));
        }

        /// <summary>
        /// Utility function: create the object, which must be null.
        ///
        /// Used to make sure that we don't use fields before they're
        /// initialized (for UpdateList): init the fields to null so
        /// we get an NRE if we try to read them before they're set,
        /// or an FPE here if we set them before we were supposed to.
        /// </summary>
        internal static void Initialize<T>(ref T item) where T : new()
        {
            if (item != null) { throw new FbxPrefabException(); }
            item = new T();
        }

        /// <summary>
        /// Utility function: append an item to a list.
        /// If the list is null, create it.
        /// </summary>
        internal static void Append<T>(ref List<T> thelist, T item)
        {
            if (thelist == null)
            {
                thelist = new List<T>();
            }
            thelist.Add(item);
        }

        /// <summary>
        /// Utility function: add an item to a dictionary.
        /// If the dictionary is null, create it.
        /// </summary>
        internal static void Add<TKey, TValue>(ref Dictionary<TKey, TValue> thedict, TKey key, TValue value)
        {
            if (thedict == null)
            {
                thedict = new Dictionary<TKey, TValue>();
            }
            thedict.Add(key, value);
        }

        /// <summary>
        /// Utility function: get an entry in the dictionary, or create it,
        /// and return it.
        /// The dictionary must not be null.
        /// </summary>
        public static TValue GetOrCreate<TKey, TValue>(Dictionary<TKey, TValue> thedict, TKey key) where TValue : new()
        {
            if (thedict == null)
            {
                throw new System.ArgumentNullException("thedict");
            }

            TValue value;
            if (!thedict.TryGetValue(key, out value))
            {
                value = new TValue();
                thedict[key] = value;
            }
            return value;
        }

        /// <summary>
        /// Utility function: append an item to a list in a dictionary of lists.
        /// Create all the entries needed to append to the list.
        /// The dictionary will be allocated if it's null.
        /// </summary>
        internal static void Append<TKey, TValue>(ref Dictionary<TKey, List<TValue>> thedict, TKey key, TValue item)
        {
            if (thedict == null)
            {
                thedict = new Dictionary<TKey, List<TValue>>();
            }
            GetOrCreate(thedict, key).Add(item);
        }

        /// <summary>
        /// Utility function: append an item to a list in a dictionary of lists.
        /// Create all the entries needed to append to the list.
        /// The dictionary must not be null.
        /// </summary>
        public static void Append<TKey, TValue>(Dictionary<TKey, List<TValue>> thedict, TKey key, TValue item)
        {
            GetOrCreate(thedict, key).Add(item);
        }

        /// <summary>
        /// Utility function: append an item to a list in a 2-level dictionary of lists.
        /// Create all the entries needed to append to the list.
        /// The dictionary will be allocated if it's null.
        /// </summary>
        internal static void Append<TKey1, TKey2, TValue>(ref Dictionary<TKey1, Dictionary<TKey2, List<TValue>>> thedict, TKey1 key1, TKey2 key2, TValue item)
        {
            if (thedict == null)
            {
                thedict = new Dictionary<TKey1, Dictionary<TKey2, List<TValue>>>();
            }
            var thesubmap = GetOrCreate(thedict, key1);
            Append(thesubmap, key2, item);
        }

        /// <summary>
        /// This class is responsible for figuring out what work needs to be done
        /// during an update. It's also responsible for actually doing the work.
        ///
        /// Key assumption: upon importing, the names in the fbx model are unique.
        ///
        /// TODO: handle conflicts. For now, we just clobber the prefab.
        /// </summary>
        internal class UpdateList
        {
            // We build up a flat list of names for the nodes of the old fbx,
            // the new fbx, and the prefab. We also figure out the parents.
            class Data
            {
                // Parent of each node, by name.
                // The empty-string name is the root of the prefab/fbx.
                // Never null.
                Dictionary<string, string> m_parents = new Dictionary<string, string>();

                // Component value by name and type, with multiplicity.
                // name -> type -> list of value. Never null.
                Dictionary<string, Dictionary<string, List<string>>> m_components
                = new Dictionary<string, Dictionary<string, List<string>>>();

                /// <summary>
                /// Recursively explore the hierarchical representation and
                /// store it with flat indices.
                /// </summary>
                [SecurityPermission(SecurityAction.LinkDemand)]
                void InitHelper(FbxRepresentation fbxrep, string nodeName)
                {
                    foreach (var typename in fbxrep.ComponentTypes)
                    {
                        var jsonValues = fbxrep.GetComponentValues(typename);
                        foreach (var jsonValue in jsonValues)
                        {
                            Append(ref m_components, nodeName, typename, jsonValue);
                        }
                    }
                    foreach (var child in fbxrep.ChildNames)
                    {
                        m_parents.Add(child, nodeName);
                        InitHelper(fbxrep.GetChild(child), child);
                    }
                }

                public Data(FbxRepresentation fbxrep)
                {
                    m_parents.Add("", ""); // the root points to itself
                    InitHelper(fbxrep, "");
                }

                public Data(Transform xfo) : this(new FbxRepresentation(xfo))
                {
                }

                /// <summary>
                /// Get the set of node names.
                /// </summary>
                public IEnumerable<string> NodeNames
                {
                    get
                    {
                        // the names are the keys of the name -> parent map
                        return new HashSet<string>(m_parents.Keys);
                    }
                }

                /// <summary>
                /// Does this data set have a node of this name?
                /// </summary>
                public bool HasNode(string name)
                {
                    return m_parents.ContainsKey(name);
                }

                /// <summary>
                /// Get the parent of the node.
                /// If the parent is the root of the fbx or the prefab,
                /// returns the empty string.
                /// </summary>
                public string GetParent(string name)
                {
                    string parent;
                    if (m_parents.TryGetValue(name, out parent))
                    {
                        return parent;
                    }
                    else
                    {
                        return "";
                    }
                }

                /// <summary>
                /// Get all the component types for a given node.
                /// e.g. UnityEngine.Transform, UnityEngine.BoxCollider, etc.
                /// </summary>
                public IEnumerable<string> GetComponentTypes(string name)
                {
                    Dictionary<string, List<string>> components;
                    if (!m_components.TryGetValue(name, out components))
                    {
                        // node doesn't exist => it has no components
                        return new string[0];
                    }
                    return components.Keys;
                }

                /// <summary>
                /// Get all the component values of a given type for a given node.
                ///
                /// Don't modify the list that gets returned.
                /// </summary>
                public List<string> GetComponentValues(string name, string typename)
                {
                    Dictionary<string, List<string>> components;
                    if (!m_components.TryGetValue(name, out components))
                    {
                        return new List<string>();
                    }
                    List<string> jsonValues;
                    if (!components.TryGetValue(typename, out jsonValues))
                    {
                        return new List<string>();
                    }
                    return jsonValues;
                }
            }

            private FbxPrefab m_fbxPrefab;
            private FbxPrefabUtility m_fbxPrefabUtility;
            /// <summary>
            /// Data for the hierarchy of the old fbx file, the new fbx file, and the prefab.
            /// </summary>
            Data m_oldFbxData, m_newFbxData, m_prefabData;

            /// <summary>
            /// Names of the new nodes to create in step 1.
            /// </summary>
            HashSet<string> m_nodesToCreate;

            /// <summary>
            /// Information about changes in parenting for step 2.
            /// Map from name of node in the prefab to name of node in prefab or newNodes.
            /// This is all the nodes in the prefab that need to be reparented.
            /// </summary>
            Dictionary<string, string> m_reparentings;

            /// <summary>
            /// Names of the nodes in the prefab to destroy in step 3.
            /// Actually calculated early.
            /// </summary>
            HashSet<string> m_nodesToDestroy;

            /// <summary>
            /// Names of the nodes that will be renamed in the prefab
            /// in step 3.
            /// </summary>
            HashSet<string> m_nodesToRename;

            /// <summary>
            /// Names of the nodes that the prefab will have after we implement
            /// steps 1, 2, and 3.
            /// </summary>
            HashSet<string> m_nodesInUpdatedPrefab;

            /// <summary>
            /// Components to destroy in step 4a.
            /// The string is the name in the prefab; we destroy the first
            /// component that matches the type.
            /// </summary>
            Dictionary<string, List<System.Type>> m_componentsToDestroy;

            struct ComponentValue
            {
                public System.Type t;
                public string jsonValue;

                public ComponentValue(System.Type t, string jsonValue)
                {
                    this.t = t;
                    this.jsonValue = jsonValue;
                }
            }

            /// <summary>
            /// List of components to update or create in steps 4b and 4c.
            /// The string is the name in the prefab.
            /// The component is a pointer to the component in the FBX.
            /// If the component doesn't exist in the prefab, we create it.
            /// If the component exists in the prefab, we update the first
            ///   match (without repetition).
            /// </summary>
            Dictionary<string, List<ComponentValue>> m_componentsToUpdate;

            [SecurityPermission(SecurityAction.LinkDemand)]
            void ClassifyDestroyCreateNodes()
            {
                // Figure out which nodes to add to the prefab, which nodes in the prefab to destroy.
                Initialize(ref m_nodesToCreate);
                Initialize(ref m_nodesToDestroy);
                Initialize(ref m_nodesToRename);


                foreach (var nodeName in m_oldFbxData.NodeNames.Union(m_newFbxData.NodeNames))
                {
                    var isOldFBXNodeName = m_oldFbxData.HasNode(nodeName);
                    var isNewFBXNodeName = m_newFbxData.HasNode(nodeName);

                    if (isOldFBXNodeName != isNewFBXNodeName)
                    {

                        // If there is a mapping for the old name, skip it. We will add the new name to the nodes to rename
                        if (m_newFbxData.HasNode(m_fbxPrefabUtility.GetFBXObjectName(nodeName)) && isOldFBXNodeName)
                        {
                            continue;
                        }

                        // A node was added or deleted in the DCC.
                        // Do the same in Unity if it wasn't already done.
                        var isPrefab = m_prefabData.HasNode(nodeName);
                        // If the New FBX has a node that has to be renamed in the prefab
                        if (isNewFBXNodeName && m_oldFbxData.HasNode(m_fbxPrefabUtility.GetUnityObjectName(nodeName)))
                        {
                            // FBX Node Name of New FBX Data
                            m_nodesToRename.Add(nodeName);
                            // If it's an old name that is present in the prefab, add it to the list to destroy
                        }
                        else if (!isNewFBXNodeName && isPrefab)
                        {
                            m_nodesToDestroy.Add(nodeName);
                            // If it's an new name that is not present in the prefab, add it to the list to destroy
                        }
                        else if (isNewFBXNodeName && !isPrefab)
                        {
                            m_nodesToCreate.Add(nodeName);
                        }
                    }
                }

                // Figure out what nodes will exist after we create and destroy.
                Initialize(ref m_nodesInUpdatedPrefab);
                m_nodesInUpdatedPrefab.Add(""); // the root is nameless
                foreach (var nodeName in m_prefabData.NodeNames.Union(m_nodesToCreate))
                {
                    if (m_nodesToDestroy.Contains(nodeName))
                    {
                        continue;
                    }
                    m_nodesInUpdatedPrefab.Add(nodeName);
                }
            }

            [SecurityPermission(SecurityAction.LinkDemand)]
            void ClassifyReparenting()
            {
                Initialize(ref m_reparentings);

                // Among prefab nodes we're not destroying, see if we need to change their parent.
                // x (means deleted)
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
                foreach (var prefabNodeName in m_prefabData.NodeNames)
                {
                    if (string.IsNullOrEmpty(prefabNodeName))
                    {
                        // Don't reparent the root.
                        continue;
                    }
                    if (m_nodesToDestroy.Contains(prefabNodeName))
                    {
                        // Don't bother reparenting, we'll be destroying this anyway.
                        continue;
                    }

                    var prefabParent = m_prefabData.GetParent(prefabNodeName);
                    var oldParent = m_oldFbxData.GetParent(prefabNodeName);
                    // The newFbxData contains the fbx name, so we get the parent from the matching prefabNodeName equivalent
                    var newParent = m_newFbxData.GetParent(m_fbxPrefabUtility.GetFBXObjectName(prefabNodeName));

                    // If it's a name mapping, don't add a reparenting, we'll rename it later in ImplementUpdates()
                    if (oldParent != newParent && prefabParent != newParent && oldParent != m_fbxPrefabUtility.GetUnityObjectName(newParent))
                    {
                        // Conflict in this case:
                        // if (oldParent != prefabParent && !ShouldDestroy(prefabParent))

                        // For now, 'newParent' always wins:
                        m_reparentings.Add(prefabNodeName, newParent);
                    }
                }

                // All new nodes need to be reparented no matter what.
                // We're guaranteed we didn't already add them because we only
                // looped over what exists in the prefab now.
                foreach (var nodeName in m_nodesToCreate)
                {
                    m_reparentings.Add(nodeName, m_newFbxData.GetParent(nodeName));
                }
            }

            [SecurityPermission(SecurityAction.LinkDemand)]
            void ClassifyComponents(Transform newFbx, Transform prefab)
            {
                Initialize(ref m_componentsToDestroy);
                Initialize(ref m_componentsToUpdate);

                // Figure out how to map from type names to System.Type values,
                // without going through reflection APIs. This allows us to handle
                // components from various assemblies.
                //
                // We're going to be adding from the newFbx, and deleting from the prefab,
                // so we don't need to know about all the types that oldFbx might have.
                var componentTypes = new Dictionary<string, System.Type>();
                foreach (var component in newFbx.GetComponentsInChildren<Component>())
                {
                    var componentType = component.GetType();
                    componentTypes[componentType.ToString()] = componentType;
                }
                foreach (var component in prefab.GetComponentsInChildren<Component>())
                {
                    // ignore missing components
                    if (component == null)
                    {
                        continue;
                    }
                    var componentType = component.GetType();
                    componentTypes[componentType.ToString()] = componentType;
                }

                // For each node in the prefab (after adding any new nodes):
                // 1. If a component is in the old but not the new, remove it from
                //    the prefab.
                // 2. If it's in the new but not the old, add it to the prefab.
                // 3. If it's in both the old and new, but with different values,
                //    update the prefab values.
                // If a component type is repeated (e.g. two BoxCollider on the
                // same node), we line up the components in the order they
                // appear. This never happens in stock Unity, someone must have
                // added an AssetPostprocessor for it to occur.
                // (Uni-31604) Improve how the auto-updater classifies components
                //
                // If the node isn't going to be in the prefab, we don't care
                // about what components might be on it.
                foreach (var nodeNameInUpdatedPrefab in m_nodesInUpdatedPrefab)
                {
                    // Get the matching name for the node in the m_newFbxData
                    string nodeNameInFBX = m_fbxPrefabUtility.GetFBXObjectName(nodeNameInUpdatedPrefab);

                    // The newFbxData contains the fbx name, so we check if the node is in it with the matching equivalent
                    if (!m_newFbxData.HasNode(nodeNameInFBX))
                    {
                        // It's not in the FBX, so clearly we're not updating any components.
                        // We don't need to check if it's in m_prefab because
                        // we're only iterating over those.
                        continue;
                    }

                    var allTypes = m_oldFbxData.GetComponentTypes(nodeNameInUpdatedPrefab).Union(
                        m_newFbxData.GetComponentTypes(nodeNameInFBX));

                    //Compare oldValues that have prefab unity names with new values that have fbx names.
                    foreach (var typename in allTypes)
                    {
                        var oldValues = m_oldFbxData.GetComponentValues(nodeNameInUpdatedPrefab, typename);
                        var newValues = m_newFbxData.GetComponentValues(nodeNameInFBX, typename);
                        List<string> prefabValues = null; // get them only if we need them.

                        // If we have multiple identical-type components, match them up by index.
                        for (int i = 0, n = System.Math.Max(oldValues.Count, newValues.Count); i < n; ++i)
                        {
                            if (/* isNew */ i < newValues.Count)
                            {
                                var newValue = newValues[i];

                                var isReparentedTransform = (typename == "UnityEngine.Transform"
                                    && m_reparentings.ContainsKey(nodeNameInUpdatedPrefab));

                                if (/* isOld */ i < oldValues.Count && oldValues[i] == newValue && !isReparentedTransform)
                                {
                                    // No change from the old => skip.
                                    continue;
                                }
                                if (prefabValues == null) { prefabValues = m_prefabData.GetComponentValues(nodeNameInUpdatedPrefab, typename); }
                                if (i < prefabValues.Count && prefabValues[i] == newValue && !isReparentedTransform)
                                {
                                    // Already updated => skip.
                                    continue;
                                }
                                Append(m_componentsToUpdate, nodeNameInUpdatedPrefab,
                                    new ComponentValue(componentTypes[typename], newValue));
                            }
                            else
                            {
                                // Not in the new, but is in the old, so delete
                                // it if it's not already deleted from the
                                // prefab.
                                if (prefabValues == null) { prefabValues = m_prefabData.GetComponentValues(nodeNameInUpdatedPrefab, typename); }
                                if (i < prefabValues.Count)
                                {
                                    Append(m_componentsToDestroy, nodeNameInUpdatedPrefab, componentTypes[typename]);
                                }
                            }
                        }
                    }
                }
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
            /// 4. Figure out what nodes we need to update or add components to.
            /// </summary>
            [SecurityPermission(SecurityAction.LinkDemand)]
            public UpdateList(
                FbxRepresentation oldFbx,
                Transform newFbx,
                FbxPrefab prefab)
            {
                m_fbxPrefab = prefab;
                m_fbxPrefabUtility = new FbxPrefabUtility(m_fbxPrefab);

                m_oldFbxData = new Data(oldFbx);
                m_newFbxData = new Data(newFbx);
                m_prefabData = new Data(prefab.transform);

                ClassifyDestroyCreateNodes();
                ClassifyReparenting();
                ClassifyComponents(newFbx, prefab.transform);
            }

            public bool NeedsUpdates()
            {
                return m_nodesToDestroy.Count > 0
                    || m_nodesToCreate.Count > 0
                    || m_nodesToRename.Count > 0
                    || m_reparentings.Count > 0
                    || m_componentsToDestroy.Count > 0
                    || m_componentsToUpdate.Count > 0
                    ;
            }

            public HashSet<String> NodesToCreate
            {
                get
                {
                    return m_nodesToCreate;
                }
            }

            public HashSet<String> NodesToDestroy
            {
                get
                {
                    return m_nodesToDestroy;
                }
            }

            public HashSet<String> NodesToRename
            {
                get
                {
                    return m_nodesToRename;
                }
            }

            /// <summary>
            /// Get the equivalent transform on the linked prefab for the given fbx transform.
            /// e.g. if there is a transform called Bone1 in the fbx, find the transform called Bone1 in the prefab.
            /// </summary>
            /// <returns>The linked prefab transform that matches the given fbx transform or null if not found</returns>
            private Transform GetMatchingPrefabTransform(Transform fbxTransform, Transform fbxAssetRoot, Transform prefabAssetRoot)
            {
                var transformPath = AnimationUtility.CalculateTransformPath(fbxTransform, fbxAssetRoot);
                return prefabAssetRoot.Find(transformPath);
            }

            /// <summary>
            /// Then we act -- in a slightly different order:
            /// 1. Create all the new nodes we need to create.
            /// 2. Reparent as needed.
            /// 3. Delete the nodes that are no longer needed.
            /// 4. Update the components:
            ///    4a. delete components no longer used
            ///    4b. create new components
            ///    4c. update component values
            ///    (4a.) and (4b.) are largely about meshfilter/meshrenderer,
            ///    (4c.) is about transforms (and materials?)
            ///
            /// Return the set of GameObject that were created or reparented
            /// in 1 and 2; or that were updated in 4. Does not return the destroyed
            /// GameObjects -- they've been destroyed!
            /// </summary>
            [SecurityPermission(SecurityAction.LinkDemand)]
            public HashSet<GameObject> ImplementUpdates(FbxPrefab prefabInstance)
            {
                if (prefabInstance == null)
                {
                    return new HashSet<GameObject>();
                }

                Log("{0}: performing updates", prefabInstance.name);

                var updatedNodes = new HashSet<GameObject>();

                // Gather up all the nodes in the prefab so we can look up
                // nodes. We use the empty string for the root node.
                var prefabRoot = prefabInstance.transform;
                var prefabNodes = new Dictionary<string, Transform>();
                foreach (var nodeTransform in prefabInstance.GetComponentsInChildren<Transform>())
                {
                    if (nodeTransform == prefabRoot)
                    {
                        prefabNodes[""] = nodeTransform;
                    }
                    else
                    {
                        prefabNodes.Add(nodeTransform.name, nodeTransform);
                    }
                }

                // Create new nodes.
                foreach (var nodeName in m_nodesToCreate)
                {
                    var newNode = new GameObject(nodeName);
                    prefabNodes.Add(nodeName, newNode.transform);

                    Log("{0}: created new GameObject", nodeName);
                    updatedNodes.Add(newNode);
                }


                // Rename old nodes (unity names) into new nodes (FBX names).
                // We do it before the reparenting because m_reparentings contains the fbx name and won't find the parent without the renaming
                foreach (var FBXNodeNameToRename in m_nodesToRename)
                {
                    if (prefabNodes[m_fbxPrefabUtility.GetUnityObjectName(FBXNodeNameToRename)] != null)
                    {
                        prefabNodes[m_fbxPrefabUtility.GetUnityObjectName(FBXNodeNameToRename)].name = FBXNodeNameToRename;

                        Transform stockTransform = prefabNodes[m_fbxPrefabUtility.GetUnityObjectName(FBXNodeNameToRename)];
                        prefabNodes.Remove(m_fbxPrefabUtility.GetUnityObjectName(FBXNodeNameToRename));
                        prefabNodes.Add(FBXNodeNameToRename, stockTransform);
                        Log("Renamed {0} into {1}", m_fbxPrefabUtility.GetUnityObjectName(FBXNodeNameToRename), FBXNodeNameToRename);
                    }
                }

                // Implement the reparenting in two phases to avoid making loops, e.g.
                // if we're flipping from a -> b to b -> a, we don't want to
                // have a->b->a in the intermediate stage.

                // First set the parents to null.
                foreach (var kvp in m_reparentings)
                {
                    var name = kvp.Key;
                    prefabNodes[name].parent = null;
                }

                // Then set the parents to the intended value.
                foreach (var kvp in m_reparentings)
                {
                    var name = kvp.Key;
                    var parent = kvp.Value;
                    Transform parentNode;
                    if (string.IsNullOrEmpty(parent))
                    {
                        parentNode = prefabRoot;
                    }
                    else
                    {
                        parentNode = prefabNodes[parent];
                    }
                    var childNode = prefabNodes[name];
                    childNode.parent = parentNode;

                    Log("changed {0} parent to {1}", name, parentNode.name);
                    updatedNodes.Add(childNode.gameObject);
                }

                // Destroy the old nodes. Remember that DestroyImmediate recursively
                // destroys, so avoid errors.
                foreach (var nodeNameToDestroy in m_nodesToDestroy)
                {
                    var xfoToDestroy = prefabNodes[nodeNameToDestroy];
                    if (xfoToDestroy)
                    {
                        GameObject.DestroyImmediate(xfoToDestroy.gameObject);
                    }
                    Log("destroyed {0}", nodeNameToDestroy);
                    prefabNodes.Remove(nodeNameToDestroy);
                }

                // Destroy the old components.
                foreach (var kvp in m_componentsToDestroy)
                {
                    Log("destroying components on {0}", kvp.Key);
                    var nodeName = kvp.Key;
                    var typesToDestroy = kvp.Value;
                    var prefabXfo = prefabNodes[nodeName];
                    updatedNodes.Add(prefabXfo.gameObject);

                    foreach (var componentType in typesToDestroy)
                    {
                        var component = prefabXfo.GetComponent(componentType);
                        if (component != null)
                        {
                            GameObject.DestroyImmediate(component);
                            Log("destroyed component {0}:{1}", nodeName, componentType);
                        }
                    }
                }

                // Create or update the new components.
                foreach (var kvp in m_componentsToUpdate)
                {
                    var componentName = kvp.Key;
                    // We rename the node before the loop, so the component name now has the FBX object name, instead of the unity one that was saved
                    componentName = m_fbxPrefabUtility.GetFBXObjectName(componentName);

                    if (FbxPrefabAutoUpdater.Verbose)
                    {
                        Debug.Log("Component: " + componentName);
                    }
                    var fbxComponents = kvp.Value;
                    var prefabXfo = prefabNodes[componentName];
                    updatedNodes.Add(prefabXfo.gameObject);

                    // Copy the components once so we can match them up even if there's multiple fbxComponents.
                    List<Component> prefabComponents = new List<Component>(prefabXfo.GetComponents<Component>());

                    foreach (var fbxComponent in fbxComponents)
                    {
                        // Find or create the component to update.
                        int index = prefabComponents.FindIndex(x => x.GetType() == fbxComponent.t);
                        Component prefabComponent;
                        if (index >= 0)
                        {
                            // Don't match this index again.
                            prefabComponent = prefabComponents[index];
                            prefabComponents.RemoveAt(index);
                            Log("updated component {0}:{1}", componentName, fbxComponent.t);
                        }
                        else
                        {
                            prefabComponent = prefabXfo.gameObject.AddComponent(fbxComponent.t);
                            Log("created component {0}:{1}", componentName, fbxComponent.t);
                        }

                        //If the prefabComponent has not been assigned yet,
                        //it means that we couldn't find it, and that we tried to add it but that it seems like it already exists.
                        if (!prefabComponent)
                        {
                            //This is to confirm that it is a RectTransform
                            index = prefabComponents.FindIndex(x => x.GetType() == typeof(RectTransform));

                            if (index < 0)
                            {
                                Log("The component could not be found or added, and was not a RectTransform");
                                continue;
                            }

                            prefabComponent = prefabComponents[index];
                            prefabComponents.RemoveAt(index);

                            UpdateRectTransform(prefabComponent as RectTransform, fbxComponent.jsonValue);

                            Log("updated component {0}:{1}", componentName, typeof(RectTransform));
                            continue;
                        }
                        // Now set the values.
                        UnityEditor.EditorJsonUtility.FromJsonOverwrite(fbxComponent.jsonValue, prefabComponent);

                        // if the prefab component is a skinned mesh renderer, then make sure to update the bone list so that
                        // the bones are in the prefab, and not the fbx
                        if (prefabComponent is SkinnedMeshRenderer)
                        {
                            UpdateSkinnedMeshBones(prefabComponent as SkinnedMeshRenderer, prefabRoot);
                            continue;
                        }
                    }
                }
                return updatedNodes;
            }

            private void UpdateRectTransform(RectTransform rectTransform, string fbxComponentJson)
            {
                GameObject tempGameObject = new GameObject();
                try
                {
                    Transform tempTransform = tempGameObject.transform;

                    UnityEditor.EditorJsonUtility.FromJsonOverwrite(fbxComponentJson, tempTransform);

                    rectTransform.localRotation = tempTransform.localRotation;
                    rectTransform.localPosition = tempTransform.localPosition;
                    rectTransform.localScale = tempTransform.localScale;

                    #region force update of rect transform 2017.3 or newer 
                    // using reflection so we can continue to compile against versions 2017.1
                    // Retrieve the method you are looking for
                    System.Reflection.MethodInfo methodInfo =
                        rectTransform.GetType().GetMethod("ForceUpdateRectTransforms");
                    // Invoke the method on the instance 
                    if (methodInfo != null)
                        methodInfo.Invoke(rectTransform, null);
                    #endregion
                }
                finally
                {
                    GameObject.DestroyImmediate(tempGameObject);
                }
            }

            private void UpdateSkinnedMeshBones(SkinnedMeshRenderer skinnedMeshComponent, Transform prefabRoot)
            {
                var rootBone = skinnedMeshComponent.rootBone;
                var bones = skinnedMeshComponent.bones;

                var fbxAssetTransform = m_fbxPrefabUtility.FbxAsset.transform;

                var rootPrefabTransform = GetMatchingPrefabTransform(rootBone, fbxAssetTransform, prefabRoot);
                if (rootPrefabTransform == null)
                {
                    Debug.LogWarningFormat("FbxPrefabAutoUpdater: Could not find root bone {0} for {1} skinned mesh in linked prefab", rootBone.name, skinnedMeshComponent.name);
                }
                skinnedMeshComponent.rootBone = rootPrefabTransform;

                var prefabBones = new Transform[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = bones[i];

                    var prefabTransform = GetMatchingPrefabTransform(bone, fbxAssetTransform, prefabRoot);
                    if (prefabTransform == null)
                    {
                        Debug.LogWarningFormat("FbxPrefabAutoUpdater: Could not find bone {0} for {1} skinned mesh", bone.name, skinnedMeshComponent.name);
                        continue;
                    }

                    prefabBones[i] = prefabTransform;
                }
                skinnedMeshComponent.bones = prefabBones;
            }
        }

        /// <summary>
        /// Return whether this FbxPrefab component requests automatic updates.
        /// </summary>
        public bool WantsAutoUpdate()
        {
            return m_fbxPrefab.AutoUpdate;
        }

        /// <summary>
        /// Set whether this FbxPrefab component requests automatic updates.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public void SetAutoUpdate(bool autoUpdate)
        {
            if (!WantsAutoUpdate() && autoUpdate)
            {
                // We just turned autoupdate on, so update now!
                CompareAndUpdate();
            }
            m_fbxPrefab.AutoUpdate = autoUpdate;
        }

        /// <summary>
        /// Compare the old and new, and update the old according to the rules.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        void CompareAndUpdate()
        {
            // If we're not tracking anything, stop updating now.
            // (Typically this is due to a manual update.)
            if (!m_fbxPrefab.FbxModel)
            {
                return;
            }

            // First write down what we want to do.
            var updates = new UpdateList(FbxHistory, m_fbxPrefab.FbxModel.transform, m_fbxPrefab);

            // Instantiate the prefab, work on the instance, then copy back.
            // We could optimize this out if we had nothing to do, but then the
            // OnUpdate handler wouldn't always get called, and that makes for
            // confusing API.

            // This FbxPrefab may not be at the root of its prefab. We instantiate the root of the prefab, then
            // we find the corresponding FbxPrefab.
            var prefabRoot = UnityEditor.PrefabUtility.FindPrefabRoot(m_fbxPrefab.gameObject);
            var prefabInstanceRoot = UnityEditor.PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject;
            if (!prefabInstanceRoot)
            {
                throw new FbxPrefabException(string.Format("Failed to instantiate {0}; is it really a prefab?",
                    m_fbxPrefab.gameObject));
            }
            var fbxPrefabInstance = prefabInstanceRoot.GetComponentsInChildren<FbxPrefab>().FirstOrDefault(
                fbxPrefab => UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(fbxPrefab) == m_fbxPrefab);
            if (!fbxPrefabInstance)
            {
                throw new FbxPrefabException(string.Format("Internal error: couldn't find the right FbxPrefab after instantiating."));
            }

            // Do ALL the things (potentially nothing).
            var updatedObjects = updates.ImplementUpdates(fbxPrefabInstance);

            // Tell listeners about it. They're free to make adjustments now.
            FbxPrefab.CallOnUpdate(fbxPrefabInstance, updatedObjects);

            // Update the representation of the history to match the new fbx.
            var newFbxRep = new FbxRepresentation(m_fbxPrefab.FbxModel.transform);
            var newFbxRepString = newFbxRep.ToJson();
            fbxPrefabInstance.FbxHistory = newFbxRepString;

            // Save the changes back to the prefab.
            UnityEditor.PrefabUtility.ReplacePrefab(prefabInstanceRoot, prefabRoot);

            // Destroy the prefabInstance.
            GameObject.DestroyImmediate(prefabInstanceRoot);
        }

        /// <summary>
        /// Returns the fbx model we're tracking.
        /// </summary>
        public GameObject FbxAsset
        {
            get
            {
                return m_fbxPrefab.FbxModel;
            }
        }

        /// <summary>
        /// Returns the asset path of the fbx model we're tracking.
        /// </summary>
        public string FbxAssetPath
        {
            get
            {
                if (!FbxAsset) { return ""; }
                return UnityEditor.AssetDatabase.GetAssetPath(FbxAsset);
            }
        }

        /// <summary>
        /// Returns the tree representation of the fbx file as it was last time we sync'd.
        /// </summary>
        public FbxRepresentation FbxHistory
        {
            get
            {
                return new FbxRepresentation(m_fbxPrefab.FbxHistory);
            }
        }

        /// <summary>
        /// Returns the string representation of the fbx file as it was last time we sync'd.
        /// Really just for debugging.
        /// </summary>
        public string FbxHistoryString
        {
            get
            {
                return m_fbxPrefab.FbxHistory;
            }
        }

        /// <summary>
        /// Sync the prefab to match the FBX file.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
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
        [SecurityPermission(SecurityAction.LinkDemand)]
        public void SetSourceModel(GameObject fbxModel)
        {
            // Null is OK. But otherwise, fbxModel must be an fbx.
            if (fbxModel && !FbxPrefabAutoUpdater.IsFbxAsset(UnityEditor.AssetDatabase.GetAssetPath(fbxModel)))
            {
                throw new System.ArgumentException("FbxPrefab source model must be an fbx asset");
            }

            m_fbxPrefab.FbxModel = fbxModel;

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
            if (!FbxAsset)
            {
                // Case 0 or 1
                return;
            }

            if (string.IsNullOrEmpty(FbxHistoryString))
            {
                // Case 2.
                // This is the first time we've seen the FBX file. Assume that
                // it's the original FBX. Further assume that the user is happy
                // with the prefab as it is now, so don't update it to match the FBX.
                m_fbxPrefab.FbxHistory = new FbxRepresentation(m_fbxPrefab.FbxModel.transform).ToJson();
            }
            else
            {
                // Case 3.
                // User wants to reconnect or change the connection.
                // Update immediately.
                CompareAndUpdate();
            }
        }
    }

    /// <summary>
    /// Representation of a hierarchy with components.
    ///
    /// Converts to/from json for serialization, or initialize it from a
    /// Unity Transform.
    /// </summary>
    public class FbxRepresentation
    {
        /// <summary>
        /// Children of this node.
        /// The key is the name, which is assumed to be unique.
        /// Old fbx data should have Unity Object Name
        /// new fbx data should have FBX Object Name
        /// Prefab data should have Unity Object Name
        /// The value is, recursively, the representation of that subtree.
        /// </summary>
        Dictionary<string, FbxRepresentation> m_children = new Dictionary<string, FbxRepresentation>();

        /// <summary>
        /// Components of this node.
        /// The key is the name of the type of the Component. We accept that there may be several.
        /// The value is the json for the component, to be decoded with EditorJsonUtility.
        ///
        /// Note that we skip the FbxPrefab component because we never want to update that
        /// automatically.
        /// </summary>
        Dictionary<string, List<string>> m_components = new Dictionary<string, List<string>>();

        /// <summary>
        /// Build a hierarchical representation based on a transform.
        /// </summary>
        public FbxRepresentation(Transform xfo, bool isRoot = true)
        {
            if (!xfo)
            {
                return;
            }

            m_children = new Dictionary<string, FbxRepresentation>();

            foreach (Transform child in xfo)
            {
                m_children.Add(child.name, new FbxRepresentation(child, isRoot: false));
            }
            foreach (var component in xfo.GetComponents<Component>())
            {
                // ignore missing components
                if (component == null) { continue; }

                // Don't save the prefab link, to avoid a logic loop.
                if (component is FbxPrefab) { continue; }

                // Don't save the root transform, to allow importing at a place other than zero.
                if (isRoot && component is Transform) { continue; }

                var typeName = component.GetType().ToString();
                var jsonValue = UnityEditor.EditorJsonUtility.ToJson(component);
                FbxPrefabUtility.Append(ref m_components, typeName, jsonValue);
            }
        }

        /// <summary>
        /// Read an expected character from a stream (represented as string
        /// + index), skipping whitespace. Leaves the index just past the
        /// character that we read.
        ///
        /// If the character isn't found, leaves the index at the
        /// mismatched character. By default, it throws an exception;
        /// set 'required' to false to get a false return value instead.
        /// </summary>
        internal static bool Consume(char expected, string json, ref int index, bool required = true)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            while (true)
            { // loop breaks if index == json.Length
                switch (json[index])
                {
                    case ' ':
                    case '\t':
                    case '\n':
                        index++;
                        continue;
                }
                if (json[index] == expected)
                {
                    index++;
                    return true;
                }
                else if (required)
                {
                    throw new FbxPrefabException(string.Format(
                        "expected {0} at index {1} in [{2}]",
                        expected, index, json));
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Read a string from a stream (represented as string + index).
        /// Leaves the index just past the close-quote character.
        /// </summary>
        internal static string ReadString(string json, ref int index)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            int startIndex = index;
            Consume('"', json, ref index);
            var builder = new System.Text.StringBuilder();
            while (json[index] != '"')
            {
                if (index == json.Length)
                {
                    throw new FbxPrefabException(
                        string.Format("Unterminated quote in string starting at index {0}: [{1}]",
                            startIndex, json));

                }
                if (json[index] == '\\')
                {
                    // A backslash followed by a backslash or a quote outputs the
                    // next character. Otherwise it outputs itself.
                    if (index + 1 < json.Length)
                    {
                        switch (json[index + 1])
                        {
                            case '\\':
                            case '"':
                                index++;
                                break;
                        }
                    }
                }
                builder.Append(json[index]);
                index++;
            }
            Consume('"', json, ref index);
            return builder.ToString();
        }

        /// <summary>
        /// Escape a string so that ReadString can read it if we put it in
        /// quotes.
        ///
        /// This just encodes backslashes and quotes in the normal json way.
        /// It does *not* surround str with quotes.
        /// </summary>
        public static string EscapeString(string str)
        {
            if (str == null)
            {
                return null;
            }

            var builder = new System.Text.StringBuilder();
            foreach (var c in str)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }

        void InitFromJson(string json, ref int index)
        {
            Consume('{', json, ref index);
            if (Consume('}', json, ref index, required: false))
            {
                // this is a leaf; we're done.
                return;
            }
            else
            {
                do
                {
                    string name = ReadString(json, ref index);
                    Consume(':', json, ref index);

                    // hack: If the name starts with a '-' it's the name
                    // of a gameobject, and we parse it recursively. Otherwise
                    // it's the name of a component, and we store its value as a string.
                    bool isChild = (name.Length > 0) && (name[0] == '-');
                    if (isChild)
                    {
                        var subrep = new FbxRepresentation(json, ref index);
                        FbxPrefabUtility.Add(ref m_children, name.Substring(1), subrep);
                    }
                    else
                    {
                        string jsonComponent = ReadString(json, ref index);
                        FbxPrefabUtility.Append(ref m_components, name, jsonComponent);
                    }
                } while (Consume(',', json, ref index, required: false));
                Consume('}', json, ref index);
            }
        }

        public FbxRepresentation(string json, ref int index)
        {
            InitFromJson(json, ref index);
        }

        public FbxRepresentation(string json)
        {
            if (string.IsNullOrEmpty(json)) { return; }
            int index = 0;
            InitFromJson(json, ref index);
        }

        void ToJsonHelper(System.Text.StringBuilder builder)
        {
            builder.Append("{");
            bool first = true;
            if (m_children != null)
            {
                foreach (var kvp in m_children.OrderBy(kvp => kvp.Key))
                {
                    if (!first) { builder.Append(','); }
                    else { first = false; }

                    // print names with a '-' in front
                    builder.AppendFormat("\"-{0}\":", kvp.Key);
                    kvp.Value.ToJsonHelper(builder);
                }
            }
            if (m_components != null)
            {
                foreach (var kvp in m_components.OrderBy(kvp => kvp.Key))
                {
                    var name = kvp.Key;
                    foreach (var componentValue in kvp.Value)
                    {
                        if (!first) { builder.Append(','); }
                        else { first = false; }

                        // print component name and value, but escape the value
                        // string to make sure we can parse it later
                        builder.AppendFormat("\"{0}\": \"{1}\"", name,
                            EscapeString(componentValue));
                    }
                }
            }
            builder.Append("}");
        }

        public string ToJson()
        {
            var builder = new System.Text.StringBuilder();
            ToJsonHelper(builder);
            return builder.ToString();
        }

        public HashSet<string> ChildNames { get { return new HashSet<string>(m_children.Keys); } }

        public FbxRepresentation GetChild(string childName)
        {
            FbxRepresentation child;
            if (m_children.TryGetValue(childName, out child))
            {
                return child;
            }
            return null;
        }

        public HashSet<string> ComponentTypes { get { return new HashSet<string>(m_components.Keys); } }

        public List<string> GetComponentValues(string componentType)
        {
            List<string> jsonValues;
            if (m_components.TryGetValue(componentType, out jsonValues))
            {
                return jsonValues;
            }
            return null;
        }
    }
}