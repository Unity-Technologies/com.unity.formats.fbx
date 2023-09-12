# Use the Unity Integration tool in Maya

Work in Maya to import FBX files from a Unity project and re-export them after editing.

>[!NOTE]
>This page provides instructions to perform tasks in Autodesk Maya using the Unity Integration tool, which [requires to be installed first](integration-setup.md).

## Import a Unity Project's FBX file

Importing an FBX Model automatically configures the plug-in for export. The plug-in remembers your Unity Project, the export filenames for your Models and animations, and which objects to export per file.

1. Open a file browser directly in your current Unity Project: select __File__ > __Unity__ > __Import__.

   ![](images/FBXExporter_MayaUnityGameExporter.png)

2. Use the file browser to select the FBX files to import.

   **Note:** You can select multiple files at once: hold Shift or Ctrl to select multiple files.

   ![The FBX Import menu in Autodesk® Maya® and Autodesk® Maya LT™](images/FBXExporter_MayaUnityFileBrowser.png)

### Result

The FBX Exporter adds the contents of each imported file to an export set named after the imported FBX file. For example, if you import `model.fbx`, you can find its contents in an export set called `model_UnityExportSet`.

In addition, the contents of the file are placed into a namespace based on the filename. For `model.fbx`, the contents are placed into the `model:` namespace.

### The `@` notation

Animation files that use the `@` notation (`modelname@animation.fbx`) are recognized as animation files belonging to the Model contained in `model.fbx`.

For instance, if you import a file called `model@anim.fbx`, the export set and namespace name are based on the name before the `@` symbol. Therefore, it uses the same set and namespace as `model.fbx`.

This allows animation files to be easily imported and applied to the appropriate objects in the Scene. Autodesk® Maya® and Autodesk® Maya LT™ store the animation filename and path for easy export. A single animation file is supported per model file. Importing a new animation overwrites the existing animation in the Scene.

## Re-export the objects to Unity

There are three options available for export in Autodesk® Maya® and Autodesk® Maya LT™:

* Export
* Export Model Only
* Export Animation Only

![](images/FBXExporter_MayaUnityMenuItems.png)

| Option | Description |
|:-------|:------------|
| **File > Unity > Export** | Exports both the Models and animation contained in the export sets selected for export. |
| **File > Unity > Export Model Only** | Exports all Models in the selected export sets, but does not export any animation. |
| **File > Unity > Export Animation Only** | Exports only the animation applied to the objects in the export set as well as the minimum components required for the animation (such as transforms). |

To export objects from the desired export set, you can select one or more objects in the set, or the set itself. In either case, the FBX Exporter exports the entire contents of the set.

If you select multiple sets or objects from multiple sets, then the FBX Exporter exports each set to its respective file defined in the attributes of the set.
