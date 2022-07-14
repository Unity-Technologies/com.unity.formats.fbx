# Developer’s Guide

As a developer, you have access to the FBX Exporter from C# scripting. You can use the basic API by providing a single GameObject or a list of GameObjects. 

## Managing export settings

To use custom export settings, you can create and pass an instance of `ExportModelSettingsSerialize` class with modified settings.

If you don't pass any export settings, Unity uses default export settings to export the GameObjects to the FBX file.

## Calling the FBX Exporter

You can call the FBX Exporter from C# using methods found in the [UnityEditor.Formats.Fbx.Exporter](UnityEditor.Formats.Fbx.Exporter.html) namespace, for example:

```
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

public static void ExportGameObjects(Object[] objects)
{
    string filePath = Path.Combine(Application.dataPath, "MyGame.fbx");

    ExportModelSettingsSerialize exportSettings = new ExportModelSettingsSerialize();
    exportSettings.SetExportFormat(ExportFormat.Binary);
    exportSettings.SetKeepInstances(false);

    // Note: If you don't pass any export settings, Unity uses the default settings.
    ModelExporter.ExportObjects(filePath, objects, exportSettings);

    // ModelExporter.ExportObject can be used instead of 
    // ModelExporter.ExportObjects to export a single game object
}
```

## Creating an FBX Prefab Variant 

You can convert a GameObject hierarchy to an [FBX Prefab Variant](../manual/prefabs.html) using the API. 

The principle is to export the GameObject hierarchy to an FBX and then convert the exported FBX Model Prefab into a Prefab Variant while maintaining the components from the original hierarchy.

For example:

```
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

public static GameObject ConvertGameObject(GameObject go)
{
    string filePath = Path.Combine(Application.dataPath, "MyObject.fbx");
    string prefabPath = Path.Combine(Application.dataPath, "MyObject.prefab");

    // Settings to use when exporting the FBX to convert to a prefab.
    // Note: If you don't pass any export settings, Unity uses the default settings.
    ConvertToPrefabSettingsSerialize convertSettings = new ConvertToPrefabSettingsSerialize();
    convertSettings.SetExportFormat(ExportFormat.Binary);

    // Returns the prefab variant linked to an FBX file.
    return ConvertToNestedPrefab.Convert(go, fbxFullPath: filePath, prefabFullPath: prefabPath, exportOptions: convertSettings);
}
```


## Runtime

The FBX SDK bindings can be executed during gameplay allowing import and export at runtime. Currently a custom importer/exporter needs to be written in order to do so, as the FBX Exporter is Editor only.

> **NOTE:** The FBX SDK bindings are Editor only by default and will not be included in a build. In order for the package to be included in the build, add the FBXSDK_RUNTIME define to Edit > Project Settings... > Player > Other Settings > Scripting Define Symbols.

### Basic Exporter:

```
using Autodesk.Fbx;
using UnityEngine;
using UnityEditor;

protected void ExportScene (string fileName)
{
    using(FbxManager fbxManager = FbxManager.Create ()){
        // configure IO settings.
        fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));
        
        // Export the scene
        using (FbxExporter exporter = FbxExporter.Create (fbxManager, "myExporter")) {

            // Initialize the exporter.
            bool status = exporter.Initialize (fileName, -1, fbxManager.GetIOSettings ());

            // Create a new scene to export
            FbxScene scene = FbxScene.Create (fbxManager, "myScene");

            // Export the scene to the file.
            exporter.Export (scene);
        }
    }
}
```

### Basic Importer:

```
using Autodesk.Fbx;
using UnityEngine;
using UnityEditor;

protected void ImportScene (string fileName)
{
    using(FbxManager fbxManager = FbxManager.Create ()){
        // configure IO settings.
        fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));
        
        // Import the scene to make sure file is valid
        using (FbxImporter importer = FbxImporter.Create (fbxManager, "myImporter")) {

            // Initialize the importer.
            bool status = importer.Initialize (fileName, -1, fbxManager.GetIOSettings ());

            // Create a new scene so it can be populated by the imported file.
            FbxScene scene = FbxScene.Create (fbxManager, "myScene");

            // Import the contents of the file into the scene.
            importer.Import (scene);
        }
    }
}
```

### Limitations

* Only 64 bit Windows, MacOS and Ubuntu standalone player builds are supported