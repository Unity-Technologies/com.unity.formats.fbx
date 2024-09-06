---
uid: api_index
---

# FBX Exporter scripting API

The FBX Exporter package includes an API that allows you to write C# scripts and applications to handle FBX export processes based on your custom needs.

> [!NOTE]
> While technically possible, it's not recommended to use the API for FBX import, as it wasn't designed for such a use case.


## Get started

This section points out the base elements you need to know to get started with the FBX Exporter API. [Example scripts](#example-scripts) and [additional resources](#additional-resources) are also provided below.

### Export to FBX

To export Unity GameObjects to FBX, use the [`ModelExporter` class](xref:UnityEditor.Formats.Fbx.Exporter.ModelExporter).  
Depending on the method, you can specify a single GameObject or a list of GameObjects to export to FBX.

### Convert to FBX Prefab Variant

To convert a GameObject hierarchy to an FBX Prefab Variant, use the The [`ConvertToNestedPrefab` class](xref:UnityEditor.Formats.Fbx.Exporter.ConvertToNestedPrefab).

### Use custom export settings

To use custom export settings, create and pass an instance of [`ExportModelOptions` class](xref:UnityEditor.Formats.Fbx.Exporter.ExportModelOptions) with modified settings. If you don't pass any export settings, Unity uses default export settings.

### Export FBX at runtime

By default, the FBX Exporter is Editor only and the FBX SDK bindings are not included in builds. To enable FBX export at runtime, you have to perform some Editor configuration and custom scripting.

1. Include the FBX SDK bindings in the build: go to **Edit** > **Project Settings** > **Player** > **Other Settings** > **Script Compilation** > **Scripting Define Symbols** and add `FBXSDK_RUNTIME` to the list.
1. Script a custom exporter like in the [basic example](#runtime-fbx-exporter) provided below.

> [!NOTE]
> Runtime FBX export only works with 64 bit Windows, MacOS and Ubuntu standalone player builds.

## Example scripts

### FBX export

Use this script as an example to export GameObjects to FBX files within the Unity Editor.

```
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

public static void ExportGameObjects(Object[] objects)
{
    string filePath = Path.Combine(Application.dataPath, "MyGame.fbx");

    ExportModelOptions exportSettings = new ExportModelOptions();
    exportSettings.ExportFormat = ExportFormat.Binary;
    exportSettings.KeepInstances = false;

    // Note: If you don't pass any export settings, Unity uses the default settings.
    ModelExporter.ExportObjects(filePath, objects, exportSettings);

    // You can use ModelExporter.ExportObject instead of
    // ModelExporter.ExportObjects to export a single GameObject.
}
```

### FBX Prefab Variant conversion

Use this script as an example to convert a GameObject hierarchy to an FBX Prefab Variant within the Unity Editor.

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
    ConvertToPrefabVariantOptions convertSettings = new ConvertToPrefabVariantOptions();
    convertSettings.ExportFormat = ExportFormat.Binary;

    // Returns the prefab variant linked to an FBX file.
    return ConvertToNestedPrefab.ConvertToPrefabVariant(go, fbxFullPath: filePath, prefabFullPath: prefabPath, convertOptions: convertSettings);
}
```

### Runtime FBX exporter

Use this script as an example to export FBX at runtime.

> [!NOTE]
> Before you move forward with this scenario, review the [specific requirements and implications](#export-fbx-at-runtime) about using the FBX Exporter at runtime.

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

## Additional resources

For more details about the FBX package concepts and features, refer to the user manual pages:
* [FBX Exporter features and behaviors](xref:features-behaviors)
* [Export models and animations to FBX](xref:export)
* [Work with FBX Prefab Variants](xref:prefab-variants)
