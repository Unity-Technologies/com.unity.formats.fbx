using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Unity.FbxSdk;
using System.Linq;

namespace FbxExporters
{
    namespace Editor
    {
        public static class ConvertToModel
        {
            const string GameObjectMenuItemName = "GameObject/Convert To Linked Prefab Instance...";
            const string AssetsMenuItemName = "Assets/Convert To Linked Prefab...";

            /// <summary>
            /// OnContextItem is called either:
            /// * when the user selects the menu item via the top menu (with a null MenuCommand), or
            /// * when the user selects the menu item via the context menu (in which case there's a context)
            ///
            /// OnContextItem gets called once per selected object (if the
            /// parent and child are selected, then OnContextItem will only be
            /// called on the parent)
            /// </summary>
            [MenuItem (GameObjectMenuItemName, false, 30)]
            static void OnGameObjectContextItem (MenuCommand command) {
                OnContextItem(command, SelectionMode.Editable | SelectionMode.TopLevel);
            }
            [MenuItem (AssetsMenuItemName, false, 30)]
            static void OnAssetsContextItem (MenuCommand command) {
                OnContextItem(command, SelectionMode.Assets);
            }

            static void OnContextItem (MenuCommand command, SelectionMode mode)
            {
                GameObject [] selection = null;

                if (command == null || command.context == null) {
                    // We were actually invoked from the top GameObject menu, so use the selection.
                    selection = Selection.GetFiltered<GameObject> (mode);
                } else {
                    // We were invoked from the right-click menu, so use the context of the context menu.
                    var selected = command.context as GameObject;
                    if (selected) {
                        selection = new GameObject[] { selected };
                    }
                }

                if (selection == null || selection.Length == 0) {
                    ModelExporter.DisplayNoSelectionDialog ();
                    return;
                }

                Selection.objects = CreateInstantiatedModelPrefab (selection);
            }

            /// <summary>
            // Validate the menu items defined above.
            /// </summary>
            [MenuItem (GameObjectMenuItemName, true, 30)]
            [MenuItem (AssetsMenuItemName, true, 30)]
            public static bool OnValidateMenuItem ()
            {
                return true;
            }

            /// <summary>
            /// Gets the export settings.
            /// </summary>
            public static EditorTools.ExportSettings ExportSettings {
                get { return EditorTools.ExportSettings.instance; }
            }

            /// <summary>
            /// Create instantiated model prefabs from a selection of objects.
            ///
            /// Every hierarchy in the selection will be exported, under the name of the root.
            ///
            /// If an object and one of its descendents are both selected, the descendent is not promoted to be a prefab -- we only export the root.
            /// </summary>
            /// <returns>list of instanced Model Prefabs</returns>
            /// <param name="unityGameObjectsToConvert">Unity game objects to convert to Model Prefab instances</param>
            /// <param name="path">Path to save Model Prefab; use FbxExportSettings if null</param>
            public static GameObject[] CreateInstantiatedModelPrefab (
                GameObject [] unityGameObjectsToConvert)
            {
                var toExport = ModelExporter.RemoveRedundantObjects (unityGameObjectsToConvert);
                ConvertToPrefabEditorWindow.Init (toExport);
                return toExport.ToArray();
            }

