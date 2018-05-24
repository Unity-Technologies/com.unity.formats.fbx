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

        private const string DefaultInstallLocation = "Assets/FbxExporters";

        /// <summary>
        /// Map of GUID to expected path and filename of assets to delete. First try finding asset by GUID, and try path if that fails.
        /// </summary>
        private static readonly Dictionary<string, string> AssetStoreFiles = new Dictionary<string, string>
        {
            /* FBX Exporter */
            { "", "FBX_Exporter_User_Guide.pdf" },
            { "", "LICENSE.txt" },
            { "", "README.txt" },
            { "", "RELEASE_NOTES.txt" },
            { "", "UnityFbxForMax.zip" },
            { "", "UnityFbxForMaya.zip" },
            { "628ffbda3fdf4df4588770785d91a698", "UnityFbxPrefab.dll" },
            { "660c183247865bf46a724b508ed5c1a3", "Editor/UnityFbxExporterEditor.dll" },
            /* FBX SDK */
            { "", "Editor/FbxSdk/docs.zip" },
            { "", "Editor/FbxSdk/LICENSE.txt" },
            { "", "Editor/FbxSdk/README.txt" },
            { "", "Editor/FbxSdk/Plugins/UnityFbxSdk.dll" },
            { "", "Editor/FbxSdk/Plugins/x64/Windows/UnityFbxSdkNative.dll" },
            { "", "Editor/FbxSdk/Plugins/x64/MacOS/UnityFbxSdkNative.bundle" }
        };

        static bool ProjectContainsAssetStorePackage()
        {
            var guidsToCheck = new List<string>(AssetStoreEditorDLLGuids);
            guidsToCheck.AddRange(AssetStoreFbxPrefabDLLGuids);
            if(AreAnyAssetsLoaded(guidsToCheck.ToArray()))
            {
                return true;
            }

            // if couldn't find the GUID check if the dlls are loaded at the expected path
            foreach(var guid in guidsToCheck)
            {
                string path;
                if(AssetStoreFiles.TryGetValue(guid, out path))
                {
                    if(AssetImporter.GetAtPath(DefaultInstallLocation + "/" + path) != null)
                    {
                        return true;
                    }
                }
            }
            return false;
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

        static void DeleteAssetStoreAssets()
        {
            foreach(var pair in AssetStoreFiles)
            {
                var guid = pair.Key;
                var path = pair.Value;

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    assetPath = DefaultInstallLocation + "/" + path;
                }

                if (!assetPath.Contains(path))
                {
                    Debug.LogWarningFormat("FBXUpgrade: Found asset at unexpected path {0}, won't delete", assetPath);
                    continue;
                }

                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    Debug.LogWarningFormat("FBXUpgrade: Failed to delete asset at path {0}", assetPath);
                }
            }
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