# Working with Prefabs

In version 2018.3, Unity introduced a new way of working with Prefabs: [Nested Prefabs](https://docs.unity3d.com/2018.3/Documentation/Manual/NestedPrefabs.html) allow you to create Prefabs inside other Prefabs, and [Prefab Variants](https://docs.unity3d.com/2018.3/Documentation/Manual/PrefabVariants.html) allow you to save a variation on an existing Prefab. However, this changes the way you work with the FBX Exporter, because you can no longer use *Linked Prefabs* to maintain a connection between Unity and your 3D modeling application. 

Previously, Linked Prefabs were created by adding an FbxPrefab component to a Regular Prefab in order to connect it to an FBX file. 
From Unity 2018.3 onwards, FBX Linked Prefabs will instead be created as Prefab Variants of the exported FBX file.

When you open an FBX file in Unity, the FBX Importer creates a Model Prefab. Model Prefabs are read-only, but you can create a *Prefab Variant* of that Model Prefab. 
Prefab Variants are Prefabs which inherit all data from a base Prefab. Prefab Variants can create inheritance between ordinary Prefabs. 
For example, you can create a Variant of a button Prefab which has a different color but the same functionality. 

Since Prefab Variants allow you to override its base Prefab's properties, you can use a Prefab Variant of a Model Prefab to add components, Materials, or change most other property values without affecting the original FBX file.

> ***Note:*** While this method allows you to override properties, you cannot change the internal hierarchy of the Variant without breaking the link to the Model Prefab.

This type of connection is easy to maintain.

* For information on Model Prefabs in general and how to create FBX Linked Prefabs from FBX files, see [Working with FBX Linked Prefabs](nested-prefabs.md). If you are upgrading from a previous version of Unity, you can also [convert any existing](nested-prefabs.md#conversion) Linked Prefabs you may have to the new FBX Linked Prefabs.