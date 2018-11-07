<a name="NestedPrefab"></a>
# Converting to Nested Prefabs
## Applies To: Unity 2018.3

As of 2018.3 Linked Prefabs will be replaced with the new [Nested Prefab workflow](https://unity3d.com/prefabs). 
More specifically, [Prefab Variants](https://docs.unity3d.com/2018.3/Documentation/Manual/PrefabVariants.html) can be used to achieve almost the same functionality as Linked Prefabs. 
Variant Prefabs cover the same updates as Linked Prefabs, as well as increased control over model updates.
For example, consider an FBX with a Point Light of size 10. If the size of the Point Light is set to 1 in the Prefab Variant,
 and the Point Light color is modified in the FBX, the prefab variant will update the color of the Point Light but not the size.
If it were a Linked Prefab, both the size and color would be overwritten to what is in the FBX.

Additionally, Prefab Variants provide a more natural, built-in way to create a prefab which receives updates from the FBX model.
They can be created and look like any other prefab without requiring an additional FbxPrefab component.
They also extend the flexibility of the prefab in allowing you to create variants of variants that will still update with the model prefab.

In order to convert existing Linked Prefabs or new FBX files to Nested Prefabs, follow these steps:

1. Right click the fbx file and select “Prefab Variant” to create a variant prefab that links to the FBX

![](images/FBXExporter_CreatePrefabVariant.png)

Note: If converting an existing Linked Prefab, then the components from the Linked Prefab will need to be manually copied to the variant.

Note: With Variant Prefabs, the name remapping functionality will be lost, so make sure to fix any name discrepancies before converting.