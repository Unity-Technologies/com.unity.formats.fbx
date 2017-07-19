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
        class FbxRepresentation
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
                    foreach(var kvp in c) {
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
        }

        /// <summary>
        /// Recursively perform the update.
        /// </summary>
        void TreeDiff(FbxRepresentation oldHistory,
                Transform newFbx,
                Transform instance,
                string indent = "")
        {
            // TODO: a better tree diff algorithm, e.g. Demaine et al 2009.
            //
            // For now we half-ass it: there's no possibility of handling
            //   renaming other than delete-and-add. Demaine handles it.
            // There's also no possibility of nicely handling hierarchy
            //   changes, but that's essentially impossible (NP-hard, no PTAS),
            //   so it's not likely worth even trying unless we feel like doing
            //   serious algorithmics.
            // 1. For each child in the union of the old, new, and prefab:
            //    7- old and new and prefab   => recur.
            //    6- old and new and !prefab  => skip (deleted by user)
            //    5- old and !new and prefab  => delete from prefab
            //    4- old and !new and !prefab => skip, we happen to match
            //    3- !old and new and prefab  => recur, we happen to match
            //    2- !old and new and !prefab => instantiate the new into the prefab
            //    1- !old and !new and prefab => skip (added by user)
            //    0- !old and !new and !prefab  => not possible given the loop
            var names = new HashSet<string>();
            if (oldHistory != null && oldHistory.c != null) {
                foreach(var name in oldHistory.c.Keys) { names.Add(name); }
            }
            foreach(Transform child in newFbx) { names.Add(child.name); }
            foreach(Transform child in instance) { names.Add(child.name); }

            indent += "  ";
            foreach(var name in names) {
                var isOld = (oldHistory == null) ? false : oldHistory.c.ContainsKey(name);
                var isNew = newFbx.Find(name) != null;
                var isPre = instance.Find(name) != null;
                int index = (isOld ? 4 : 0) | (isNew ? 2 : 0) | (isPre ? 1 : 0);
                switch(index) {
                    case 7:
                        Debug.Log(indent + "recur into " + name);
                        TreeDiff(oldHistory.c[name], newFbx.Find(name), instance.Find(name), indent);
                        break;
                    case 6:
                        Debug.Log(indent + "skip user-deleted " + name);
                        break;
                    case 5:
                        Debug.Log(indent + "delete " + name);
                        GameObject.DestroyImmediate(instance.Find(name).gameObject);
                        break;
                    case 4:
                        Debug.Log(indent + "skip deleted in both new and instance " + name);
                        break;
                    case 3:
                        Debug.Log(indent + "accidental match; recur into " + name);
                        TreeDiff(null, newFbx.Find(name), instance.Find(name), indent);
                        break;
                    case 2:
                        Debug.Log(indent + "instantiate into instance " + name);
                        {
                            Transform src = newFbx.Find(name);
                            Transform dst = GameObject.Instantiate(src);
                            dst.parent = instance;
                            dst.name = src.name;
                            dst.localPosition = src.localPosition;
                            dst.localRotation = src.localRotation;
                            dst.localScale = src.localScale;
                        }
                        break;
                    case 1:
                        Debug.Log(indent + "skip user-added node " + name);
                        break;
                    default:
                        // This shouldn't happen.
                        throw new System.NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Compare the old and new, and update the old according to the rules.
        /// </summary>
        void CompareAndUpdate()
        {
            // todo: only instantiate if there's a change
            var oldRep = FbxRepresentation.FromJson(m_fbxHistory);
            if (oldRep == null || oldRep.c == null) { oldRep = null; }

            // Instantiate the prefab and compare & update.
            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(this.transform) as Transform;
            TreeDiff(oldRep, m_fbxModel.transform, instance);

            // Update the representation of the history.
            var fbxSource = instance.GetComponent<FbxSource>();
            var newFbxRep = FbxRepresentation.FromTransform(m_fbxModel.transform);
            var newFbxRepString = newFbxRep.ToJson();
            Debug.Log("SyncPrefab " + m_fbxModel.name + " => " + newFbxRep.c.Count + " children: " + newFbxRepString);
            fbxSource.m_fbxHistory = newFbxRepString;

            // Save the changes back to the prefab.
            UnityEditor.PrefabUtility.ReplacePrefab(instance.gameObject,
                    this.transform, UnityEditor.ReplacePrefabOptions.ReplaceNameBased);

            // Destroy the instance.
            GameObject.DestroyImmediate(instance.gameObject);
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
            CompareAndUpdate();
        }
#endif
    }
}
