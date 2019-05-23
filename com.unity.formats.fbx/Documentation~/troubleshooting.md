# Troubleshooting

This section covers the following issues:
    * [Upgrading the FBX Exporter from the Asset Store package](#AssetStoreToPackman)
    * [Incorrect skinning on Animated Skinned Mesh](#SkinnedMeshExport)
    * [Converting GameObjects with UI components](#ConvertUI)
    * [Overwriting FBX files](#OverwritingFiles)
    * [Tree primitive no longer editable after conversion](#EditableTree)
    * [Trail and line particles lose material after being converted](#ParticlesLoseMaterial)

<a name="AssetStoreToPackman"></a>
## Upgrading the FBX Exporter from the Asset Store package

When installing a new version of the FBX Exporter package after using version 1.3.0f1 or earlier, the link between Assets and FbxPrefab components may be lost. See [Updating from 1.3.0f1 or earlier](index.md#Repairs_1_3_0f_1) for repairing instructions.

<a name="SkinnedMeshExport"></a>
## Incorrect skinning on Animated Skinned Mesh

If animated skinned meshes do not export with the correct skinning, it may be because they are not in the bind pose on export.
Before exporting the animated skinned mesh, check the following:
* The skinned mesh animation is not being previewed in the Animation of Timeline windows, as this may cause issues on export
* The original Rig's FBX does not contain animation
    * It is currently not possible to fix this issue in Unity. Animation will first need to be separated from the Rig in a separate modeling software such as Autodesk® Maya®

<a name="ConvertUI"></a>
## Converting GameObjects with UI components

Converting hierarchies with UI components (i.e. RectTransform) will break the UI.
A workaround for this is to first prepare the hierarchy so that it has no GameObjects with UI elements before converting, then add the UI elements to the FBX Linked Prefab afterwards.

<a name="OverwritingFiles"></a>
## Overwriting FBX files

In some situations, overwriting an FBX file that is used by an FBX Linked Prefab instance, may lead to unexpected results such as additional objects being added to the Prefab hierarchy.
Therefore, we recommend not to overwrite an FBX file that is used by an FBX Linked Prefab.
    
<a name="EditableTree"></a>
## Tree primitive no longer editable after conversion

Converting a Tree primitive will make the Tree no longer editable.
Make sure to only convert the Tree when finished editing. Otherwise click undo after converting to return to previous state where the Tree was editable.

<a name="ParticlesLoseMaterial"></a>
## Trail and line particles lose material after being converted

If materials are lost when converting trail or line particles, the materials will currently need to be re-applied to the FBX Linked Prefab after conversion.