# Troubleshooting

This section covers the following issues:

* [Upgrading the FBX Exporter from the Asset Store package](#AssetStoreToPackman)
* [Incorrect skinning on Animated Skinned Mesh](#SkinnedMeshExport)
* [Converting GameObjects with UI components](#ConvertUI)
* [Overwriting FBX files](#OverwritingFiles)
* [Tree primitive no longer editable after conversion](#EditableTree)
* [Trail and line particles lose material after being converted](#ParticlesLoseMaterial)
* [Uninstalling FBX Exporter breaks Unity Recorder](#BrokenRecorder)



<a name="AssetStoreToPackman"></a>

## Upgrading the FBX Exporter from the Asset Store package

When installing a new version of the FBX Exporter package after using version 1.3.0f1 or earlier, the link between Assets and Prefabs may be lost. To repair these problems, follow the instructions under [Updating from 1.3.0f1 or earlier](assetstoreUpgrade.md).



<a name="SkinnedMeshExport"></a>
## Incorrect skinning on Animated Skinned Mesh

If animated skinned Meshes do not export with the correct skinning, it may be because they are not in the bind pose on export.

Before exporting the animated skinned Mesh, make sure that:

* The skinned mesh animation is not being previewed in the Animation of Timeline windows, as this may cause issues on export.

* The original Rig's FBX does not contain animation. 
  
    **NOTE**: It is currently not possible to fix this issue in Unity. You need to separate your animation from the Rig in a separate modeling software such as Autodesk® Maya® first.



<a name="ConvertUI"></a>

## Converting GameObjects with UI components

Converting hierarchies with UI components (for example, **RectTransform**) breaks the UI. 

To work around this: 

1. Prepare the hierarchy so that it has no GameObjects with UI elements before converting.
2. Add the UI elements to the FBX Linked Prefab afterwards.



<a name="OverwritingFiles"></a>

## Overwriting FBX files

If you have a Variant of an FBX Model, avoid exporting your Variant to the FBX file; otherwise your changes might be applied twice after the export. For example, if your Variant adds an object, then after exporting, you'll have two copies of the object: the one in the new FBX Model you just exported, plus the one that you had previously added to the Variant.



<a name="EditableTree"></a>

## Tree primitive no longer editable after conversion

Converting a Tree primitive makes the Tree read-only.

To avoid this, make sure to convert only the Tree when finished editing. Otherwise, Undo the conversion to return to previous state where the Tree was editable.



<a name="ParticlesLoseMaterial"></a>

## Trail and line particles lose Material after being converted

If you lose Materials when converting trail or line particles, you need to re-apply the Materials to the FBX Linked Prefab after conversion.



<a name="BrokenRecorder"></a>

## Uninstalling FBX Exporter breaks Unity Recorder

If you are uninstalling the FBX Exporter package but would like to continue using the Unity Recorder make sure to first remove all existing FBX Recorders.
You can do this by right clicking on the FBX recorders in the Recorder Window or Timeline and selecting "Delete".

If you have already uninstalled the FBX Exporter package and are now having issues with the Unity Recorder, follow these steps:
1. Reinstall the FBX Exporter package
2. Find and remove all FBX recorder instances (right click and Delete from the Recorder Window and Timeline(s))
3. Uninstall the FBX Exporter from the Package Manager
