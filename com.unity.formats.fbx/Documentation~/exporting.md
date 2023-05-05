# Exporting FBX files from Unity

Use __Export To FBX__ (menu: __GameObject__ > __Export To FBX__) to manually export GameObject hierarchies to an FBX file. The FBX Exporter exports selected objects and their descendants to a single FBX file. However, if you select both a parent and a descendant, only the parent’s hierarchy is exported.

The FBX Exporter exports the following objects:

* GameObject hierarchies and their transforms
* [Meshes](#meshes)
* SkinnedMeshRenderers with the following exceptions:
    * Humanoid rigs are not supported
    * Meshes in bone hierarchies are not supported
* [Materials](#materials)
* [Textures](#textures)
* [Cameras](#cameras)
* [Lights](#lights)
* [Contraints](#constraints)
* [Animation](#animation)
* Blendshapes



<a name="meshes"></a>

## Mesh support

The FBX Exporter exports multiple copies of the same Mesh as instances (to export all Meshes as unique, uncheck the *Keep Instances* option in the export settings).
The FBX Exporter also exports the following mesh attributes:

- Normals
- Binormals
- Tangents
- Vertex Colors
- All 8 Mesh UVs, if present
- Quads or Triangles

<a name="materials"></a>

## Materials

The FBX Exporter exports Unity PBS materials to FBX classic materials: Phong if the material has specular; Lambert in all other cases.
Primarily Standard and Standard (Specular) shaders are supported.

The following material properties are exported:
- Color
- Emission Color
- Bump Scale
- Specular Color (for specular materials)

<a name="textures"></a>

## Textures

The FBX Exporter can export textures as embedded, or with a link to the absolute path of the textures.
Textures can be embedded by selecting the "Embed Textures" option and "Binary" export format in the export settings on export.

The following textures are exported:
- Main Texture
- Emission Map
- Bump Map/Normal Map
- Specular Gloss Map (for specular materials)

<a name="cameras"></a>

## Cameras

The FBX Exporter exports both Game Cameras and Physical Cameras.

> **NOTE:** In Unity's Inspector, a Camera's **Physical Camera** property determines whether it is a *Physical Camera* or a *Game Camera*.

### Physical Cameras

The FBX Exporter exports Physical Cameras, including these properties:

- **Focal Length**
- **Lens Shift**
- **Focus Distance**

### Game Cameras

On export, the FBX Exporter sets the **Aperture Height** to 0.612 inches, and calculates the **Aperture Width** using this sensor back relative to the Camera's Aspect Ratio. For example:

    * Full 1024 4:3 (1024x768)
       *  Aspect Ratio 4:3
       *  Aperture Width = 0.612 * (1024/768)

The Aperture Width and Height values appear in Unity's Inspector as the **Sensor Size** property in millimeters.

The FBX Exporter derives the **Focal Length** from the vertical Field of View (FOV) and the sensor back settings (Aperture Width and Aperture Height). The FBX Exporter uses the default FBX setting for Aperture Mode: Vertical.

**Film Resolution Gate** is set to Horizontal so that the importing software fits the resolution gate horizontally within the film gate.

The **Near & Far** Clipping Plane values have a range of 30 cm to 600000 cm.

In addition, the **Projection** type (perspective/orthographic) and **Aspect Ratio** are also exported.



<a name="lights"></a>

## Lights

The FBX Exporter exports Lights of type *Directional*, *Spot*, *Point*, and *Area*.

It also exports the following Light attributes:

- Spot Angle (for Spot lights)
- Color
- Intensity
- Range
- Shadows (either On or Off)


<a name="constraints"></a>

## Constraints

The FBX Exporter exports these types of Constraints:

- [Rotation](#cns_rotation)
- [Aim](#cns_aim)
- [Position](#cns_position)
- [Scale](#cns_scale)
- [Parent](#cns_parent)

In addition, the FBX Exporter also exports the following attributes for all Constraint types:

- Sources
- Source Weight
- Weight
- Active

<a name="cns_rotation"></a>

### Rotation

The FBX Exporter exports the following attributes for the Rotation Constraint type:

- Affected axes (X,Y,Z)
- Rotation Offset
- Rest Rotation

<a name="cns_aim"></a>

### Aim

The FBX Exporter exports the following attributes for the Rotation Constraint type:

- Affected axes (X,Y,Z)
- Rotation Offset
- Rest Rotation
- World Up Type
- World Up Object
- World Up Vector
- Up Vector
- Aim Vector

<a name="cns_position"></a>

### Position

The FBX Exporter exports the following attributes for the Position Constraint type:

- Affected axes (X,Y,Z)
- Translation Offset
- Rest Translation

<a name="cns_scale"></a>

### Scale

The FBX Exporter exports the following attributes for the Scale Constraint type:

- Affected axes (X,Y,Z)
- Scale Offset
- Rest Scale

<a name="cns_parent"></a>

### Parent

The FBX Exporter exports the following attributes for the Parent Constraint type:

- Source Translation Offset
- Source Rotation Offset
- Affect Rotation Axes
- Affect Translation Axes
- Rest Translation
- Rest Rotation



<a name="animation"></a>

## Animation

The FBX Exporter exports Legacy and Generic Animation from Animation and Animator components, or from a Timeline clip.

In addition, the FBX Exporter exports the following animated attributes:

- Transforms
- Lights:
  - Intensity
  - Spot Angle (for Spot lights)
  - Color
- Cameras:
  - Field of View
- Constraints:
  - Weight
  - Source Weight
  - Translation Offset (Position Constraint)
  - Rotation Offset (Rotation Constraint and Aim Constraint)
  - Scale Offset (Scale Constraint)
  - Source Translation Offset (Parent Constraint)
  - Source Rotation Offset (Parent Constraint)
  - World Up Vector (Aim Constraint)
  - Up Vector (Aim Constraint)
  - Aim Vector (Aim Constraint)
- Blendshapes (since version 4.1.0)

### Animation curve tangents

The FBX Exporter includes the animation curve tangents when it exports an animation.

The only exception is for objects with a prerotation, such as bones of skinned meshes. In that case, the FBX Exporter bakes the curves at each frame, with the prerotation factored out. This ensures that the result matches the original animation despite slight differences between the Unity architecture and the FBX format. More precisely:
* Unity combines prerotation and rotation, while the FBX format stores them separately. Unity also stores prerotation and rotation data in a single curve.
* At export, the FBX Exporter separates the rotations into two separate fields: prerotation and local rotation. However, to split the rotation curves in the same way, the FBX Exporter would need to remove the prerotation at each key, which would affect not only the values at each key, but also the key tangents. The FBX Exporter would then need to recalculate them.

### Exporting an animation clip from the Timeline

To export an animation clip from the timeline, in the Timeline editor select the desired clip, then right click on it and select **Export Clip to FBX...** from the context menu.

### Exporting animations with the FBX Recorder

If you installed the [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html) package, you can also use the [FBX Recorder](recorder.md) to export animations to FBX files, either from a dedicated Recorder window or through a Recorder Track in Timeline.

## Export Options window

When exporting an FBX file, the following **Export Options** window opens, displaying options for specifying what gets exported.

![Export Options window](images/FBXExporter_ExportOptionsWindow.png)

### Properties

| Property:                 | Function:                                                    |
| :------------------------ | :----------------------------------------------------------- |
| __Export Name__           | Specify the name of the FBX file to export.                  |
| __Export Path__           | Specify the location where the FBX Exporter will save the FBX file. |
| __Source__                | Transfer the transform animation from this object to the __Destination__ transform. <br/><br/>**NOTES:**<br/> - __Source__ must be an ancestor of __Destination__<br/> - __Source__ may be an ancestor of the selected object. |
| __Destination__           | Which object to transfer the transform animation to.<br/><br/>This object receives the transform animation on objects between __Source__ and __Destination__ as well as the animation on the Source itself. |
| __Export Format__         | Select the format to use in the FBX file (ASCII or Binary).  |
| __Include__               | Choose whether to export both Models and Animation, only Models, or only Animations. |
| __LOD level__             | For level of detail (LOD) groups, choose the desired level of detail to export (all, highest, or lowest). <br/><br/>**NOTES:**<br/> - The FBX Exporter ignores LODs outside of selected hierarchy.<br/> - The FBX Exporter does not filter out objects that are used as LODs and doesn't export them if they aren’t direct descendants of their respective LOD Group |
| __Object(s) Position__    | Choose whether to reset the exported objects to world center, or keep world transforms during export.<br/><br/>If you select multiple objects for export, and you choose __Local Centered__ from this drop-down menu, the FBX Exporter centers objects around a shared root while keeping their relative placement unchanged. |
| __Animated Skinned Mesh__ | Enable this option to export animation on objects with skinned meshes.<br/><br/>If unchecked, the FBX Exporter does not export animation on skinned meshes. |
| __Compatible Naming__     | Enable this option to control renaming the GameObject and Materials during export. <br/><br/>The FBX Exporter ensures compatible naming with Autodesk® Maya® and Autodesk® Maya LT™ to avoid unexpected name changes between Unity and Autodesk® Maya® and Autodesk® Maya LT™. During export the FBX Exporter replaces characters in Unity names as follows:<br/> - Replaces invalid characters with underscores ("\_"). Invalid characters are all non-alphanumeric characters, except for the colon (":").<br/> - Adds an underscore ("\_") to names that begin with a number.<br/> - Replaces diacritics. For example, replaces "é" with “e”.<br/><br/>**NOTE:** If you have a Material with a space in its name, the space is replaced with an underscore ("_"). This results in a new Material being created when it is imported. For example, the Material named "Default Material" is exported as "Default_Material" and is created as a new Material when it is imported. If you want the exported Material to match an existing Material in the scene, you must manually rename the Material before exporting. |
| __Export Unrendered__     | Enable this option to export meshes that either don't have a renderer component, or that have a disabled renderer component. For example, a simplified mesh used as a Mesh collider. |
|__Preserve Import Settings__ | Enable this option to preserve all import settings applied to an existing FBX file that is overwritten during the export.<br/>If you export the GameObject as a new FBX file, the FBX Exporter does not carry over the import settings.|
| __Keep Instances__        | Enable this option to export multiple copies of the same Mesh as instances.<br/>If unchecked, the FBX Exporter exports all Meshes as unique. |
| __Embed Textures__        | Enable this option to embed textures in the exported FBX. |
| __Don't ask me again__    | Enable this option to use the same **Export Options** properties and hide this window when you export FBX files in the future.<br/>If you need to reset this property: from the Unity Editor menu, select **Edit** > **Project Settings** > **Fbx Export** and enable **Display Options Window**. |

> **Note:** For FBX Model filenames, the FBX Exporter ensures that names do not contain invalid characters for the file system. The set of invalid characters might differ between file systems.

### Default property values

If you set a Default Preset in the Preset Manager, the FBX Exporter automatically uses the values of this Preset as default property values. Otherwise, the FBX Exporter falls back to the values defined in **Edit > Project Settings... > Fbx Export** under **FBX File Options**.

However, if you modify the settings in the Export Options window during an export, the FBX Exporter preserves them as long as you keep the Unity session open.

## Exporting with relevant system units

The FBX Exporter exports in centimeter units (cm) with the Mesh set to real world meter (m) scale. For example, if vertex[0] is at [1, 1, 1] m, it is converted to [100, 100, 100] cm.

In Autodesk® 3ds Max®, it is recommended to set the system units to centimeters to avoid any scaling on Model import and export.

There are no specific import options to adjust between Unity and Autodesk® Maya® and Autodesk® Maya LT™. When working in Autodesk® Maya® and Autodesk® Maya LT™, you can set the working units to meters if you prefer.

When working with large models in Autodesk® Maya® and Autodesk® Maya LT™, to ensure that the models clip to meters, adjust the scale of the near and far clipping planes for all cameras by 100x. In addition, you should scale lights and cameras by 100x so that objects display in the viewport.
