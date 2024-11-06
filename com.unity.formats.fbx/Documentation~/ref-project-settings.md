# FBX Export Project Settings

Use the FBX Export Project Settings to:
* Manage the display of the options windows when exporting to FBX or converting to Model Prefab Variant.
* Change the default option values for FBX File export and Model Prefab Variant conversion.
* Install the [Unity Integration](integration.md) for Autodesk® Maya®, Autodesk® Maya LT™, or Autodesk® 3ds Max® and manage the related integration settings.

![](images/FBXExporter_FBXExportSettingsWindow.png)

<a name="FBXSettings"></a>

## Export Options

| Property                           | Function                                                                                                                                                                                                                                                                                                                                                                                                                         |
|:-----------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Verbose logging**                | Enable this option to get more debugging messages in the console.                                                                                                                                                                                                                                                                                                                                                                |
| **Disable Texture Path Warning**   | Enable this option to suppress a specific console message that warns about an absolute path in the exported FBX file when the export includes references to texture files.<br/><br/>This warning ensures users are informed that their exported file contains an absolute path, allowing them to take action if the paths contain sensitive information that should not be shared with others.                                   |
| **Display Options Window**         | Enable this option to display the **Export Options** or **Convert Options** window before letting the FBX Exporter proceed when you respectively select the **Export To FBX** or **Convert To FBX Prefab Variant** menu action.<br /><br />If you disable this option, the FBX Exporter directly converts or exports without asking. It uses the last path you specified and a filename based on the selected GameObject's name. |

### FBX File Options

Manage the default values of the [Export Options window](ref-export-options.md).

| Property  |  | Function |
| :-------  | :------- | :------- |
| **Export Path**  |  | The location to use by default to save the exported FBX file. |
| **Export Format**  |  | The FBX file format to use by default:**ASCII** or **Binary**. |
| **Include**  |  | The scope to use by default for the export: **Model(s) Only**, **Animation Only**, or **Model(s) + Animation**. |
| **LOD level**  |  | For level of detail (LOD) groups, the level of detail to use by default for the export: **All levels**, **Highest**, or **Lowest**. <br/><br/>**Note:**<br/> • The FBX Exporter ignores LODs outside of the selected hierarchy.<br/> • The FBX Exporter does not filter out GameObjects that are used as LODs and does not export them if they are not direct descendants of their respective LOD Group. |
| **Object(s) Position**  |  | The position reference to use by default for the GameObjects to export.  |
|  | Local Pivot | Resets the transform of the selected GameObject or group of GameObjects to the World center.<br/><br/>If you select multiple GameObjects for export, the FBX Exporter centers these GameObjects around a shared root and keeps their relative placement unchanged. |
|  | World Absolute | Keeps world transforms unchanged during the export. |
| **Animated Skinned Mesh**  |  | Enable this option to export animation on GameObjects with skinned meshes.<br/><br/>If you disable this option, the FBX Exporter does not export animation on skinned meshes. |
| **Compatible Naming**  |  | Enable this option to make the FBX Exporter rename GameObjects and Materials on export, according to specific [character replacement rules](features-behaviors-compatible-naming.md).<br /><br />This ensures a compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and these software. |
| **Export Unrendered**  |  | Enable this option to export meshes that don't have a renderer component or that have a disabled renderer component.<br/><br/>For example, a simplified mesh used as a Mesh collider. |
|**Preserve Import Settings**  |  | Enable this option to preserve all import settings applied to an existing FBX file that is overwritten during the export.<br/>If you export the GameObject as a new FBX file, the FBX Exporter does not carry over the import settings. |
| **Keep Instances** | | Enable this option to export multiple copies of the same Mesh as instances.<br/>If unchecked, the FBX Exporter exports all Meshes as unique. |
| **Embed Textures** | | Enable this option to embed textures in the exported FBX. |

### Convert to Prefab Options

Manage the default values of the [Convert Options window](ref-convert-options.md).

| Property | Function |
| :--- | :--- |
| **Prefab Path** | The location to use by default to save the FBX Prefab Variant file. |
| **Export Format** | The FBX file format to use by default:**ASCII** or **Binary**. |
| **Include** | **Convert to FBX Prefab Variant** always exports both the Models and Animation in the hierarchy. |
| **LOD level** | **Convert to FBX Prefab Variant** always exports all levels of detail (LOD) available in the hierarchy for LOD groups. |
| **Object(s) Position** | **Convert to FBX Prefab Variant** always resets the root GameObject's transform during export. However, the Prefab maintains the global transform for the root GameObject. |
| **Animated Skinned Mesh** | Enable this option to export animation on GameObjects with skinned meshes.<br/><br/>If you disable this option, the FBX Exporter does not export animation on skinned meshes. |
| **Compatible Naming** | Enable this option to make the FBX Exporter rename GameObjects and Materials on conversion, according to specific [character replacement rules](features-behaviors-compatible-naming.md).<br /><br />This ensures a compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and these software. |

## Integration

| Property | Function |
| :------- | :------- |
| **3D Application** | The 3D modeling software you want to integrate with Unity. The available options depend on the software you have already installed on your computer, among [the ones that the FBX Exporter currently supports](installation.md).<br/><br/>Use the **[...]** (Browse) button to select the 3D modeling software if you installed it in a non-standard location. |
| **Keep Open** | Enable this option to keep the selected 3D modeling software open after installing it. |
| **Hide Native Menu** | Enable this option to hide the native **Send to Unity** menu in Autodesk® Maya® and Autodesk® Maya LT™. |
| **Install Unity Integration** | Select this button to install [Unity Integration](integration.md) for the selected **3D Application**. |
