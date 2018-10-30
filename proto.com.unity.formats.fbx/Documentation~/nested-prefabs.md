<a name="NestedPrefab"></a>
# Converting to Nested Prefabs
## Applies To: Unity 2018.3

As of 2018.3 Linked Prefabs will be replaced with the new [Nested Prefab workflow](https://unity3d.com/prefabs).

In order to convert existing Linked Prefabs or new FBX files to Nested Prefabs, follow these steps:

1. Right click the fbx file and select “Prefab Variant” to create a variant prefab that links to the FBX

![](images/FBXExporter_CreatePrefabVariant.png)

Note: If converting an existing Linked Prefab, then the components from the Linked Prefab will need to be manually copied to the variant.

Note: With Variant Prefabs, the name remapping functionality will be lost, so make sure to fix any name discrepancies before converting.