            /// <summary>
            /// Convert one object (and the hierarchy below it) to an auto-updating prefab.
            ///
            /// Returns the prefab asset that's linked to the fbx.
            ///
            /// If 'toConvert' is:
            /// (1) an object in the scene, then the hierarchy will be exported and a new auto-updating prefab created pointing to the new fbx
            /// (2) the root of an fbx asset, or the root of an instance of an fbx asset, then a new auto-updating prefab will be created pointing to the existing fbx
            /// (3) a prefab asset (but *not* if it's an instance of a prefab), then a new fbx asset will be exported and the prefab will be made to auto-update from the new fbx
            /// </summary>
            /// <returns>The prefab asset linked to an fbx file.</returns>
            /// <param name="toConvert">Object to convert.</param>
            /// <param name="fbxFullPath">Absolute platform-specific path to the fbx file. If the file already exists, it will be overwritten. May be null, in which case we construct a unique filename. Ignored if 'toConvert' is an fbx asset or is an instance of one.</param>
            /// <param name="fbxDirectoryFullPath">Absolute platform-specific path to a directory in which to put the fbx file under a unique filename. May be null, in which case we use the export settings. Ignored if 'fbxFullPath' is specified. Ignored if 'toConvert' is an fbx asset or an instance of one.</param>
            /// <param name="prefabFullPath">Absolute platform-specific path to the prefab file. If the file already exists, it will be overwritten. May be null, in which case we construct a unique filename. Ignored if 'toConvert' is a prefab asset.</param>
            /// <param name="prefabDirectoryFullPath">Absolute platform-specific path to a directory in which to put the prefab file under a unique filename. May be null, in which case we use the export settings. Ignored if 'prefabFullPath' is specified. Ignored if 'toConvert' is a prefab asset.
            public static GameObject Convert (
                GameObject toConvert,
                string fbxDirectoryFullPath = null,
                string fbxFullPath = null,
                string prefabDirectoryFullPath = null,
                string prefabFullPath = null, 
                EditorTools.ConvertToPrefabSettingsSerialize exportOptions = null)
            {
                // If we selected the something that's already backed by an FBX, don't export.
                var mainAsset = GetOrCreateFbxAsset (toConvert, fbxDirectoryFullPath, fbxFullPath, exportOptions);

                // Get 'toConvert' into an editable state. We can't edit
                // assets, and toConvert might be an asset.
                var toConvertInstance = GetOrCreateInstance (toConvert);

                // Set it up to track the FbxPrefab.
                SetupFbxPrefab (toConvertInstance, mainAsset);

                // Now get 'toConvertInstance' into a prefab. If toConvert is already a prefab,
                // this is equivalent to an 'apply' ; if it's not, we're creating a new prefab.
                var prefab = ApplyOrCreatePrefab (toConvertInstance, prefabDirectoryFullPath, prefabFullPath);

                if (toConvertInstance == toConvert) {
                    // If we were converting an instance, the caller expects
                    // the instance to have the name it got saved with.
                    var path = AssetDatabase.GetAssetPath (prefab);
                    var filename = Path.GetFileNameWithoutExtension (path);
                    toConvert.name = filename;
                } else {
                    // If 'toConvert' was an asset, we created a temp
                    // instance to add the component; destroy it.
                    Object.DestroyImmediate(toConvertInstance);
                }
                return prefab;
            }

            /// <summary>
            /// Check whether GetOrCreateFbxAsset will be exporting an fbx file, or reusing one.
            /// </summary>
            public static bool WillExportFbx(GameObject go) {
                return GetFbxAssetOrNull(go) == null;
            }

