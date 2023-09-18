# Create a Prefab Variant based on an existing FBX model

Create a Prefab Variant based on an existing FBX Model Prefab without having to perform FBX re-export.

For example, you might need to do this if you already exported a Unity GameObject to FBX using the FBX Exporter, or if you imported an external FBX file in your project.

## Results to expect

When you perform this action, Unity:
* Keeps the FBX file and its corresponding Model Prefab unchanged, and
* Creates a new Prefab Variant based on the Model Prefab.

## Create the Prefab Variant

To create a Prefab Variant based on an FBX Model Prefab:

1. In the Project window, right-click on the Model Prefab to use as the base of the Prefab Variant.

2. Select **Create** > **Prefab Variant**.

## Further management

After you create the Prefab Variant:

* The FBX file remains unchanged and continues to be the model source which you can edit via a 3D modeling application.

* The created Prefab Variant becomes the only asset you should manipulate and instantiate in your Unity project.

## Additional resources

* [FBX files and Prefab Variants](prefab-variants-concepts.md)
* [FBX Prefab Variant workflow](prefab-variants-concepts.md)
* [Export to FBX](export.md)
* [Importing a model](https://docs.unity3d.com/Manual/ImportingModelFiles.html)
