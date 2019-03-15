using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Formats.Fbx.Exporter
{
    internal static class ConvertToNestedPrefab
    {
        const string GameObjectMenuItemName = "GameObject/Convert To Nested Prefab Instance...";
        const string AssetsMenuItemName = "Assets/Convert To Nested Prefab...";

        /// <summary>
        /// OnContextItem is called either:
        /// * when the user selects the menu item via the top menu (with a null MenuCommand), or
        /// * when the user selects the menu item via the context menu (in which case there's a context)
        ///
        /// OnContextItem gets called once per selected object (if the
        /// parent and child are selected, then OnContextItem will only be
        /// called on the parent)
        /// </summary>
        [MenuItem(GameObjectMenuItemName, false, 30)]
        static void OnGameObjectContextItem(MenuCommand command)
        {
            OnContextItem(command, SelectionMode.Editable | SelectionMode.TopLevel);
        }
        [MenuItem(AssetsMenuItemName, false, 30)]
        static void OnAssetsContextItem(MenuCommand command)
        {
            OnContextItem(command, SelectionMode.Assets);
        }

        static void OnContextItem(MenuCommand command, SelectionMode mode)
        {
            GameObject[] selection = null;

            if (command == null || command.context == null)
            {
                // We were actually invoked from the top GameObject menu, so use the selection.
                selection = Selection.GetFiltered<GameObject>(mode);
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

            if (selection == null || selection.Length == 0)
            {
                ModelExporter.DisplayNoSelectionDialog();
                return;
            }

            //Selection.objects = CreateInstantiatedModelPrefab(selection);
        }

        /// <summary>
        // Validate the menu items defined above.
        /// </summary>
        [MenuItem(GameObjectMenuItemName, true, 30)]
        [MenuItem(AssetsMenuItemName, true, 30)]
        public static bool OnValidateMenuItem()
        {
            return true;
        }

        /// <summary>
        /// Gets the export settings.
        /// </summary>
        public static ExportSettings ExportSettings
        {
            get { return ExportSettings.instance; }
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
        public static GameObject[] CreateInstantiatedModelPrefab(
            GameObject[] unityGameObjectsToConvert)
        {
            // if root or descendants are prefabs, make sure to unpack
            // rename all items in hierarchy to unique names
            // export hierarchy
            // turn fbx into prefab variant
            // in prefab variant add all missing components and copy over references
            // replace hierarchy in the scene with prefab variant instance

            var toExport = ModelExporter.RemoveRedundantObjects(unityGameObjectsToConvert);

            /*if (ExportSettings.instance.ShowConvertToPrefabDialog)
            {
                ConvertToPrefabEditorWindow.Init(toExport);
                return toExport.ToArray();
            }

            var converted = new List<GameObject>();
            var exportOptions = ExportSettings.instance.ConvertToPrefabSettings.info;
            foreach (var go in toExport)
            {
                var convertedGO = Convert(go, exportOptions: exportOptions);
                if (convertedGO != null)
                {
                    converted.Add(convertedGO);
                }
            }
            return converted.ToArray();*/
        }
    }
}