            /// <summary>
            /// Return an FBX asset that corresponds to 'toConvert'.
            ///
            /// If 'toConvert' is the root of an FBX asset, return it.
            ///
            /// If it's an instance in a scene the points to the root of an FBX asset, return that asset.
            ///
            /// Otherwise, export according to the paths and options, and return the new asset.
            /// </summary>
            /// <param name="toConvert">GameObject for which we want an fbx asset</param>
            /// <param name="fbxDirectoryFullPath">Export will choose an appropriate filename in this directory. Ignored if fbxFullPath is set. Ignored if toConvert is an fbx asset or an instance of an fbx.</param>
            /// <param name="fbxDirectoryFullPath">Export will create this file. Overrides fbxDirectoryFullPath. Ignored if toConvert is an fbx asset or an instance of an fbx.</param>
            /// <returns>The root of a model prefab asset.</returns>
            public static GameObject GetOrCreateFbxAsset(GameObject toConvert,
                    string fbxDirectoryFullPath = null,
                    string fbxFullPath = null,
                    EditorTools.ConvertToPrefabSettingsSerialize exportOptions = null)
            {
                var mainAsset = GetFbxAssetOrNull(toConvert);
                if (mainAsset) {
                    return mainAsset;
                }

                if (string.IsNullOrEmpty(fbxFullPath)) {
                    // Generate a unique filename.
                    if (string.IsNullOrEmpty (fbxDirectoryFullPath)) {
                        fbxDirectoryFullPath = FbxExporters.EditorTools.ExportSettings.GetFbxAbsoluteSavePath();
                    } else {
                        fbxDirectoryFullPath = Path.GetFullPath (fbxDirectoryFullPath);
                    }
                    var fbxBasename = ModelExporter.ConvertToValidFilename (toConvert.name + ".fbx");

                    fbxFullPath = Path.Combine (fbxDirectoryFullPath, fbxBasename);
                    if (File.Exists (fbxFullPath)) {
                        fbxFullPath = IncrementFileName (fbxDirectoryFullPath, fbxFullPath);
                    }
                }
                var projectRelativePath = EditorTools.ExportSettings.GetProjectRelativePath (fbxFullPath);

                // Make sure that the object names in the hierarchy are unique.
                // The import back in to Unity would do this automatically but
                // we prefer to control it so that the Maya artist can see the
                // same names as exist in Unity.
                EnforceUniqueNames(new GameObject[] { toConvert });

                // Export to FBX. It refreshes the database.
                {
                    var fbxActualPath = ModelExporter.ExportObject (
                                            fbxFullPath, toConvert,
                                            exportOptions != null ? exportOptions : new EditorTools.ConvertToPrefabSettingsSerialize()
                                        );
                    if (fbxActualPath != fbxFullPath) {
                        throw new System.Exception ("Failed to convert " + toConvert.name);
                    }
                }

                // Replace w Model asset. LoadMainAssetAtPath wants a path
                // relative to the project, not relative to the assets folder.
                var unityMainAsset = AssetDatabase.LoadMainAssetAtPath (projectRelativePath) as GameObject;
                if (!unityMainAsset) {
                    throw new System.Exception ("Failed to convert " + toConvert.name);
                }

                // Copy the mesh/materials from the FBX
                UpdateFromSourceRecursive (toConvert, unityMainAsset);

                return unityMainAsset;
            }

            /// <summary>
            /// Return a gameobject in the scene that corresponds to 'toConvert'.
            ///
            /// If toConvert is an asset, instantiate it and retain the link.
            /// If it's a gameobject in the scene, return it.
            /// </summary>
            public static GameObject GetOrCreateInstance(GameObject toConvert)
            {
                switch(PrefabUtility.GetPrefabType(toConvert)) {
                    case PrefabType.Prefab:
                    case PrefabType.ModelPrefab:
                        return PrefabUtility.InstantiatePrefab(toConvert) as GameObject;
                    default:
                        return toConvert;
                }
            }

            /// <summary>
            /// Check whether ApplyOrCreatePrefab will be creating a new prefab, or updating one.
            /// </summary>
            public static bool WillCreatePrefab(GameObject go) {
                return PrefabUtility.GetPrefabType(go) != PrefabType.PrefabInstance;
            }

            /// <summary>
            /// Create a prefab from 'instance', or apply 'instance' to its
            /// prefab if it's already an instance of a prefab.
            ///
            /// Return the new or updated prefab.
            /// </summary>
            public static GameObject ApplyOrCreatePrefab(GameObject instance,
                string prefabDirectoryFullPath = null,
                string prefabFullPath = null)
            {
                if(PrefabUtility.GetPrefabType(instance) == PrefabType.PrefabInstance) {
                    // Apply: there's already a prefab.
                    return PrefabUtility.ReplacePrefab(instance, PrefabUtility.GetPrefabParent(instance));
                }

                // Otherwise, create a new prefab. First choose its filename/path.
                if (string.IsNullOrEmpty(prefabFullPath)) {
                    // Generate a unique filename.
                    if (string.IsNullOrEmpty (prefabDirectoryFullPath)) {
                        prefabDirectoryFullPath = FbxExporters.EditorTools.ExportSettings.GetPrefabAbsoluteSavePath();
                    } else {
                        prefabDirectoryFullPath = Path.GetFullPath (prefabDirectoryFullPath);
                    }
                    var prefabBasename = ModelExporter.ConvertToValidFilename (instance.name + ".prefab");

                    prefabFullPath = Path.Combine (prefabDirectoryFullPath, prefabBasename);
                    if (File.Exists (prefabFullPath)) {
                        prefabFullPath = IncrementFileName (prefabDirectoryFullPath, prefabFullPath);
                    }
                }
                var prefabProjectRelativePath = EditorTools.ExportSettings.GetProjectRelativePath (prefabFullPath);
                var prefabFileName = Path.ChangeExtension(prefabProjectRelativePath, ".prefab");

                var prefab = PrefabUtility.CreatePrefab(prefabFileName, instance, ReplacePrefabOptions.ConnectToPrefab);
                if (!prefab) {
                    throw new System.Exception(string.Format("Failed to create prefab asset in [{0}]", prefabFileName));
                }
                return prefab;
            }

