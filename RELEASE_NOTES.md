RELEASE NOTES

**Version**: 0.0.7a 

NEW FEATURES

* Export with common center

Now if you export multiple root objects, they will export with the world centered around the union of their bounding boxes.
Added an option to preferences to toggle whether the objects are exported with a common center or not.

* Handle export of Quads

If submesh has triangle or quad topology in Unity, then it will have same topology in FBX.

* Enforce unique names in exported Fbx

Rename objects with duplicate names when exporting to ensure naming stays consistent, as
both Maya and Unity rename objects with duplicate names on import. 

* Save Application name and version to Fbx

Save name and version of FbxExporter to file when exporting.

* Added export performance test

Test exporting a large mesh and fail if export takes too long.

FIXES

* Export: Export rotation in XYZ order instead of ZXY so Maya always imports rotation correctly
* Export: Improved performance by caching Mesh data (i.e. triangles, tangents, vertices, etc.)
* Export: Set emissive color default to 0 so material does not appear white if no emissive color found.
* Export: Write first to temporary file to avoid clobbering destination file if export cancelled.
* Convert to Model: Fixed so "GameObject/Convert to Model" menu item works
* Convert to Model: Ensure imported model name matches incremented filename
* Convert to Model: Don't reference embedded materials of original model in new model.
* Convert to Model: If existing filename ends with a number, increment it instead of appending 1 (i.e. Sphere_1 becomes Sphere_2 instead of Sphere_1 1)

**Version**: 0.0.6a 

NEW FEATURES

* Model prefab path preference

Added a preference to the Fbx Export Preferences to control where Convert to Model saves models.

* FbxExporter Maya plugin

Added a Maya plugin with one click integration from within Unity to install the plugin into Maya 2017. 
Headless install option also available through command line.
The plugin creates a Unity menu item with 3 options in the drop down menu: Configure, Review, and Publish.
The menu options do not yet have any functionality.

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