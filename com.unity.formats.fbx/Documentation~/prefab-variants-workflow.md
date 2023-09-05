# FBX Prefab Variant workflow

To enable a convenient FBX model workflow between Unity and your 3D modeling application, you should consider the creation of Prefab Variants based on your FBX files and only use instances of these Prefab Variants in your Scene instead of Model Prefab instances.

## Benefits and limitations

With FBX Prefab Variants, you can:

* Open and edit FBX files stored in your project using a separate 3D modeling application.
* Automatically inherit from the FBX file edits in the Prefab Variants used in your project.
* Override some properties of the Prefab Variants without affecting the FBX files they're based on.

For example, if a Model Prefab includes a Spot Light of size 10 and you override the size to 1 in the Prefab Variant, when the size and color change in the source FBX file, the color changes in the Prefab Variant but the size remains 1.

>[!WARNING]
>To avoid any data conflicts and unexpected behaviors between the Prefab Variant and the base FBX file, you must not make structural changes to the Prefab Variant. This includes deleting, reordering, and renaming inherited child GameObjects.

## Available features

The FBX Exporter package allows you to [convert a GameObject and its children](prefab-variants-convert-gameobject.md), or to convert [a Prefab asset](prefab-variants-convert-prefab-asset.md) to an FBX Prefab Variant. Such a conversion corresponds to an FBX export followed by a Prefab Variant creation, all in one single action.

>[!NOTE]
>If the asset you need to convert is an existing FBX file, you can directly [create a Prefab Variant](prefab-variants-create-from-model-prefab.md) based on it.

## Additional resources

* [FBX files and Prefab Variants](prefab-variants-concepts.md)
