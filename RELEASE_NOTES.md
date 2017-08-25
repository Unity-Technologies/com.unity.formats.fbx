RELEASE NOTES

**Version**: 0.0.11a

NEW FEATURES

* Maya Integration: Store path and filename of imported FBX, publish to stored path

On Unity->Import, store the path and filename of the imported FBX as attributes on the export set.
On Unity->Publish, if path and filename attributes are set, publish directly to this location without prompting user.

* Maya Integration: Unity->Import creates export set containing imported objects

If an export set already exists, replace its contents with newly imported objects

* Maya Integration: Unity->Publish exports what is in the export set

Export contents of export set, or if there is no export set, then the current selection will be exported.

* Maya Integration: Exporting from the Unity plugin first loads export settings saved in Unity project

Export settings stored in Integrations/Autodesk/maya2017/scripts/unityFbxExportSettings.mel are loaded into Maya before
exporting either with Unity->Review or Unity->Publish.

FIXES

* Export Settings: fix export path doesn't refresh if selectable text box selected
* Convert to Prefab: fix model added to wrong scene if multiple scenes open

**Version**: 0.0.10a

NEW FEATURES

* Turntable Review shows minimal Unity window

The Game window is maximized so that it takes up most of the layout.

* Turntable Review frames camera onto model

* Turntable Review rotates model

Model rotates either when selected in the editor or in play mode.

* Maya Integration: Turntable Review publishes to temporary location

Running Unity -> Review command in Maya publishes the asset to a temporary location inside the Unity project.

* Maya Integration: Added support for multiple Maya versions

* Set Turntable scene from Project Settings

Scene to use for Turntable review can be selected in Project Settings.

* Select Turntable Base GameObject by attaching FbxTurnTableBase script

Attaching the FbxTurnTableBase script to a GameObject will parent the model being reviewed under this GameObject.
If none present, an empty GameObject will be used as the base.

FIXES

* FbxPrefab: Don't allow settings to be changed on prefab instance
* Maya Integration: Fix so review brings Unity window to front on Windows if already open

**Version**: 0.0.9a

* Auto updater for instanced prefabs

Convert To Prefab will now create both a .prefab file and a FBX file from the selected GameObject hierarchy. The newly instanced prefab will automatically update whenever the FBX file changes. The instanced prefab will now include updates whenever objects are added or removed from the source FBX file.

* Maya-to-Unity turntable review workflow

Unity One Click integration for Maya 2017 now includes a "Review in Unity" feature.

You can review how your Model looks in Unity by clicking "Unity->Review" menu Item. This will start Unity and your Model will be loaded into "FbxExporters_TurnTableReview" scene. If the scene cannot be found then an empty scene will be created for you. If the scene contains a "Turntable" object then your model will be parented under that object.

While Unity has the "FbxExporters_TurnTableReview" scene active it will automatically update each time you publish a Model. If you've changed the active scene and want to go back to the reviewing you can run the command "FbxExporters->Turntable Review->Auto Load Last Saved Prefab". If you have unmodified scene changes in a previously saved scene then you'll be prompted to save these changes before the active scene is switched for you. If the scene is an Untitled but modified scene then these changes will be left as-is and the active scene will be switched.

* FBXSDK C# unitypackage with docs ready for release

**Version**: 0.0.8a

NEW FEATURES

* Added Model Exporter unit tests

Added unit tests for frequently used public functions

FIXES

* Export: If nothing selected on export, pop up a dialog saying that nothing is selected
* Export: Remove menu items from Assets menu
* Convert to Model: fix issues with file number not incrementing properly (e.g. Sphere_1 would become Sphere_ 2)
* Convert to Model: Allow copying multiple components of the same type (e.g. GameObject with multiple Box Colliders)
* Convert to Model: Always add model to same scene as the original GameObject, if multiple scenes open
* Convert to Model: Rename to "Convert to Prefab"
* Export Settings: Saved settings weren't getting reloaded
* Export Settings: Model prefab paths settings fixes
  * Pressing cancel in the browse dialog no longer prints a warning
  * Browse dialog opens at previously saved location
  * Fix so button isn't clipped with default panel width  
* Maya Integration: fix so destructors are always called
* Export Unit tests: fix so ConvertToValidFilename tests pass on Mac
  
**Version**: 0.0.7a 

NEW FEATURES

* Export with common center

Now if you export multiple root objects, they will export so they are centered around the union of their bounding boxes.
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