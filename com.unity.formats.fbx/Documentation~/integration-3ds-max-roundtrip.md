# Use the Unity Integration tool in 3ds Max

Work in 3ds Max to import FBX files from a Unity project and re-export them after editing.

>[!NOTE]
>This page provides instructions to perform tasks in Autodesk 3ds Max using the Unity Integration tool, which [requires to be installed first](integration-setup.md).

## Import a Unity Project's FBX file

Importing an FBX Model automatically configures the plug-in for export. The plug-in remembers your Unity Project, the export filename, and which objects to export.

1. Select **Import** to open a file browser directly in your current Unity Project:

   * Select **File > Import > Import from Unity**.

     ![](images/FBXExporter_MaxUnityMenuImport.png)

2. Use the file browser to select the Models to import.

   **Note:** You can select multiple files at once.

   ![FBX import file browser in Autodesk® 3ds Max®](images/FBXExporter_MaxUnityFileBrowser.png)

### Result

The FBX Exporter automatically adds the contents of each imported file to a selection set named after the imported FBX file. For example, if you import `model.fbx`, you can find its contents in a selection set called `model_UnityExportSet`.

The FBX Exporter also creates a dummy with the same name (`model_UnityExportSet`) for each imported file and parents it under another dummy object called `UnityFbxExportSets`.

![Dummy created in Autodesk® 3ds Max® after importing Wolf.fbx](images/FBXExporter_MaxUnityExportSet.png)

This dummy contains the imported file’s path and filename as custom attributes, which the FBX Exporter also uses on export.

![Custom attributes on a UnityExportSet dummy](images/FBXExporter_MaxUnityExportSetCustomAttribs.png)

### The `@` notation

The `@` notation (`modelname@animation.fbx`), indicates that this is an animation file belonging to the Model contained in `model.fbx`.

For instance, if you import a file called `model@anim.fbx`, the export set is based on the name before the `@` symbol. Therefore, it uses the same set as `model.fbx`.

This allows you to easily import animation files and apply them to the appropriate objects in the Scene. A single animation file is supported per Model file. Importing a new animation overwrites the existing animation in the Scene.

### Limitation

You cannot export animation only from Autodesk® 3ds Max®.

### System units

If the system units are not set to centimeters, Autodesk® 3ds Max® prompts you to change them:

![](images/FBXExporter_MaxUnitWarningMsg.png)

* Click __Yes__ to change the system units to centimeters (recommended). This ensures to maintain the scaling on export.

  OR

* Click __No__ to use the current system units (not recommended). The prompt does not appear again for the remainder of the Autodesk® 3ds Max® session or, in the case of a `.max` file, does not appear again for this file.


## Re-export the objects to Unity

There are two options available for export in Autodesk® 3ds Max®:

| Option | Description |
|:-------|:------------|
| **Export** | Exports both the Models and animation contained in the export sets selected for export. |
| **Export Model Only** | Exports all Models in the selected export sets, but does not export any animation. |

Both options automatically export the files with the settings and Models you configured during import. To use them:

* From the menu, select **File > Export** and then choose **Export to Unity** or **Export to Unity (Model Only)**.

  ![](images/FBXExporter_MaxUnityMenuExport.png)

To export objects from the desired export set, you can select one or more objects in the set, the set itself, or the corresponding dummy object. In either case, the FBX Exporter exports the entire contents of the set.

If you select multiple dummy objects corresponding to sets or objects from multiple sets, then the FBX Exporter exports each set to its respective file defined in the custom attributes of the set’s dummy object.

In each case, the __Export__ option automatically exports the current Model back to Unity. When you switch back to Unity, your Scene has already been updated.
