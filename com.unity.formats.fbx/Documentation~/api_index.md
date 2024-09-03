# FBX Exporter scripting API reference

The FBX Exporter package includes an API that allows to create C# scripts and applications to handle FBX export processes based on your custom needs.

## Get started

This section points out the base elements to know to get started with the FBX Exporter API.

### Use cases

* To export Unity GameObjects to FBX, use the [`ModelExporter` class](xref:UnityEditor.Formats.Fbx.Exporter.ModelExporter).  
  Depending on the method, you can specify a single GameObject or a list of GameObjects to export to FBX.

* To convert a GameObject hierarchy to an FBX Prefab Variant, use the The `ConvertToNestedPrefab` class.

### Export settings

* To use custom export settings, you can create and pass an instance of `ExportModelOptions` class with modified settings.
* If you don't pass any export settings, Unity uses default export settings.

### Underlying concepts and features

For more details about the FBX package concepts and features, refer to the user manual pages:
* [FBX Exporter features and behaviors](../manual/features-behaviors.html)
* [Export models and animations to FBX](../manual/export.html)
* [Work with FBX Prefab Variants](../manual/prefab-variants.html)

## Example scripts

### Export to FBX

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

### Create an FBX Prefab Variant

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


## FBX export at runtime

By default, the FBX Exporter is Editor only. However, you can script a custom exporter to execute the FBX SDK bindings during gameplay to perform FBX exports at runtime.

### Prerequisite

The FBX SDK bindings are Editor only by default and are not included in builds.  
To include the package in the build, add the `FBXSDK_RUNTIME` define to **Edit** > **Project Settings** > **Player** > **Other Settings** > **Scripting Define Symbols**.

### Limitations

* Only 64 bit Windows, MacOS and Ubuntu standalone player builds are supported.

### Basic exporter example script

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
