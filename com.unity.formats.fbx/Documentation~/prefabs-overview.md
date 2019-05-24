# Working with Prefabs

In Unity, [Nested Prefabs](https://docs.unity3d.com/Documentation/Manual/NestedPrefabs.html) allow you to create Prefabs inside other Prefabs, and [Prefab Variants](https://docs.unity3d.com/Documentation/Manual/PrefabVariants.html) allow you to save a variation on an existing Prefab. 

When you open an FBX file in Unity, the FBX Importer creates a **Model Prefab**, which is a read-only representation of the FBX contents. When the FBX file is modified inside the originating 3D modeling software, Unity updates the Model Prefab.

You can convert the Model Prefab to an **FBX Linked Prefab**. This is a Prefab Variant of the exported FBX's Model Prefab. Since Prefab Variants allow you to override its base Prefab's properties, you can use an FBX Linked Prefab to add components, Materials, or change most other property values without affecting the original FBX file.

> **NOTE:** While this method allows you to override properties, you cannot change the internal hierarchy of the Variant without breaking the link to the Model Prefab.

For information on Model Prefabs in general and how to create FBX Linked Prefabs from FBX files, see [Working with FBX Linked Prefabs](#linked). If you are upgrading from a previous version of Unity, you can also [convert any existing](#conversion) Linked Prefabs you may have to the new FBX Linked Prefabs.


## Terminology

* __Model Prefab__: When you open an FBX file in Unity, the FBX Importer creates a Model Prefab. A Model Prefab is a read-only representation of the FBX contents, which updates when the FBX file is modified.

* __Prefab Variant__: Prefab Variants are Prefabs which inherit all data from a base Prefab. Prefab Variants can create inheritance between ordinary Prefabs. For example, you can create a Variant of a button Prefab which has a different color but the same functionality. 

* __Linked Prefab__: A Regular Prefab containing an FbxPrefab component which connects to an FBX file. 
                 The Prefab updates automatically when the FBX file is modified. Components are maintained and if object names or hierarchies change in the FBX file,
                 it is possible to remap the objects by name on import.
                 This is specific to previous versions of the FBX exporter, and requires Unity 2018.2 or lower.

* __FBX Linked Prefab__: A Prefab Variant with a Model Prefab as its base. This feature replaces Linked Prefabs in Unity 2018.3 and above.
                     A special component is no longer required, as FBX Linked Prefabs use only the Unity Prefab features.