using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Formats.Fbx.Exporter;

namespace UnityEditor.Formats.Fbx.Exporter
{
    public class RepairLinkedPrefabs
    {

        private string[] m_assetsToRepair;
        internal string[] AssetsToRepair
        {
            get
            {
                if (m_assetsToRepair == null)
                {
                    m_assetsToRepair = FindAssetsToRepair();
                }
                return m_assetsToRepair;
            }
        }

        internal int AssetsToRepairCount
        {
            get
            {
                return AssetsToRepair.Length;
            }
        }

        private static bool AssetNeedsRepair(string filePath)
        {
            var fbxPrefab = AssetDatabase.LoadAssetAtPath(filePath, typeof(FbxPrefab));
            if(fbxPrefab != null)
            {
                return true;
            }
            return false;
        }


        internal static string[] FindAssetsToRepair()
        {
            // check all prefabs, leave scenes alone for now
            string[] searchFilePatterns = new string[] { "*.prefab" };

            List<string> assetsToRepair = new List<string>();
            foreach (string searchPattern in searchFilePatterns)
            {
                foreach (string file in Directory.GetFiles(Application.dataPath, searchPattern, SearchOption.AllDirectories))
                {
                    var assetRelPath = ExportSettings.ConvertToAssetRelativePath(file);
                    assetRelPath = "Assets/" + assetRelPath;
                    if (AssetNeedsRepair(assetRelPath))
                    {
                        assetsToRepair.Add(assetRelPath);
                    }
                }
            }
            return assetsToRepair.ToArray();
        }

        public void ConvertLinkedPrefabs()
        {
            foreach (string file in AssetsToRepair)
            {
                GameObject root = AssetDatabase.LoadMainAssetAtPath(file) as GameObject;
                if (root)
                {
                    var savePath = Path.GetDirectoryName(file);
                    ConvertToNestedPrefab.Convert(root, fbxDirectoryFullPath: savePath, prefabDirectoryFullPath: savePath);
                }
            }
            AssetDatabase.Refresh();
        }
    }
}
