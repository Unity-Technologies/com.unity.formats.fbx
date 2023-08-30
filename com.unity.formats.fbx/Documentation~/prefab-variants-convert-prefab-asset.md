# Convert a Prefab asset to an FBX Prefab Variant

Convert a Prefab asset file to a Prefab Variant based on an FBX model.

## Results to expect

When you perform this action, Unity:
* Exports the Prefab asset to an FBX file and generates the corresponding Model Prefab, and
* Creates a new Prefab Variant based on the generated Model Prefab.

## Convert the Prefab asset

To perform the conversion:

1. In the Project window, right-click on the Prefab asset to convert.

2. Select **Convert To FBX Prefab Variant**.

3. Adjust the export properties in the [Convert Options](ref-convert-options.md) window and click **Convert**.

>[!NOTE]
>You can convert a Model Prefab to an FBX Prefab Variant via this method, but Unity overwrites the existing FBX file by default due to FBX re-export. If you need to skip the FBX re-export step, use Unity's default option to [create a Prefab Variant](prefab-variants-create-from-model-prefab.md).

## Further management

After you convert the Prefab asset:

* The exported FBX file becomes the new model source which you can edit via a 3D modeling application.

* The created Prefab Variant becomes the only asset you should manipulate and instantiate in your Unity project.

* The original Prefab asset file you selected for conversion has no more relationship with the created FBX file and Prefab Variant. You might get rid of it unless you need it for another context.

## Additional resources

* [FBX files and Prefab Variants](prefab-variants-concepts.md)
* [FBX Prefab Variant workflow](prefab-variants-concepts.md)
