# Convert a GameObject to an FBX Prefab Variant

Convert a GameObject and its children to a Prefab Variant based on an FBX model.

## Results to expect

When you perform this action, Unity:
* Exports the selected GameObject and its children to a new FBX file and generates the corresponding Model Prefab,
* Creates a new Prefab Variant based on the generated Model Prefab, and
* Replaces the original GameObject and its children in the Hierarchy by an instance of the created Prefab Variant.

## Convert the GameObject

To perform the conversion:

1. In the Hierarchy, select a GameObject or a Prefab instance to convert.

2. Right-click on the selection and select **Convert To FBX Prefab Variant**.

3. Adjust the export properties in the [Convert Options](ref-convert-options.md) window and click **Convert**.

>[!NOTE]
>You can convert a Model Prefab instance to an FBX Prefab Variant via this method, but Unity overwrites by default the corresponding FBX asset file due to FBX re-export. If you need to skip the FBX re-export step, use Unity's default option to [create a Prefab Variant](prefab-variants-create-from-model-prefab.md) of the Model Prefab asset, and then re-instantiate it in the Hierarchy.

## Further management

After you convert the GameObject:

* The exported FBX file becomes the model source which you can edit via a 3D modeling application.

* The created Prefab Variant becomes the only asset you should manipulate and instantiate in your Unity project.

* Any Prefab or Model Prefab that had instances under the converted GameObject have no more relationship with the created FBX file and Prefab Variant. You might get rid of these remaining asset files unless you need them for another context.

## Additional resources

* [FBX files and Prefab Variants](prefab-variants-concepts.md)
* [FBX Prefab Variant workflow](prefab-variants-concepts.md)
