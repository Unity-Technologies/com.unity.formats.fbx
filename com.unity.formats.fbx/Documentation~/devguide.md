# Developer’s Guide

As a developer you have access to the FBX Exporter from C# scripting. You can use the basic API by providing a single GameObject or a list of GameObjects. Note that default export settings are used for exporting the GameObjects to the FBX file.

You can call the FBX Exporter from C# using methods found in the [UnityEditor.Formats.Fbx.Exporter](../api/UnityEditor.Formats.Fbx.Exporter.html) namespace, for example:

```
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

public static void ExportGameObjects(Object[] objects)
{
    string filePath = Path.Combine(Application.dataPath, "MyGame.fbx");
    ModelExporter.ExportObjects(filePath, objects);
    
    // ModelExporter.ExportObject can be used instead of 
    // ModelExporter.ExportObjects to export a single game object
}
```

## Runtime

The FBX SDK bindings can be executed during gameplay allowing import and export at runtime. Currently a custom importer/exporter needs to be written in order to do so, as the FBX Exporter is Editor only.

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

* IL2CPP is not supported
* Only 64 bit Windows and MacOS standalone player builds are supported