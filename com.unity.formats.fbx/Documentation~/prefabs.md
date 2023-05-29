# Working with Model Prefabs and FBX Prefab Variants

In Unity, [Nested Prefabs](https://docs.unity3d.com/Documentation/Manual/NestedPrefabs.html) allow you to create Prefabs inside other Prefabs, and [Prefab Variants](https://docs.unity3d.com/Documentation/Manual/PrefabVariants.html) allow you to save a variation on an existing Prefab.

When Unity imports a Model from a 3D modeling application such as Autodesk® Maya®, Unity creates a **Model Prefab**, which is a read-only representation of the FBX file's contents. You can't edit Model Prefabs in Unity, apart from [changing the import settings](https://docs.unity3d.com/Documentation/Manual/class-FBXImporter.html). When the FBX file is modified inside the originating 3D modeling software, Unity updates the Model Prefab.

![A Model Prefab in the Scene and Hierarchy views](images/FBXExporter_ModelPrefab.png)

If you want to add components, Materials, or change most other property values without affecting the original FBX file in Unity, convert the Model Prefab to an **FBX Prefab Variant**. This is a Prefab Variant of the exported FBX's Model Prefab. Prefab Variants allow you to override their base Prefabs' properties.

Using FBX Prefab Variants is the best way to ensure that your Models continue to reflect any changes you make to your FBX files in external applications while still taking full advantage of the Prefab system.

In addition, you can use Variants to control updates from external applications. For example, if you have a Model with a Spot Light of size 10 and you override the size to 1 in your Variant, when the size and color change in the FBX file, the color changes but the size remains 1.

> **IMPORTANT:** Because the Prefab Variant inherits data from the base Model Prefab, you cannot make structural changes to the Prefab Variant. This means that you cannot delete inherited child objects or re-arrange the order of inherited child objects on the Prefab Variant.

## Creating an FBX Prefab Variant

You can either create an FBX Prefab Variant [from a GameObject](#fromGameObject) or generate it directly [from the selected .fbx or .prefab file](#fromFBXorAssetFile). If you are upgrading from version 2.0.1-preview or earlier of the FBX Exporter, you can also [convert any existing](#conversion) Linked Prefabs you may have to the new FBX Prefab Variants.

When you convert a GameObject to an FBX Prefab Variant, the FBX Exporter exports each selected GameObject hierarchy and writes an FBX file and a Prefab Variant (`.prefab`) with the FBX as its base.

When you generate an FBX Prefab Variant from a selected file, the FBX Exporter generates the FBX Prefab Variant without modifying the Scene:

* If you select an FBX file, the FBX Exporter generates a Prefab Variant Asset file.
* If you select a Prefab Asset file, the FBX Exporter generates a Prefab Variant Asset file and an FBX file.



<a name="fromGameObject"></a>
### Converting a GameObject

To replace the GameObject hierarchy with an instance of an FBX Prefab Variant:

1. Right-click on the GameObject in the Hierarchy view and select **Convert To FBX Prefab Variant** from the context menu.

	![Convert GameObject to FBX Prefab Variant](images/FBXExporter_FBXLinkedPrefab-GameObject.png)

	Alternatively, you can use the main menu: **GameObject** > **Convert To FBX Prefab Variant** with the GameObject selected.

2. Specify how you want to export the GameObject using the properties on the [Convert Options](ref-convert-options.md) window and click **Convert**.

FBX Prefab Variants use the same rules as for exporting: all selected objects and their descendants are exported to a single FBX file. If you select both a parent and a descendant, the FBX Prefab Variant only exports the parent’s hierarchy.

> **NOTE:** If the selected object is a Model Prefab instance, then the hierarchy is not re-exported: instead the FBX Prefab Variant links to the FBX file that already exists.

<a name="fromFBXorAssetFile"></a>
### Converting an FBX file or a Prefab

To generate the FBX Prefab Variant from the selected file without modifying the Scene:

1. Right-click on an FBX or Prefab Asset file in the Project view and select **Convert To FBX Prefab Variant** from the context menu.

	![Convert Prefab file to FBX Linked Prefab](images/FBXExporter_FBXLinkedPrefab-Asset.png)

	Alternatively, you can use the main menu: **Assets** > **Convert To FBX Prefab Variant**.

2. Specify how you want to export the GameObject using the properties on the [Convert Options](ref-convert-options.md) window and click **Convert**.

Depending on which type of file you selected, the FBX Exporter creates the FBX Prefab Variant in one of the following ways:

* If an FBX file is selected, the FBX Exporter generates a Prefab Variant file with the selected FBX file as its base.
* If a Prefab Asset file is selected, the FBX Exporter exports the Prefab to an FBX file and creates a new FBX Prefab Variant.

<a name="conversion"></a>
## Converting from Linked Prefabs to FBX Prefab Variants

To convert existing Linked Prefabs to FBX Prefab Variants, follow these steps:

1. Fix any name discrepancies before converting. FBX Prefab Variants do not handle name remapping. If you start to add game logic to components, and then the objects in the FBX get renamed, you are at risk of losing the components. This can happen because the old GameObjects are deleted and new objects with the new names are added in their place.

2. Right-click the Linked Prefab file and select **Convert to FBX Prefab Variant** from the context menu.

	![Create a Prefab variant from the file's context menu](images/FBXExporter_FBXLinkedPrefab-Asset.png)

> **NOTE:** This re-exports the Linked Prefab to a new FBX file, and it is no longer connected to the original FBX.
