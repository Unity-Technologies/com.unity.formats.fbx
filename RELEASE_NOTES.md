RELEASE NOTES

**Version**: 0.0.5a 

NEW FEATURES

* Automatic file naming 

When exporting the default filename is name of top level selection. If multiple objects are selected then we use the last name exported or if it’s the first time then we use Untitled.fbx.

Added option to rename Object and Material names on export to be Maya compatible.

We ensure that generated filenames do not contain invalid characters.

* Reliable file system units 

Export in cm units, mesh set to real world (meter) scale. No import options need to be adjusted between Unity and Maya.

* Freeze transform on export

For selection containing a single object then zero out its transform in the FBX so it is at the world centre in Maya.
For selection containing multiple objects then export their global transforms.

* Support exporting multiple materials per mesh

* Support exporting mesh instances

Instanced meshes are exported once and then shared between FbxNodes.

* Support exporting all available UV sets

TODO: publish video demonstrating feature

* Unit Tests

Added test for default selection behaviour.

FIXES

* Export : Set default Albedo colour to white instead of grey if not found in source material so that it uses the same default as a new material.
* Convert to Model : copy component values for components that already between the source hierarchy and the imported copy.
* Convert to Model : ensure only called once from context menu with multiple selection.
* Convert to Model: ensure instanced prefab linked to prefab on disk
* Convert to Model: don’t overwrite existing files for example, if “Sphere.fbx” exists then the file will be called “Sphere1.fbx”
* Convert to Model: ensure order of copied siblings in matches original; ensure all GameObjects have unique names before export
