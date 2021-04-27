# FBX Export settings

Use the Fbx Export Settings window to:
* Manage the display of the options windows when exporting to FBX or converting to Model Prefab Variant.
* Change the default option values for FBX File export and Model Prefab Variant conversion.
* Install the [Unity Integration](integration.md) for Autodesk® Maya®, Autodesk® Maya LT™, or Autodesk® 3ds Max® and manage the related integration settings.
* Repair FBX Prefab components if you were using version 1.3.0f1 or earlier of the FBX Exporter package.

![](images/FBXExporter_FBXExportSettingsWindow.png)



<a name="FBXSettings"></a>
## Properties

### Export Options

| Property | Function |
| :------- | :------- |
| **Display Options Window** | Enable this option to display the **Export Options** or **Convert Options** window before letting the FBX Exporter proceed when you respectively select the **Export To FBX** or **Convert To FBX Prefab Variant** menu action.<br /><br />If you disable this option, the FBX Exporter directly converts or exports without asking. It uses the last path you specified and a filename based on the selected GameObject's name. |

#### FBX File Options
| Property  |  | Function |
| :-------  | :------- | :------- |
| **Export Path**  |  | The default location where the FBX Exporter saves the exported FBX file. |
| **Export Format**  |  | The default format to use in the FBX file (**ASCII** or **Binary**). |
| **Include**  |  | This defines the default scope of the export. You can include **Model(s) Only**, **Animation Only**, or **Model(s) + Animation**. |
| **LOD level**  |  | For level of detail (LOD) groups, the default level of detail to export to. You can select **All levels**, **Highest**, or **Lowest**. <br/><br/>**Note:**<br/> • The FBX Exporter ignores LODs outside of the selected hierarchy.<br/> • The FBX Exporter does not filter out GameObjects that are used as LODs and does not export them if they are not direct descendants of their respective LOD Group. |
| **Object(s) Position**  |  | The position reference to use for the GameObjects to export.  |
|  | Local Pivot | Resets the transform of the selected GameObject or group of GameObjects to the World center.<br/><br/>If you select multiple GameObjects for export, the FBX Exporter centers these GameObjects around a shared root and keeps their relative placement unchanged. |
|  | World Absolute | Keeps world transforms unchanged during the export. |
| **Animated Skinned Mesh**  |  | Enable this option to export animation on GameObjects with skinned meshes.<br/><br/>If you disable this option, the FBX Exporter does not export animation on skinned meshes. |
| **Compatible Naming**  |  | Enable this option to make the FBX Exporter rename GameObjects and Materials on export, according to specific [character replacement rules](#compatible-naming-rules).<br /><br />This ensures a compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and Autodesk® Maya® and Autodesk® Maya LT™. |
| **Export Unrendered**  |  | Enable this option to export meshes that don't have a renderer component or that have a disabled renderer component.<br/><br/>For example, a simplified mesh used as a Mesh collider. |
|**Preserve Import Settings**  |  | Enable this option to preserve all import settings applied to an existing FBX that is overwritten in the export. If the FBX Exporter exports the GameObject as a new FBX, the import settings are not carried over. |


#### Convert to Prefab Options
| Property | Function |
| :------- | :------- |
| **Prefab Path** | The default location where the FBX Exporter saves the FBX Prefab Variant file. |
| **Export Format** | The default format to use in the FBX file (**ASCII** or **Binary**). |
| **Include** | **Convert to FBX Prefab Variant** always exports both the Models and Animation in the hierarchy. |
| **LOD level** | **Convert to FBX Prefab Variant** always exports All levels of detail (LOD) available in the hierarchy for LOD groups. |
| **Object(s) Position** | **Convert to FBX Prefab Variant** always resets the root GameObject's transform during export. However, the Prefab maintains the global transform for the root GameObject. |
| **Animated Skinned Mesh** | Enable this option to export animation on GameObjects with skinned meshes.<br/><br/>If you disable this option, the FBX Exporter does not export animation on skinned meshes. |
| **Compatible Naming** | Enable this option to make the FBX Exporter rename GameObjects and Materials on conversion, according to specific [character replacement rules](#compatible-naming-rules).<br /><br />This ensures a compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and Autodesk® Maya® and Autodesk® Maya LT™. |

#### Compatible Naming rules

During an export or a conversion to a Prefab Variant, if you enabled the **Compatible Naming** option, the FBX Exporter manages characters in Unity names as follows:
* Replaces invalid characters with underscores ("\_"). Invalid characters are all non-alphanumeric characters, except for the colon (":").
* Adds an underscore ("\_") to names that begin with a number.
* Replaces diacritics. For example, replaces "é" with "e".

**Note:** If a Material has a space in its name, the FBX Exporter replaces this space by an underscore ("_") on export. As a result, if you re-import the same GameObject in Unity, the FBX Exporter creates a new Material based on the modified name.
<br />For example, if the Material name is "Default Material", the FBX Exporter exports this Material as "Default_Material". After you re-import the same GameObject, you get twice the same material, respectively named "Default Material" and "Default_Material". If you want to keep an exact match of Materials through your export/import iterations, you must rename the involved Materials to remove any space characters before exporting.

### Integration

| Property | Function |
| :------- | :------- |
| **3D Application** | The 3D modeling software you want to integrate with Unity. The available options depend on the software you have already installed on your computer, among [the ones that the FBX Exporter currently supports](index.md#requirements).<br/><br/>Use the **[...]** (Browse) button to select the 3D modeling software if you installed it in a non-standard location. |
| **Keep Open** | Enable this option to keep the selected 3D modeling software open after installing it. |
| **Hide Native Menu** | Enable this option to hide the native **Send to Unity** menu in Autodesk® Maya® and Autodesk® Maya LT™. |
| **Install Unity Integration** | Select this button to install [Unity Integration](integration.md) for the selected **3D Application**. |

### FBX Prefab Component Updater
| Property | Function |
| :------- | :------- |
| **Run Component Updater** | Select this button to run the [Component Updater](assetstoreUpgrade.md) to repair any missing FbxPrefab components if you were using version 1.3.0f1 or earlier of the FBX Exporter package. |
