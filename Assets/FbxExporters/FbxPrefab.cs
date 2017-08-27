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

        /// <summary>
        /// Utility function: log a message, make clear it's the prefab update.
        /// </summary>
        [System.Diagnostics.Conditional("FBXEXPORTER_DEBUG")]
        public static void Log(string message) {
            Debug.Log("Fbx prefab update: " + message);
        }

        [System.Diagnostics.Conditional("FBXEXPORTER_DEBUG")]
        public static void Log(string format, params object[] p) {
            Log(string.Format(format, p));
        }

        /// <summary>
        /// Utility function: create the object, which must be null.
        /// </summary>
        public static void Initialize<T>(ref T item) where T: new()
        {
            if (item != null) { throw new FbxPrefabException(); }
            item = new T();
        }

        /// <summary>
        /// Utility function: append an item to a list.
        /// If the list is null, create it.
        /// </summary>
        public static void Append<T>(ref List<T> thelist, T item)
        {
            if (thelist == null) {
                thelist = new List<T>();
            }
            thelist.Add(item);
        }

        public static void Add<K,V>(ref Dictionary<K,V> thedict, K key, V value)
        {
            if (thedict == null) {
                thedict = new Dictionary<K, V>();
            }
            thedict.Add(key, value);
        }

        public static V GetOrCreate<K,V>(Dictionary<K,V> thedict, K key) where V : new()
        {
            V value;
            if (!thedict.TryGetValue(key, out value)) {
                value = new V();
                thedict[key] = value;
            }
            return value;
        }

        /// <summary>
        /// Utility function: append an item to a list in a dictionary of lists.
        /// Create all the entries needed to append to the list.
        /// The dictionary must not be null.
        /// </summary>
        public static void Append<K, V>(ref Dictionary<K, List<V>> thedict, K key, V item)
        {
            if (thedict == null) {
                thedict = new Dictionary<K, List<V>>();
            }
            GetOrCreate(thedict, key).Add(item);
        }

        public static void Append<K, V>(Dictionary<K, List<V>> thedict, K key, V item)
        {
            GetOrCreate(thedict, key).Add(item);
        }

        /// <summary>
        /// Utility function: append an item to a list in a 2-level dictionary of lists.
        /// Create all the entries needed to append to the list.
        /// The dictionary must not be null.
        /// </summary>
        public static void Append<K1, K2, V>(ref Dictionary<K1, Dictionary<K2, List<V>>> thedict, K1 key1, K2 key2, V item)
        {
            if (thedict == null) {
                thedict = new Dictionary<K1, Dictionary<K2, List<V>>>();
            }
            var thesubmap = GetOrCreate(thedict, key1);
            Append(thesubmap, key2, item);
        }

        /// <summary>
        /// Exception that denotes a likely programming error.
        /// </summary>
        public class FbxPrefabException : System.Exception
        {
            public FbxPrefabException() { }
            public FbxPrefabException(string message) : base(message) { }
            public FbxPrefabException(string message, System.Exception inner) : base(message, inner) { }
        }

        public class FbxRepresentation
        {
            /// <summary>
            /// Children of this node.
            /// The key is the name, which is assumed to be unique.
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
                m_children = new Dictionary<string, FbxRepresentation>();
                foreach(Transform child in xfo) {
                    m_children.Add(child.name, new FbxRepresentation(child, isRoot: false));
                }
                foreach(var component in xfo.GetComponents<Component>()) {
                    // Don't save the prefab link, to avoid a logic loop.
                    if (component is FbxPrefab) { continue; }

                    // Don't save the root transform, to allow importing at a place other than zero.
                    if (isRoot && component is Transform) { continue; }

                    var typeName = component.GetType().ToString();
                    var jsonValue = UnityEditor.EditorJsonUtility.ToJson(component);
                    Append(ref m_components, typeName, jsonValue);
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
            public static bool Consume(char expected, string json, ref int index, bool required = true) {
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
                        throw new FbxPrefabException(string.Format(
                                    "expected {0} at index {1} in [{2}]",
                                    expected, index, json));
                    } else {
                        return false;
                    }
                }
            }

            /// <summary>
            /// Read a string from a stream (represented as string + index).
            /// Leaves the index just past the close-quote character.
            /// </summary>
            public static string ReadString(string json, ref int index) {
                int startIndex = index;
                Consume('"', json, ref index);
                var builder = new System.Text.StringBuilder();
                while (json[index] != '"') {
                    if (index == json.Length) {
                        throw new FbxPrefabException(
                                string.Format("Unterminated quote in string starting at index {0}: [{1}]",
                               startIndex, json));

                    }
                    if (json[index] == '\\') {
                        // A backslash followed by a backslash or a quote outputs the
                        // next character. Otherwise it outputs itself.
                        if (index + 1 < json.Length) {
                            switch(json[index + 1]) {
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
            public static string EscapeString(string str) {
                var builder = new System.Text.StringBuilder();
                foreach(var c in str) {
                    switch(c) {
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
                if (Consume('}', json, ref index, required: false)) {
                    // this is a leaf; we're done.
                    return;
                } else {
                    do {
                        string name = ReadString(json, ref index);
                        Consume(':', json, ref index);

                        // hack: If the name starts with a '-' it's the name
                        // of a gameobject, and we parse it recursively. Otherwise
                        // it's the name of a component, and we store its value as a string.
                        bool isChild = (name.Length > 0) && (name[0] == '-');
                        if (isChild) {
                            var subrep = new FbxRepresentation(json, ref index);
                            Add(ref m_children, name.Substring(1), subrep);
                        } else {
                            string jsonComponent = ReadString(json, ref index);
                            Append(ref m_components, name, jsonComponent);
                        }
                    } while(Consume(',', json, ref index, required: false));
                    Consume('}', json, ref index);
                }
            }

            public FbxRepresentation(string json, ref int index) {
                InitFromJson(json, ref index);
            }

            public FbxRepresentation(string json) {
                if (json.Length == 0) { return; }
                int index = 0;
                InitFromJson(json, ref index);
            }

            void ToJsonHelper(System.Text.StringBuilder builder) {
                builder.Append("{");
                bool first = true;
                if (m_children != null) {
                    foreach(var kvp in m_children.OrderBy(kvp => kvp.Key)) {
                        if (!first) { builder.Append(','); }
                        else { first = false; }

                        // print names with a '-' in front
                        builder.AppendFormat("\"-{0}\":", kvp.Key);
                        kvp.Value.ToJsonHelper(builder);
                    }
                }
                if (m_components != null) {
                    foreach(var kvp in m_components.OrderBy(kvp => kvp.Key)) {
                        var name = kvp.Key;
                        foreach(var componentValue in kvp.Value) {
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

            public string ToJson() {
                var builder = new System.Text.StringBuilder();
                ToJsonHelper(builder);
                return builder.ToString();
            }

            public HashSet<string> ChildNames { get { return new HashSet<string> (m_children.Keys); } }

            public FbxRepresentation GetChild(string childName) {
                FbxRepresentation child;
                if (m_children.TryGetValue(childName, out child)) {
                    return child;
                }
                return null;
            }

            public HashSet<string> ComponentTypes { get { return new HashSet<string> (m_components.Keys); } }

            public List<string> GetComponentValues(string componentType) {
                List<string> jsonValues;
                if (m_components.TryGetValue(componentType, out jsonValues)) {
                    return jsonValues;
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
                // Parent of each node, by name.
                // The empty-string name is the root of the prefab/fbx.
                Dictionary<string, string> m_parents;

                // Component value by name and type, with multiplicity.
                // name -> type -> list of value
                Dictionary<string, Dictionary<string, List<string>>> m_components;

                public Data() {
                    m_parents = new Dictionary<string, string>();
                    m_components = new Dictionary<string, Dictionary<string, List<string>>>();
                }

                public void AddNode(string name, string parent) {
                    m_parents.Add(name, parent);
                }

                public void AddComponent(string name, string typename, string jsonValue) {
                    Append(ref m_components, name, typename, jsonValue);
                }

                public void AddComponents(string name, string typename, IEnumerable<string> jsonValues) {
                    // todo: optimize this if needed. We only need to look up through the maps once.
                    foreach(var jsonValue in jsonValues) {
                        AddComponent(name, typename, jsonValue);
                    }
                }

                public bool HasNode(string name) {
                    return m_parents.ContainsKey(name);
                }

                public string GetParent(string name) {
                    string parent;
                    if (m_parents.TryGetValue(name, out parent)) {
                        return parent;
                    } else {
                        return "";
                    }
                }

                public IEnumerable<string> GetComponentTypes(string name)
                {
                    Dictionary<string, List<string>> components;
                    if (!m_components.TryGetValue(name, out components)) {
                        // node doesn't exist => it has no components
                        return new string[0];
                    }
                    return components.Keys;
                }

                public List<string> GetComponentValues(string name, string typename)
                {
                    Dictionary<string, List<string>> components;
                    if (!m_components.TryGetValue(name, out components)) {
                        return new List<string>();
                    }
                    List<string> jsonValues;
                    if (!components.TryGetValue(typename, out jsonValues)) {
                        return new List<string>();
                    }
                    return jsonValues;
                }


                public static HashSet<string> GetAllNames(params Data [] data) {
                    var names = new HashSet<string>();
                    foreach(var d in data) {
                        // the names are the keys of the name -> parent map
                        names.UnionWith(d.m_parents.Keys);
                    }
                    return names;
                }
            }

            /// <summary>
            /// Data for the hierarchy of the old fbx file, the new fbx file, and the prefab.
            /// </summary>
            Data m_old, m_new, m_prefab;

            /// <summary>
            /// Names of the new nodes to create in step 1.
            /// </summary>
            HashSet<string> m_nodesToCreate;

            /// <summary>
            /// Information about changes in parenting for step 2.
            /// Map from name of node in the prefab to name of node in prefab or newNodes.
            /// This is all the nodes in the prefab that need to be reparented.
            /// </summary>
            Dictionary<string,string> m_reparentings;

            /// <summary>
            /// Names of the nodes in the prefab to destroy in step 3.
            /// Actually calculated early.
            /// </summary>
            HashSet<string> m_nodesToDestroy;

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

            /// <summary>
            /// List of components to update or create in steps 4b and 4c.
            /// The string is the name in the prefab.
            /// The component is a pointer to the component in the FBX.
            /// If the component doesn't exist in the prefab, we create it.
            /// If the component exists in the prefab, we update the first
            ///   match (without repetition).
            /// </summary>
            Dictionary<string, List<Component>> m_componentsToUpdate;

            static void SetupDataHelper(Data data, FbxRepresentation fbxrep, string nodeName)
            {
                foreach(var typename in fbxrep.ComponentTypes) {
                    var jsonValues = fbxrep.GetComponentValues(typename);
                    data.AddComponents(nodeName, typename, jsonValues);
                }
                foreach(var child in fbxrep.ChildNames) {
                    data.AddNode(child, nodeName);
                    SetupDataHelper(data, fbxrep.GetChild(child), child);
                }
            }

            static void SetupData(ref Data data, FbxRepresentation fbxrep)
            {
                Initialize(ref data);

                // The root node has no name
                SetupDataHelper(data, fbxrep, "");
            }

            void ClassifyDestroyCreateNodes()
            {
                // Figure out which nodes to add to the prefab, which nodes in the prefab to destroy.
                Initialize(ref m_nodesToCreate);
                Initialize(ref m_nodesToDestroy);
                foreach(var name in Data.GetAllNames(m_old, m_new, m_prefab)) {
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

                // Figure out what nodes will exist after we create and destroy.
                Initialize(ref m_nodesInUpdatedPrefab);
                m_nodesInUpdatedPrefab.Add(""); // the root is nameless
                foreach(var node in Data.GetAllNames(m_prefab).Union(m_nodesToCreate)) {
                    if (m_nodesToDestroy.Contains(node)) {
                        continue;
                    }
                    m_nodesInUpdatedPrefab.Add(node);
                }
            }

            void ClassifyReparenting()
            {
                Initialize(ref m_reparentings);

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
                    if (m_nodesToDestroy.Contains(name)) {
                        // Reparent to null. This is to avoid the nuisance of
                        // trying to destroy objects that are already destroyed
                        // because a parent got there first. Maybe there's a
                        // faster way to do it, but performance seems OK.
                        m_reparentings.Add(name, "");
                        continue;
                    }

                    var prefabParent = m_prefab.GetParent(name);
                    var oldParent = m_old.GetParent(name);
                    var newParent = m_new.GetParent(name);

                    if (oldParent != newParent && prefabParent != newParent) {
                        // Conflict in this case:
                        // if (oldParent != prefabParent && !ShouldDestroy(prefabParent))

                        // For now, 'newParent' always wins:
                        m_reparentings.Add(name, newParent);
                    }
                }

                // All new nodes need to be reparented no matter what.
                // We're guaranteed we didn't already add them because we only
                // looped over what exists in the prefab now.
                foreach(var name in m_nodesToCreate) {
                    m_reparentings.Add(name, m_new.GetParent(name));
                }
            }

            void ClassifyComponents(Transform newFbx)
            {
                Initialize(ref m_componentsToDestroy);
                Initialize(ref m_componentsToUpdate);

                // Flatten the list of components in the transform hierarchy so we can remember what to copy.
                var components = new Dictionary<string, Dictionary<string, List<Component>>>();
                var builder = new System.Text.StringBuilder();
                foreach(var component in newFbx.GetComponentsInChildren<Component>()) {
                    string name;
                    if (component.transform == newFbx) {
                        name = "";
                    } else {
                        name = component.name;
                    }
                    var typename = component.GetType().ToString();
                    builder.AppendFormat("\t{0}:{1}\n", name, typename);
                    Append(ref components, name, typename, component);
                }

                // What's the logic?
                // First check if a component is present or absent. It's
                // present if the node exists and has that component; it's
                // absent if the node doesn't exist, or the node exists but
                // doesn't have that component:
                //   old  new  prefab
                //    x    x      x   => ignore
                //    x    x      y   => ignore
                //    x    y      x   => create a new component, copy from new
                //    x    y      y   => ignore
                //    y    x      x   => ignore
                //    y    x      y   => destroy the component
                //    y    y      x   => ignore
                //    y    y      y   => check if we need to update
                //
                // In that last case, check whether we need to update the values:
                //    a    a      a   => ignore
                //    a    a      b   => ignore
                //    a    a      c   => ignore (indistinguishable from aab case)
                //    a    b      a   => update to b
                //    a    b      b   => ignore (already matches)
                //    a    b      c   => conflict, update to b
                //
                // Big question: how do we handle multiplicity? I'll skip it for today...

                // We only care about nodes in the prefab after creating/destroying.
                foreach(var name in m_nodesInUpdatedPrefab)
                {
                    if (!m_new.HasNode(name)) {
                        // It's not in the FBX, so clearly we're not updating any components.
                        continue;
                    }
                    var allTypes = m_old.GetComponentTypes(name).Union(
                            m_new.GetComponentTypes(name).Union(
                                m_prefab.GetComponentTypes(name)));

                    List<string> typesToDestroy = null;
                    List<string> typesToUpdate = null;

                    foreach(var typename in allTypes) {
                        var oldValues = m_old.GetComponentValues(name, typename);
                        var newValues = m_new.GetComponentValues(name, typename);
                        var prefabValues = m_prefab.GetComponentValues(name, typename);

                        // TODO: handle multiplicity! The algorithm is eluding me right now...
                        // We'll need to do some kind of 3-way matching.
                        if (oldValues.Count > 1) { Debug.LogError("TODO: handle multiplicity " + oldValues.Count); }
                        if (newValues.Count > 1) { Debug.LogError("TODO: handle multiplicity " + newValues.Count); }
                        if (prefabValues.Count > 1) { Debug.LogError("TODO: handle multiplicity " + prefabValues.Count); }

                        if (oldValues.Count == 0 && newValues.Count != 0 && prefabValues.Count == 0) {
                            Append(ref typesToUpdate, typename);
                        }
                        else if (oldValues.Count != 0 && newValues.Count == 0 && prefabValues.Count != 0) {
                            Append(ref typesToDestroy, typename);
                        }
                        else if (oldValues.Count != 0 && newValues.Count != 0 && prefabValues.Count != 0) {
                            // Check whether we need to update.
                            var oldValue = oldValues[0];
                            var newValue = newValues[0];
                            var prefabValue = prefabValues[0];

                            if (oldValue != newValue) {
                                // if oldValue == prefabValue, update.
                                // if oldValue != prefabValue, conflict =>
                                //      resolve in favor of Chris, so update
                                //      anyway.
                                Append(ref typesToUpdate, typename);
                            }
                        }
                    }

                    // Figure out the types so we can destroy them.
                    if (typesToDestroy != null) {
                        // TODO: handle monobehaviour in other assemblies
                        // Sample use: using custom attributes in fbx to drive
                        // unity behaviour by adding a monobehaviour in an
                        // assetpostprocessor.
                        var unityEngine = typeof(Component).Assembly;
                        foreach (var typename in typesToDestroy) {
                            var thetype = unityEngine.GetType (typename);
                            Append (ref m_componentsToDestroy, name, thetype);
                        }
                    }

                    // Find the actual components in the new fbx so we can copy them.
                    if (typesToUpdate != null) {
                        foreach (var typename in typesToUpdate) {
                            if (components [name] [typename].Count > 1) {
                                Debug.LogError (string.Format("todo: multiplicity {0} on {1}:{2}",
                                    components [name] [typename].Count, name, typename));
                            }
                            Append (ref m_componentsToUpdate, name, components [name] [typename] [0]);
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
            public UpdateList(
                    FbxRepresentation oldFbx,
                    Transform newFbx,
                    FbxPrefab prefab)
            {
                SetupData(ref m_old, oldFbx);
                SetupData(ref m_new, new FbxRepresentation(newFbx));
                SetupData(ref m_prefab, new FbxRepresentation(prefab.transform));

                ClassifyDestroyCreateNodes();
                ClassifyReparenting();
                ClassifyComponents(newFbx);
            }

            public bool NeedsUpdates() {
                return m_nodesToDestroy.Count > 0
                    || m_nodesToCreate.Count > 0
                    || m_reparentings.Count > 0
                    || m_componentsToDestroy.Count > 0
                    || m_componentsToUpdate.Count > 0
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
                    Log("{0}: created new GameObject", name);
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
                    if (string.IsNullOrEmpty(parent)) {
                        parentNode = prefabInstance.transform;
                    } else {
                        parentNode = prefabNodes[parent];
                    }
                    prefabNodes[name].parent = parentNode;
                    Log("changed {0} parent to {1}", name, parentNode.name);
                }

                // Destroy the old nodes.
                foreach(var toDestroy in m_nodesToDestroy) {
                    GameObject.DestroyImmediate(prefabNodes[toDestroy].gameObject);
                    Log("destroyed {0}", toDestroy);
                }

                // Destroy the old components.
                foreach(var kvp in prefabNodes) {
                    var name = kvp.Key;
                    var xfo = kvp.Value;
                    List<System.Type> typesToDestroy;
                    if (m_componentsToDestroy.TryGetValue(name, out typesToDestroy)) {
                        foreach(var componentType in typesToDestroy) {
                            var component = xfo.GetComponent(componentType);
                            if (component != null) {
                                Object.DestroyImmediate(component);
                                Log("destroyed component {0}:{1}", xfo.name, componentType);
                            }
                        }
                    }
                }

                // Create or update the new components.
                foreach(var kvp in prefabNodes) {
                    var name = kvp.Key;
                    var prefabXfo = kvp.Value;
                    List<Component> fbxComponents;
                    if (m_componentsToUpdate.TryGetValue(name, out fbxComponents)) {
                        // Copy the components once so we can match them up even if there's multiple fbxComponents.
                        List<Component> prefabComponents = new List<Component>(prefabXfo.GetComponents<Component>());

                        foreach(var fbxComponent in fbxComponents) {
                            int index = prefabComponents.FindIndex(x => x.GetType() == fbxComponent.GetType());
                            if (index >= 0) {
                                // Don't match this index again.
                                Component prefabComponent = prefabComponents[index];
                                prefabComponents.RemoveAt(index);

                                // Now update it.
                                if (UnityEditorInternal.ComponentUtility.CopyComponent(fbxComponent)) {
                                    UnityEditorInternal.ComponentUtility.PasteComponentValues(prefabComponent);
                                    Log("updated component {0}:{1}", name, fbxComponent.GetType());

                                }
                            } else {
                                // We didn't find a match, so create the component as new.
                                if (UnityEditorInternal.ComponentUtility.CopyComponent(fbxComponent)) {
                                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(prefabXfo.gameObject);
                                    Log("added new component {0}:{1}", name, fbxComponent.GetType());
                                }
                            }
                        }
                    }
                }
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
            var newFbxRep = new FbxRepresentation(m_fbxModel.transform);
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
            return new FbxRepresentation(m_fbxHistory);
        }

        /// <summary>
        /// Returns the string representation of the fbx file as it was last time we sync'd.
        /// Really just for debugging.
        /// </summary>
        public string GetFbxHistoryString()
        {
            return m_fbxHistory;
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
                m_fbxHistory = new FbxRepresentation(m_fbxModel.transform).ToJson();
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
