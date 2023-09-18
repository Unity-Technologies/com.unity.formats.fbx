# Exported objects and attributes

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
* [Constraints](#constraints)
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

>[!NOTE]
>In Unity's Inspector, a Camera's **Physical Camera** property determines whether it is a *Physical Camera* or a *Game Camera*.

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

The FBX Exporter exports the following attributes for the Aim Constraint type:

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
