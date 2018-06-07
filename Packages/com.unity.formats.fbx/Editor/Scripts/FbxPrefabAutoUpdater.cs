using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Runtime.Serialization;
using UnityEngine.Formats.Fbx.Exporter;

namespace UnityEditor.Formats.Fbx.Exporter
{
    /// <summary>
    /// Exception that denotes a likely programming error.
    /// </summary>
    [System.Serializable]
    public class FbxPrefabException : System.Exception
    {
        public FbxPrefabException() { }
        public FbxPrefabException(string message) : base(message) { }
        public FbxPrefabException(string message, System.Exception inner) : base(message, inner) { }
        protected FbxPrefabException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// This class handles updating prefabs that are linked to an FBX source file.
    ///
    /// Whenever the Unity asset database imports (or reimports) assets, this
    /// class receives the <code>OnPostprocessAllAssets</code> event.
    ///
    /// If any FBX assets were (re-)imported, this class finds prefab assets
    /// that are linked to those FBX files, and makes them sync to the new FBX.
    ///
    /// The FbxPrefab component handles the sync process. This class is limited to
    /// discovering which prefabs need to be updated automatically.
    ///
    /// All functions in this class are static: there is no reason to make an
    /// instance.
    /// </summary>
    public /*static*/ class FbxPrefabAutoUpdater : UnityEditor.AssetPostprocessor
    {
        #if COM_UNITY_FORMATS_FBX_AS_ASSET
        public const string FbxPrefabFile = "/UnityFbxPrefab.dll";
        #else
        public const string FbxPrefabFile = "Packages/com.unity.formats.fbx/Runtime/FbxPrefab.cs";
        #endif

        const string MenuItemName = "GameObject/Update from FBX";
        public static bool runningUnitTest = false;

        public static bool Verbose { get { return ExportSettings.instance.VerboseProperty; } }

        public static string FindFbxPrefabAssetPath()
        {
#if COM_UNITY_FORMATS_FBX_AS_ASSET
            // Find guids that are scripts that look like FbxPrefab.
            // That catches FbxPrefabTest too, so we have to make sure.
            var allGuids = AssetDatabase.FindAssets("FbxPrefab t:MonoScript");
            string foundPath = "";
            foreach (var guid in allGuids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(FbxPrefabFile)) {
                    if (!string.IsNullOrEmpty(foundPath)) {
                        // How did this happen? Anyway, just don't try.
                        Debug.LogWarning(string.Format("{0} found in multiple places; did you forget to delete one of these?\n{1}\n{2}",
                                FbxPrefabFile.Substring(1), foundPath, path));
                        return "";
                    }
                    foundPath = path;
                }
            }
            if (string.IsNullOrEmpty(foundPath)) {
                Debug.LogWarning(string.Format("{0} not found; are you trying to uninstall {1}?", FbxPrefabFile.Substring(1), ModelExporter.PACKAGE_UI_NAME));
            }
            return foundPath;
#else
            // In Unity 2018.1 and 2018.2.0b7, FindAssets can't find FbxPrefab.cs in a package.
            // So we hardcode the path.
            var path = FbxPrefabFile;
            if (System.IO.File.Exists(System.IO.Path.GetFullPath(path))) {
                return path;
            } else {
                Debug.LogWarningFormat("{0} not found; update FbxPrefabFile variable in FbxPrefabAutoUpdater.cs to point to FbxPrefab.cs path.", FbxPrefabFile);
                return "";
            }
#endif
        }

        public static bool IsFbxAsset(string assetPath) {
            return !string.IsNullOrEmpty(assetPath) && assetPath.ToLower().EndsWith(".fbx");
        }

        public static bool IsPrefabAsset(string assetPath) {
            return !string.IsNullOrEmpty(assetPath) && assetPath.ToLower().EndsWith(".prefab");
        }

