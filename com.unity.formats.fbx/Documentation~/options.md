# FBX Export settings

Use the Fbx Export Settings window to show or hide the Export and Convert to Model Prefab Variant UI, to change the default FBX File options and Convert To Prefab options, and to install the [Unity Integration](integration.html) for Autodesk® Maya®, Autodesk® Maya LT™, or Autodesk® 3ds Max®.

![](images/FBXExporter_FBXExportSettingsWindow.png)



<a name="FBXSettings"></a>
## Fbx Export Settings window

### Export Options

| Property:                     | Function:                                                    |
| :---------------------------- | :----------------------------------------------------------- |
| __Display Options Window__    | Check this option to hide the Convert to Model Prefab Variant and Export UI when converting or exporting. The last selected path will be used and the filename will be based on the selected object's name. |

#### FBX File Options
| Property:                     | Function:                                                    |
| :---------------------------- | :----------------------------------------------------------- |
| __Export Path__               | Specify the location where the FBX Exporter will save the FBX file. |
| __Export Format__             | Select the format to use in the FBX file (ASCII or Binary).  |
| __Include__                   | Choose whether to export both Models and Animation, only Models, or only Animations. |
| __LOD level__                 | For level of detail (LOD) groups, choose the desired level of detail to export (all, highest, or lowest). <br/><br/>**NOTES:**<br/> - The FBX Exporter ignores LODs outside of selected hierarchy.<br/> - The FBX Exporter does not filter out objects that are used as LODs and doesn't export them if they aren’t direct descendants of their respective LOD Group |
| __Object(s) Position__        | Choose whether to reset the exported objects to world center, or keep world transforms during export.<br/><br/>If you select multiple objects for export, and you choose __Local Centered__ from this drop-down menu, the FBX Exporter centers objects around a shared root while keeping their relative placement unchanged. |
| __Animated Skinned Mesh__     | Check this option to export animation on objects with skinned meshes.<br/><br/>If unchecked, the FBX Exporter does not export animation on skinned meshes. |
| __Compatible Naming__         | Check this option to control renaming the GameObject and Materials during export. <br/><br/>The FBX Exporter ensures compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and Autodesk® Maya® and Autodesk® Maya LT™. During export the FBX Exporter replaces characters in Unity names as follows:<br/> - Replaces invalid characters with underscores ("\_"). Invalid characters are all non-alphanumeric characters, except for the colon (":").<br/> - Adds an underscore ("\_") to names that begin with a number.<br/> - Replaces diacritics. For example, replaces "é" with “e”.<br/><br/>**NOTE:** If you have a Material with a space in its name, the space is replaced with an underscore ("_"). This results in a new Material being created when it is imported. For example, the Material named "Default Material" is exported as "Default_Material" and is created as a new Material when it is imported. If you want the exported Material to match an existing Material in the scene, you must manually rename the Material before exporting. |
| __Export Unrendered__         | Check this option to export meshes that either don't have a renderer component, or that have a disabled renderer component. For example, a simplified mesh used as a Mesh collider. |
|__Preserve Import Settings__   | Check this option to preserve all import settings applied to an existing fbx that will be overwritten in the export. If the GameObject is being exported as a new fbx, the import settings will not be carried over.|


#### Convert to Prefab Options
| Property:                     | Function:                                                    |
| :---------------------------- | :----------------------------------------------------------- |
| __Prefab Path__               | Specify the location where the FBX Exporter will save the FBX Prefab Variant file. |
| __Export Format__             | Select the format for the FBX Exporter to use when exporting the FBX file (ASCII or binary). |
| __Include__                   | __Convert to FBX Prefab Variant__ always exports both Models and Animation in the hierarchy. |
| __LOD level__                 | __Convert to FBX Prefab Variant__ always exports All levels of detail (LOD) available in the hierarchy for LOD groups. |
| __Object(s) Position__        | __Convert to FBX Prefab Variant__ always resets the root object's transform during export. However, the Prefab maintains the global transform for the root object. |
| __Animated Skinned Mesh__     | Check this option to export animation on objects with skinned meshes.<br/><br/>If unchecked, the FBX Exporter does not export animation on skinned meshes. |
| __Compatible Naming__         | Check this option to control renaming the GameObject and Materials during export. <br/><br/>The FBX Exporter ensures compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and Autodesk® Maya® and Autodesk® Maya LT™. During export the FBX Exporter replaces characters in Unity names as follows:<br/> - Replaces invalid characters with underscores ("\_"). Invalid characters are all non-alphanumeric characters, except for colon (":").<br/> - Adds an underscore ("\_") to names that begin with a number. - Replaces diacritics. For example, replaces "é" with “e”.<br/><br/>**NOTE:** If you have a Material with a space in its name, the space is replaced with an underscore ("_"). This results in a new Material being created when it is imported. For example, the Material named "Default Material" is exported as "Default_Material" and is created as a new Material when it is imported. If you want the exported Material to match an existing Material in the scene, you must manually rename the Material before exporting. |


### Integration
| Property:                     | Function:                                                    |
| :---------------------------- | :----------------------------------------------------------- |
| __3D Application__            | Select the 3D modeling software you want to integrate with Unity. Autodesk® Maya® 2017+, Autodesk® Maya LT™ 2017+, and Autodesk® 3ds Max® 2017+ are the three applications currently supported.<br/><br/>Click the Browse button to choose a 3D modeling software installed in a non-standard location. |
| __Keep Open__                 | Check this option to keep the selected 3D modeling software open after installing it. |
| __Hide Native Menu__          | Check this option to hide the native __Send to Unity__ menu in Autodesk® Maya® and Autodesk® Maya LT™. |
| __Install Unity Integration__ | Click this button to install [Unity Integration](integration.html) for the selected __3D Application__. |

### FBX Prefab Component Updater
| Property:                     | Function:                                                    |
| :---------------------------- | :----------------------------------------------------------- |
| __Run Component Updater__     | Click this button to run the [Component Updater](index.html#Repairs_1_3_0f_1) to repair any missing FbxPrefab components if you were using version 1.3.0f1 or earlier of the FBX Exporter package. |


