# Known issues and limitations

#### Limitations

* The FBX Exporter package does not support exporting `.asset` files.

* The bind poses of animated skinned Meshes are lost on export. For example, if you export an animated skinned Mesh from Unity and import it into Autodesk® Maya® and Autodesk® Maya LT™, you will not be able to set the character into the bind pose using the **Rigging** > **Skin** > **Go to Bind Pose** command.

* The FBX Exporter package ignores name or path changes when converting a Model instance.

* The FBX Exporter package does not support exporting animation only for animated Lights and Cameras from Autodesk® Maya® and Maya LT™.

* Exporting an empty mesh (MeshFilter with null sharedMesh), fails to export with ArgumentNullException.

* Integrations with Autodesk® Maya® and Autodesk® 3ds Max® are not available on Linux.

#### Incorrect skinning on Animated Skinned Mesh

If animated skinned Meshes do not export with the correct skinning, it may be because they are not in the bind pose on export.

Before exporting the animated skinned Mesh, make sure that:

* The skinned mesh animation is not being previewed in the Animation of Timeline windows, as this may cause issues on export.

* The original Rig's FBX does not contain animation.

    **NOTE**: It is currently not possible to fix this issue in Unity. You need to separate your animation from the Rig in a separate modeling software such as Autodesk® Maya® first.


#### Converting GameObjects with UI components

Converting hierarchies with UI components (for example, **RectTransform**) breaks the UI.

To work around this:

1. Prepare the hierarchy so that it has no GameObjects with UI elements before converting.
2. Add the UI elements to the FBX Linked Prefab afterwards.

#### Overwriting FBX files

If you have a Variant of an FBX Model, avoid exporting your Variant to the FBX file; otherwise your changes might be applied twice after the export. For example, if your Variant adds an object, then after exporting, you'll have two copies of the object: the one in the new FBX Model you just exported, plus the one that you had previously added to the Variant.

#### Tree primitive no longer editable after conversion

Converting a Tree primitive makes the Tree read-only.

To avoid this, make sure to convert only the Tree when finished editing. Otherwise, Undo the conversion to return to previous state where the Tree was editable.

#### Trail and line particles lose Material after being converted

If you lose Materials when converting trail or line particles, you need to re-apply the Materials to the FBX Prefab Variant after conversion.

#### Uninstalling FBX Exporter breaks Unity Recorder

If you want to uninstall the FBX Exporter package but still need to use the Unity Recorder, make sure to first remove all existing FBX Recorders you might have added in the Recorder List or in a Timeline Recorder Track:

* In the Recorder Window: right click on any FBX Recorder listed in the Recorder List (at the left of the window) and select **Delete**.
* In any Timeline of your project: look for Recorder Tracks, right click on any Recorder Clip that use an FBX Recorder, and select **Delete**, or simply delete the Recorder Track the same way.

If you have already uninstalled the FBX Exporter package and are experiencing issues with the Unity Recorder:
1. Re-install the FBX Exporter package.
2. Find and remove all FBX recorder instances (see above).
3. Uninstall the FBX Exporter from the Package Manager.

#### Exporting camera animation only from Maya gives incorrect camera rotations

When using the Unity FBX Exporter Maya plugin to export camera animation with the **File > Unity > Export Animation Only** menu option,
the resulting exported camera animation may be incorrect.

The reason for this is that using the **Export Animation Only** menu option will export only transform animation and not the camera or its animated properties.

The workaround for this issue is to export the camera with the **File > Unity > Export** menu option, which will export the camera as well as its animation.
