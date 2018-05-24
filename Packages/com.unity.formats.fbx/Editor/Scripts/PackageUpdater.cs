using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FbxExporters.Editor
{
    public class PackageUpdater : AssetPostprocessor
    {
        public static readonly string[] AssetStoreFbxPrefabDLLGuids = new string[]
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
            { "6de62c7df48192d4bb557c86ee2e69d3", "FBX_Exporter_User_Guide.pdf" },
            { "670c8ffd945c65246ae9ed0224ddf970", "LICENSE.txt" },
            { "fa5e8bc9da963d949955cf16217de64f", "README.txt" },
            { "34004d94a3eaab7499e7ae9e76253702", "RELEASE_NOTES.txt" },
            { "2ad26927eb7fa4a4485048b20fac7dff", "UnityFbxForMax.zip" },
            { "bd3a79df710fe68418617efeba366df4", "UnityFbxForMaya.zip" },
            { "628ffbda3fdf4df4588770785d91a698", "UnityFbxPrefab.dll" },
            { "2d81c55c4d9d85146b1d2de96e084b63", "UnityFbxPrefab.dll" }, // v1.1.0b1
            { "660c183247865bf46a724b508ed5c1a3", "Editor/UnityFbxExporterEditor.dll" },
            { "8404f30404326e44dbf6d079450dc047", "Editor/UnityFbxExporterEditor.dll" }, // v1.1.0b1
            /* FBX SDK */
            { "f864a67b9ccc5d5448cd7298bb5d13ba", "Editor/FbxSdk/docs.zip" },
            { "070080e279910ff42ad2e323bcb2a737", "Editor/FbxSdk/LICENSE.txt" },
            { "479a3739c785a11469f3ee1f463994f7", "Editor/FbxSdk/README.txt" },
            { "1fbcb7a1c49a9ac449ee7443e2202cfe", "Editor/FbxSdk/Plugins/UnityFbxSdk.dll" },
            { "b7db48465611e8542b7bc6bd41743306", "Editor/FbxSdk/Plugins/x64/Windows/UnityFbxSdkNative.dll" },
            { "d0d661670bd3fc34d8b876b0f3dd9091", "Editor/FbxSdk/Plugins/x64/MacOS/UnityFbxSdkNative.bundle" }
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
                "Ok", "Cancel");
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
                    Debug.LogWarningFormat("FBX Upgrade: Found asset at unexpected path {0}, won't delete", assetPath);
                    continue;
                }

                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    Debug.LogWarningFormat("FBX Upgrade: Failed to delete asset at path {0}", assetPath);
                    continue;
                }

                // check if containing folder is empty, delete if it is
                var fullPath = System.IO.Path.GetDirectoryName(Application.dataPath) + "/" + System.IO.Path.GetDirectoryName(assetPath);
                var dir = new System.IO.DirectoryInfo(fullPath);
                while(dir.Exists && dir.Name != "Assets" && dir.GetFiles().Length == 0 && dir.GetDirectories().Length == 0)
                {
                    dir.Delete();

                    // also delete meta file for folder
                    var metaFile = dir.FullName + ".meta";
                    if (System.IO.File.Exists(metaFile))
                    {
                        System.IO.File.Delete(metaFile);
                    }

                    dir = dir.Parent;
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (ProjectContainsAssetStorePackage() && UserWantsToUpgrade())
            {
                DeleteAssetStoreAssets();
                RepairMissingScripts.RunRepairMissingScripts();
            }
        }
    }
}