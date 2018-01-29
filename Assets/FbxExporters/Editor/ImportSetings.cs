using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.FbxSdk;

class ImportSettings : AssetPostprocessor
{
    public void OnPreprocessModel () {
        if (assetPath.EndsWith("fbx"))
        {
            FbxManager manager = FbxManager.Create();

            using (FbxImporter importer = FbxImporter.Create(manager, "myImporter"))
            {
                //Initialize importer
                importer.Initialize(assetPath, -1, manager.GetIOSettings());

                //Create scene, and import the fbx
                FbxScene scene = FbxScene.Create(manager, "myScene");
                importer.Import(scene);

                //pull out the keywords from sceneInfo
                string keywords = scene.GetSceneInfo().mKeywords;

                if (keywords.Contains("AnimationTypeLegacy"))
                {
                    ModelImporter modelImporter = assetImporter as ModelImporter;
                    modelImporter.animationType = ModelImporterAnimationType.Legacy;
                    return;
                }
                if (keywords.Contains("AnimationTypeHumanoid"))
                {
                    ModelImporter modelImporter = assetImporter as ModelImporter;
                    modelImporter.animationType = ModelImporterAnimationType.Human;
                    return;
                }
                if (keywords.Contains("AnimationTypeGeneric"))
                {
                    ModelImporter modelImporter = assetImporter as ModelImporter;
                    modelImporter.animationType = ModelImporterAnimationType.Generic;
                }
            }
            
        }
    }
}