            /// <summary>
            /// Connect 'toSetUp' to the main asset.
            ///
            /// Adds the FbxPrefab components and links it. Does not actually create or update a prefab asset.
            /// </summary>
            /// <param name="toSetUp">Instance in the scene that we want to link to the fbx asset.</param>
            /// <param name="unityMainAsset">Main asset in the FBX.</param>
            public static void SetupFbxPrefab(GameObject toSetUp, GameObject unityMainAsset)
            {
                // Set up the FbxPrefab component so it will auto-update.
                // Make sure to delete whatever FbxPrefab history we had.
                var fbxPrefab = toSetUp.GetComponent<FbxPrefab>();
                if (fbxPrefab) {
                    Object.DestroyImmediate(fbxPrefab);
                }
                fbxPrefab = toSetUp.AddComponent<FbxPrefab>();
                var fbxPrefabUtility = new FbxPrefabAutoUpdater.FbxPrefabUtility (fbxPrefab);
                fbxPrefabUtility.SetSourceModel(unityMainAsset);
            }

            /// <summary>
            /// Returns the fbx asset on disk corresponding to the same hierarchy as is selected.
            ///
            /// Returns go if go is the root of a model prefab.
            /// Returns the prefab parent of go if it's the root of a model prefab.
            /// Returns null in all other circumstances.
            /// </summary>
            /// <returns>The root of a model prefab asset, or null.</returns>
            /// <param name="go">A gameobject either in the scene or in the assets folder.</param>
            public static GameObject GetFbxAssetOrNull(GameObject go) {
                // Children of model prefab instances will also have "model prefab instance"
                // as their prefab type, so it is important that it is the root that is selected.
                //
                // e.g. If I have the following hierarchy: 
                //      Cube
                //      -- Sphere
                //
                // Both the Cube and Sphere will have ModelPrefabInstance as their prefab type.
                // However, when selecting the Sphere to convert, we don't want to connect it to the
                // existing FBX but create a new FBX containing just the sphere.
                PrefabType unityPrefabType = PrefabUtility.GetPrefabType(go);
                switch(unityPrefabType) {
                case PrefabType.ModelPrefabInstance:
                    if (go.Equals(PrefabUtility.FindPrefabRoot (go))) {
                        return PrefabUtility.GetPrefabParent(go) as GameObject;
                    } else {
                        return null;
                    }
                case PrefabType.ModelPrefab:
                    if (go.Equals(PrefabUtility.FindPrefabRoot (go))) {
                        return go;
                    } else {
                        return null;
                    }
                default:
                    return null;
                }
            }

