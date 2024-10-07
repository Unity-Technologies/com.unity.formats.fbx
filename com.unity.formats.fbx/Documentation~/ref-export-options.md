# Export Options window

Use the **Export Options** window to [export a GameObject from the Hierarchy](export-gameobjects.md) or to [export an animation Clip from the Timeline](export-timeline-clip.md) to an FBX file. It displays options for specifying what gets exported.

![Export Options window](images/FBXExporter_ExportOptionsWindow.png)

## Naming

| Property | Function |
| :--- | :--- |
| **Export Name** | The name of the FBX file to export. |
| **Export Path** | The location where the FBX Exporter saves the exported FBX file. |

## Transfer Animation

[!INCLUDE [transfer-animation](includes/transfer-animation.md)]

## Options

| Property |  | Function |
| :--- | :--- | :--- |
| **Export Format**  |  | The FBX file format:**ASCII** or **Binary**. |
| **Include**  |  | The scope of the export: **Model(s) Only**, **Animation Only**, or **Model(s) + Animation**. |
| **LOD level**  |  | For level of detail (LOD) groups, the level of detail to export to: **All levels**, **Highest**, or **Lowest**. <br/><br/>**Note:**<br/> • The FBX Exporter ignores LODs outside of the selected hierarchy.<br/> • The FBX Exporter does not filter out GameObjects that are used as LODs and does not export them if they are not direct descendants of their respective LOD Group. |
| **Object(s) Position**  |  | The position reference to use for the GameObjects to export.  |
|  | Local Pivot | Resets the transform of the selected GameObject or group of GameObjects to the World center.<br/><br/>If you select multiple GameObjects for export, the FBX Exporter centers these GameObjects around a shared root and keeps their relative placement unchanged. |
|  | World Absolute | Keeps world transforms unchanged during the export. |
| **Animated Skinned Mesh**  |  | Enable this option to export animation on GameObjects with skinned meshes.<br/><br/>If you disable this option, the FBX Exporter does not export animation on skinned meshes. |
| **Compatible Naming**  |  | Enable this option to make the FBX Exporter rename GameObjects and Materials on export, according to specific [character replacement rules](features-behaviors-compatible-naming.md).<br /><br />This ensures a compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and these software. |
| **Export Unrendered**  |  | Enable this option to export meshes that don't have a renderer component or that have a disabled renderer component.<br/><br/>For example, a simplified mesh used as a Mesh collider. |
|**Preserve Import Settings**  |  | Enable this option to preserve all import settings applied to an existing FBX file that is overwritten during the export.<br/>If you export the GameObject as a new FBX file, the FBX Exporter does not carry over the import settings. |
| **Keep Instances** | | Enable this option to export multiple copies of the same Mesh as instances.<br/>If unchecked, the FBX Exporter exports all Meshes as unique. |
| **Embed Textures** | | Enable this option to embed textures in the exported FBX. |
| **Don't ask me again** | | Enable this option to use the same **Export Options** properties and hide this window when you export FBX files in the future.<br/>If you need to reset this property: from the Unity Editor menu, select **Edit** > **Project Settings** > **Fbx Export** and enable **Display Options Window**. |

>[!NOTE]
>For FBX Model filenames, the FBX Exporter ensures that names do not contain invalid characters for the file system. The set of invalid characters might differ between file systems.

## Default property values

If you set a Default Preset in the Preset Manager, the FBX Exporter automatically uses the values of this Preset as default property values. Otherwise, the FBX Exporter falls back to the values defined in [FBX Export Project Settings](ref-project-settings.md).

However, if you modify the settings in the Export Options window during an export, the FBX Exporter preserves them as long as you keep the Unity Editor session open.
