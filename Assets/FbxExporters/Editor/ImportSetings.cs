using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

class ImportSettings : AssetPostprocessor
{
    public void OnPreprocessModel () {
        if (assetPath.EndsWith("fbx"))
        {
            foreach (string line in File.ReadAllLines(assetPath))
            {
                if (line.Contains("TypeLegacy"))
                {
                    ModelImporter modelImporter = assetImporter as ModelImporter;
                    modelImporter.animationType = ModelImporterAnimationType.Legacy;
                    break;
                }
            }
            
        }
    }
}