            /// <summary>
            /// Check if the file exists, and if it does, then increment the name.
            /// e.g. if filename is Sphere.fbx and it already exists, change it to Sphere 1.fbx.
            /// </summary>
            /// <returns>new file name.</returns>
            /// <param name="filename">Filename.</param>
            public static string IncrementFileName(string path, string filename)
            {
                string fileWithoutExt = Path.GetFileNameWithoutExtension (filename);
                string ext = Path.GetExtension (filename);
                // file, space, number, extension.
                string format = "{0} {1}{2}";

                int index = 1;

                // try extracting the current index from the name and incrementing it
                var result = System.Text.RegularExpressions.Regex.Match(fileWithoutExt, @"\d+$");
                if (result != null) {
                    var number = result.Value;

                    // Parse the number.
                    int tempIndex;
                    if (int.TryParse (number, out tempIndex)) {
                        fileWithoutExt = fileWithoutExt.Remove (fileWithoutExt.LastIndexOf (number));
                        // Change the format to remove the extra space we'd add
                        // if there weren't already a number. Also, try to use the
                        // same width (so Cube001 increments to Cube002, not Cube2).
                        format = "{0}{1:D" + number.Length + "}{2}"; // file, number with padding, extension
                        index = tempIndex+1;
                    }
                }

                string file = null;
                do {
                    file = string.Format (format, fileWithoutExt, index, ext);
                    file = Path.Combine(path, file);
                    index++;
                } while (File.Exists (file));

                return file;
            }

            /// <summary>
            /// Enforces that all object names be unique before exporting.
            /// If an object with a duplicate name is found, then it is incremented.
            /// e.g. Sphere becomes Sphere 1
            /// </summary>
            /// <param name="exportSet">Export set.</param>
            public static void EnforceUniqueNames(IEnumerable<GameObject> exportSet)
            {
                Dictionary<string, int> NameToIndexMap = new Dictionary<string, int>();
                string format = "{0} {1}";

                Queue<GameObject> queue = new Queue<GameObject>(exportSet);

                while (queue.Count > 0)
                {
                    var go = queue.Dequeue();
                    var name = go.name;
                    if (NameToIndexMap.ContainsKey(name))
                    {
                        go.name = string.Format(format, name, NameToIndexMap[name]);
                        NameToIndexMap[name]++;
                    }
                    else
                    {
                        NameToIndexMap[name] = 1;
                    }

                    foreach (Transform child in go.transform)
                    {
                        queue.Enqueue(child.gameObject);
                    }
                }
            }

            /// <summary>
            /// Updates the meshes and materials of the exported GameObjects
            /// to link to those imported from the FBX.
            /// </summary>
            /// <param name="dest">GameObject to update.</param>
            /// <param name="source">Source to update from.</param>
            public static void UpdateFromSourceRecursive(GameObject dest, GameObject source)
            {
                // recurse over orig, for each transform finding the corresponding transform in the FBX
                // and copying the meshes and materials over from the FBX
                var goDict = MapNameToSourceRecursive(dest, source);
                var q = new Queue<Transform> ();
                q.Enqueue (dest.transform);
                while (q.Count > 0) {
                    var t = q.Dequeue ();

                    if (goDict [t.name] == null) {
                        Debug.LogWarning (string.Format ("Warning: Could not find Object {0} in FBX", t.name));
                        continue;
                    } 
                    CopyComponents (t.gameObject, goDict [t.name]);
                    foreach (Transform child in t) {
                        q.Enqueue (child);
                    }
                }
            }

            /// <summary>
            /// Gets a dictionary linking dest GameObject name to source game object.
            /// </summary>
            /// <returns>Dictionary containing the name to source game object.</returns>
            /// <param name="dest">Destination GameObject.</param>
            /// <param name="source">Source GameObject.</param>
            public static Dictionary<string,GameObject> MapNameToSourceRecursive(GameObject dest, GameObject source){
                var nameToGO = new Dictionary<string,GameObject> ();

                var q = new Queue<Transform> ();
                q.Enqueue (dest.transform);
                while (q.Count > 0) {
                    var t = q.Dequeue ();
                    nameToGO [t.name] = null;
                    foreach (Transform child in t) {
                        q.Enqueue (child);
                    }
                }

                nameToGO [dest.name] = source;

                var fbxQ = new Queue<Transform> ();
                foreach (Transform child in source.transform) {
                    fbxQ.Enqueue (child);
                }

                while (fbxQ.Count > 0) {
                    var t = fbxQ.Dequeue ();
                    if (!nameToGO.ContainsKey (t.name)) {
                        Debug.LogWarning (string.Format("Warning: {0} in FBX but not in converted hierarchy", t.name));
                        continue;
                    }
                    nameToGO [t.name] = t.gameObject;
                    foreach (Transform child in t) {
                        fbxQ.Enqueue (child);
                    }
                }

                return nameToGO;
            }