        /// <summary>
        /// Return false if the prefab definitely does not have an
        /// FbxPrefab component that points to one of the Fbx assets
        /// that were imported.
        ///
        /// May return a false positive. This is a cheap check.
        /// </summary>
        public static bool MayHaveFbxPrefabToFbxAsset(string prefabPath,
                string fbxPrefabScriptPath, HashSet<string> fbxImported) {
            if(fbxImported == null)
            {
                return false;
            }

            var depPaths = AssetDatabase.GetDependencies(prefabPath, recursive: false);

            // If we can find the path to the FbxPrefab, check that the prefab depends on it.
            // If we can't find FbxPrefab.cs then just ignore that dependence requirement.
            if (!string.IsNullOrEmpty(fbxPrefabScriptPath)) {
                if (!depPaths.Contains(fbxPrefabScriptPath)) {
                    return false;
                }
            }

            // We found (or don't care about) the FbxPrefab, now check if we
            // depend on any of the imported fbx files.
            foreach (var dep in depPaths) {
                if (fbxImported.Contains(dep)) {
                    return true;
                }
            }
            return false;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            // Do not start if Auto Updater is disabled in FBX Exporter Settings
            if (!UnityEditor.Formats.Fbx.Exporter.ExportSettings.instance.AutoUpdaterEnabled)
            {
                return;
            }

            if (Verbose) {
                Debug.Log ("Postprocessing...");
            }

            // Did we import an fbx file at all?
            // Optimize to not allocate in the common case of 'no'
            HashSet<string> fbxImported = null;
            foreach (var fbxModel in imported) {
                if (IsFbxAsset(fbxModel)) {
                    if (fbxImported == null)
                    {
                        fbxImported = new HashSet<string>();
                    }
                    fbxImported.Add(fbxModel);
                    if (Verbose) {
                        Debug.Log ("Tracking fbx asset " + fbxModel);
                    }
                } else {
                    if (Verbose) {
                        Debug.Log ("Not an fbx asset " + fbxModel);
                    }
                }
            }
            if (fbxImported == null) {
                if(Verbose){
                    Debug.Log("No fbx imported");
                }
                return;
            }

            //
            // Iterate over all the prefabs that have an FbxPrefab component that
            // points to an FBX file that got (re)-imported.
            //
            // There's no one-line query to get those, so we search for a much
            // larger set and whittle it down, hopefully without needing to
            // load the asset into memory if it's not necessary.
            //
            var fbxPrefabScriptPath = FindFbxPrefabAssetPath();
            var allObjectGuids = AssetDatabase.FindAssets("t:GameObject");
            foreach (var guid in allObjectGuids) {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsPrefabAsset(prefabPath)) {
                    if (Verbose) {
                        Debug.Log ("Not a prefab: " + prefabPath);
                    }
                    continue;
                }
                if (!MayHaveFbxPrefabToFbxAsset(prefabPath, fbxPrefabScriptPath, fbxImported)) {
                    if(Verbose){
                        Debug.Log("No dependence: " + prefabPath);
                    }
                    continue;
                }
                if (Verbose) {
                    Debug.Log ("Considering updating prefab " + prefabPath);
                }

                // We're now guaranteed that this is a prefab, and it depends
                // on the FbxPrefab script, and it depends on an Fbx file that
                // was imported.
                //
                // To be sure it has an FbxPrefab component that points to an
                // Fbx file, we need to load the asset (which we need to do to
                // update the prefab anyway).
                var prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
                if (!prefab) {
                    if (Verbose) {
                        Debug.LogWarning ("FbxPrefab reimport: failed to update prefab " + prefabPath);
                    }
                    continue;
                }
                foreach (var fbxPrefabComponent in prefab.GetComponentsInChildren<FbxPrefab>()) {
                    var fbxPrefabUtility = new FbxPrefabUtility(fbxPrefabComponent);
                    if (!fbxPrefabUtility.WantsAutoUpdate()) {
                        if (Verbose) {
                            Debug.Log ("Not auto-updating " + prefabPath);
                        }
                        continue;
                    }
                    var fbxAssetPath = fbxPrefabUtility.FbxAssetPath;
                    if (!fbxImported.Contains(fbxAssetPath)) {
                        if (Verbose) {
                            Debug.Log ("False-positive dependence: " + prefabPath + " via " + fbxAssetPath);
                        }
                        continue;
                    }
                    if (Verbose) {
                        Debug.Log ("Updating " + prefabPath + "...");
                    }
                    fbxPrefabUtility.SyncPrefab();
                }
            }
        }
        /// <summary>
        /// Add an option "Update from FBX" in the contextual GameObject menu.
        /// </summary>
        [MenuItem(MenuItemName, false,31)]
        static void OnContextItem(MenuCommand command)
        {
            GameObject[] selection = null;

            if (command == null || command.context == null)
            {
                // We were actually invoked from the top GameObject menu, so use the selection.
                selection = Selection.GetFiltered<GameObject>(SelectionMode.Editable | SelectionMode.TopLevel);
            }
            else
            {
                // We were invoked from the right-click menu, so use the context of the context menu.
                var selected = command.context as GameObject;
                if (selected)
                {
                    selection = new GameObject[] { selected };
                }
            }

            foreach (GameObject selectedObject in selection)
            {
                UpdateLinkedPrefab(selectedObject);
            }
        }

        /// <summary>
        /// Validate the menu item defined by the function above.
        /// </summary>
        [MenuItem(MenuItemName, true,31)]
        public static bool OnValidateMenuItem()
        {
            GameObject[] selection = Selection.gameObjects;

            if (selection == null || selection.Length == 0)
            {
                return false;
            }

            bool containsLinkedPrefab = false;
            foreach (GameObject selectedObject in selection)
            {
                GameObject prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(selectedObject) as GameObject;
                if (prefab && prefab.GetComponentInChildren<FbxPrefab>())
                {
                    containsLinkedPrefab = true;
                    break;
                }
            }

            return containsLinkedPrefab;
        }


        /// <summary>
        /// Launch the manual update of the linked prefab specified
        /// </summary>
        public static void UpdateLinkedPrefab(GameObject prefabOrInstance)
        {
            // Find the prefab, bail if this is neither a prefab nor an instance.
            GameObject prefab;
            switch(PrefabUtility.GetPrefabType(prefabOrInstance)) {
            case PrefabType.Prefab:
                prefab = prefabOrInstance;
                break;
            case PrefabType.PrefabInstance:
                prefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabOrInstance) as GameObject;
                break;
            default:
                return;
            }

            foreach (var fbxPrefabComponent in prefab.GetComponentsInChildren<FbxPrefab>())
            {
                // Launch the manual update UI to allow the user to fix
                // renamed nodes (or auto-update if there's nothing to rename).
                var fbxPrefabUtility = new FbxPrefabUtility(fbxPrefabComponent);

                if (UnityEditor.Formats.Fbx.Exporter.ExportSettings.instance.AutoUpdaterEnabled || runningUnitTest)
                {
                    fbxPrefabUtility.SyncPrefab();
                }
                else
                {
                    ManualUpdateEditorWindow window = (ManualUpdateEditorWindow)EditorWindow.GetWindow(typeof(ManualUpdateEditorWindow));
                    window.Init(fbxPrefabUtility, fbxPrefabComponent);
                    window.Show();
                }

            }
        }
    }
}
