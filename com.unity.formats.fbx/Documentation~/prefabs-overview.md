# Working with Prefabs

In version 2018.3, Unity introduced a new way of working with Prefabs: [Nested Prefabs](https://docs.unity3d.com/2018.3/Documentation/Manual/NestedPrefabs.html) allow you to create Prefabs inside other Prefabs, and [Prefab Variants](https://docs.unity3d.com/2018.3/Documentation/Manual/PrefabVariants.html) allow you to save a variation on an existing Prefab. However, this changes the way you work with the FBX Exporter, because you can no longer use *Linked Prefabs* to maintain a connection between Unity and your 3D modeling application. 

## Linked Prefabs (Unity 2018.2)

If you are working in Unity version 2018.2, you can set up an automatic connection between Unity and your 3D modeling application using a Linked Prefab. A Linked Prefab is a script that detects changes that were made to a "linked" FBX file in Unity and then seamlessly updates the data in the Prefab. Once you establish the connection, you generally don't have to re-create the connection to bring in any updates.

* For more information, see [Working with Linked Prefabs](prefabs.md).

## Model Prefab Variants (2018.3)

In Unity version 2018.3, when you open an FBX file in Unity, the FBX Importer creates a Model Prefab. Model Prefabs are read-only, but you can create a *Prefab Variant* of that Model Prefab. Prefab Variants are Prefabs which inherit all data from a base Prefab. Prefab Variants can create inheritance between ordinary Prefabs. For example, you can create a Variant of a button Prefab which has a different color but the same functionality. 

Since Prefab Variants allow you to override its base Prefab's properties, you can use a Model Prefab Variant to add components, Materials, or change most other property values without affecting the original FBX file.

> ***Note:*** While this method allows you to override properties, you cannot change the internal hierarchy of the Variant without breaking the link to the Model Prefab.

This type of connection is easy to maintain.

* For information on Model Prefabs in general and how to create Model Prefab Variants from FBX files, see [Working with Model Prefab Variants](nested-prefabs.md). If you are upgrading from a previous version of Unity, you can also [convert any existing](nested-prefab.md#conversion) Linked Prefabs you may have to Variant Model Prefabs.