            /// <summary>
            /// Copy components on the 'from' object which is the FBX,
            /// over to the 'to' object which is the object in the
            /// scene we exported.
            ///
            /// Only copy over meshes and materials, since that is all the FBX contains
            /// that is not already in the scene.
            ///
            /// The 'from' hierarchy is not modified.
            /// </summary>
            public static void CopyComponents(GameObject to, GameObject from){
                var originalComponents = new List<Component>(to.GetComponents<Component> ());

                // UNI-27534: This fixes the issue where the mesh collider would not update to point to the mesh in the fbx after export
                // Point the mesh included in the mesh collider to the mesh in the FBX file, which is the same as the one in mesh filter
                var toMeshCollider = to.GetComponent<MeshCollider>();
                var toMeshFilter = to.GetComponent<MeshFilter>();
                // if the mesh collider isn't pointing to the same mesh as in the current mesh filter then don't
                // do anything as it's probably pointing to a mesh in a different fbx
                if (toMeshCollider && toMeshFilter && toMeshCollider.sharedMesh == toMeshFilter.sharedMesh)
                {
                    var fromFilter = from.GetComponent<MeshFilter>();
                    if (fromFilter)
                    {
                        toMeshCollider.sharedMesh = fromFilter.sharedMesh;
                    }
                }

                // copy over meshes, materials, and nothing else
                foreach (var component in from.GetComponents<Component>()) {
                    // ignore missing components
                    if (component == null) {
                        continue;
                    }

                    var json = EditorJsonUtility.ToJson(component);
                    if (string.IsNullOrEmpty (json)) {
                        // this happens for missing scripts
                        continue;
                    }

                    System.Type expectedType = component.GetType();
                    Component toComponent = null;

                    // Find the component to copy to.
                    for (int i = 0, n = originalComponents.Count; i < n; i++) {
                        // ignore missing components
                        if (originalComponents [i] == null) {
                            continue;
                        }

                        if (originalComponents[i].GetType() == expectedType) {
                            // We have found the component we are looking for,
                            // remove it so we don't try to copy to it again
                            toComponent = originalComponents[i];
                            originalComponents.RemoveAt (i);
                            break;
                        }
                    }

                    if (!toComponent) {
                        // copy over mesh filter and mesh renderer to replace
                        // skinned mesh renderer
                        if (component is MeshFilter) {
                            var skinnedMesh = to.GetComponent<SkinnedMeshRenderer> ();
                            if (skinnedMesh) {
                                toComponent = to.AddComponent(component.GetType());
                                EditorJsonUtility.FromJsonOverwrite (json, toComponent);

                                var toRenderer = to.AddComponent <MeshRenderer>();
                                var fromRenderer = from.GetComponent<MeshRenderer> ();
                                if (toRenderer && fromRenderer) {
                                    EditorJsonUtility.FromJsonOverwrite (EditorJsonUtility.ToJson(fromRenderer), toRenderer);
                                }
                                Object.DestroyImmediate (skinnedMesh);
                            }
                        }
                        continue;
                    }

                    if (toComponent is SkinnedMeshRenderer) {
                        var skinnedMesh = toComponent as SkinnedMeshRenderer;
                        var fromSkinnedMesh = component as SkinnedMeshRenderer;
                        skinnedMesh.sharedMesh = fromSkinnedMesh.sharedMesh;
                        skinnedMesh.sharedMaterials = fromSkinnedMesh.sharedMaterials;
                    } else if (toComponent is MeshFilter) {
                        EditorJsonUtility.FromJsonOverwrite (json, toComponent);
                    } else if (toComponent is Renderer) {
                        var toRenderer = toComponent as Renderer;
                        var fromRenderer = component as Renderer;
                        toRenderer.sharedMaterials = fromRenderer.sharedMaterials;
                    }
                }
            }
        }
    }
}
