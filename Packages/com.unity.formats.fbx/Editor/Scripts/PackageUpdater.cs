using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FbxExporters.Editor
{
    public class PackageUpdater : AssetPostprocessor
    {
        internal static readonly string[] AssetStoreFbxPrefabDLLGuids = new string[]
        {
            "628ffbda3fdf4df4588770785d91a698", // FbxPrefab asset store package GUID
            "2d81c55c4d9d85146b1d2de96e084b63" // FbxPrefab forum package (1.1.0b1) GUID
        };

        private static readonly string[] AssetStoreEditorDLLGuids = new string[]
        {
            "660c183247865bf46a724b508ed5c1a3", // UnityFbxExporterEditor.dll asset store package GUID
            "8404f30404326e44dbf6d079450dc047" // UnityFbxExporterEditor.dll forum package (1.1.0b1) GUID
        };

        static bool ProjectContainsAssetStorePackage()
        {
            return AreAnyAssetsLoaded(AssetStoreFbxPrefabDLLGuids) || AreAnyAssetsLoaded(AssetStoreEditorDLLGuids);
        }

        static bool AreAnyAssetsLoaded(string[] guids)
        {
            foreach (var id in guids)
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id)) != null)
                    return true;
            }
            return false;
        }

        static bool UserWantsToUpgrade()
        {
            return EditorUtility.DisplayDialog(
                "FBX Exporter Asset Store Package Detected",
                "It is recommended to delete any previous package versions before installing the Package Manager package. Delete Asset Store Package?",
                "Ok");
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (ProjectContainsAssetStorePackage() && UserWantsToUpgrade())
            {
                // give popup asking user if they want to delete the old package
            }
        }
    }
}