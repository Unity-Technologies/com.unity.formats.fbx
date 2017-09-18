RELEASE NOTES

**Version**: 0.0.14a

NEW FEATURES

* Fbx Export: Add support for Third Party software, through a delegate callback, to handling adding the FbxMesh to the FbxNode on export.

* Maya Unity Integration: Added script for installing Maya integration through the command line

* Maya Unity Integration: Added dropdown to select Maya version to use for installation

Tries to find all Maya versions installed in default install location. Also contains browse option to select Maya installed
in custom location.

* Maya Unity Integration: Added icons for Import, Preview, and Export

* Moved Integrations and FbxSdk folders under a single FbxExporters folder in the Unity package

FIXES

* Maya Unity Integration: always show plugin as "what's new" regardless of Maya version
* Convert to Prefab: Handle Convert to Prefab on an FbxPrefab
* FbxPrefab: Fire OnUpdate event even if there is no obvious change to the Fbx
* Maya Unity Integration: restore selection after publish
* FbxPrefab: fix so that renaming parent doesn't affect child transform
* Convert to Prefab: fix so zeros are kept when incrementing (e.g. Cube001.fbx becomes Cube002.fbx instead of Cube2.fbx)
* Convert to Prefab: Weld vertices by default, remove option from export settings
* Convert to Prefab: By default delete original GameObject after converting
* Export Settings: Reword center objects tooltip
* Maya Unity Integration: Handle projects with spaces in the path
* Maya Unity Integration: Run turntable review with Unity project already open
* Maya Unity Integration: Rename "Review" to "Preview", and "Publish" to "Export"
* Convert to Prefab: Don't copy SkinnedMeshRenderer component to FbxPrefab (as we currently do not support skinned mesh export)
* Convert to Prefab: Rename "Convert to Prefab" to "Convert To Linked Prefab Instance"
* Fbx Export: fix memory leak with SkinnedMeshRenderer creating a temporary mesh and not destroying it.

**Version**: 0.0.13a

FIXES

* Updated license to Unity Companion License 1.0 
* Hide "auto-update" feature of FbxPrefab component from the Unity Inspector
* Remove "Embedded Textures" option from the Fbx Export Settings
* Remove "Auto Review" button from the Fbx Export Settings
* Maya Integration Plugin : the Unity menu is now submenu of the File menu
* Maya Integration Plugin : add Unity icon to Unity menu item
* Convert to Prefab : Add more unit tests

**Version**: 0.0.12a

NEW FEATURES

* Show FbxExporter package version in Fbx Export Settings

* Fbx Prefab auto-updater updates transforms and components

If components are added/removed in Maya, the changes will be reflected in Unity (e.g. if a mesh is removed from a node,
the MeshFilter and MeshRenderer components will be removed in Unity as well.
Updating the translation/rotation/scale of a transform in Maya will update the transform in the Unity prefab.

* Move Autoload Last Saved Prefab menu item to Fbx Export Settings

Now loading the turntable scene with the latest prefab can be done via a button
in the Fbx Export Settings

* Maya Integration: Unity->Import starting directory is Unity Project

Instead of opening in the default Maya project. A side effect of this is that a workspace.mel file
gets added to the Unity project root.

* Maya Integration: Install Maya Integration menu item moved into Fbx Export Settings

Now installing the maya integration can be done via a button
in the Fbx Export Settings

* Fbx Exporter menu removed from main menu bar

* Maya Integration: Hide Configure button, guess Unity project on Unity->Import

On Unity->Import try to guess which Unity project we are loading the fbx from (if any), set it to be the project we
use in Maya if found, do nothing otherwise.

* Fbx Exporter: Allow GameObjects and/or components to specify the mesh to export

Added callbacks to allow the GameObject or components to specify the mesh that should be exported, fallback
to using the MeshFilter or SkinnedMeshRenderer meshes.

* Fbx Prefab: Added OnUpdate event that returns which GameObjects were updated

The returned objects include all objects in the temporary instance that were created, changed parent, or had a component
that was created, destroyed, or updated. 
The event happens before changes are applied to the prefab, so any further modification of the returned GameObjects
will be applied as well.

FIXES

* Export Settings: make sure export path always points to an existing folder in assets
* Maya Integration: fix using Unity->Import to import the same model twice clears export set the second time
* Maya Integration: fix fbxmaya and GamePipeline plugins not being autoloaded on Mac
* Maya Integration: remove version number from maya integration folder
* Maya Integration: module file installed into Maya version independent location

**Version**: 0.0.11a

NEW FEATURES

* Maya Integration: Publish automatically writes to the same file you imported from.

On Unity->Import, store the path and filename of the imported FBX as attributes on the export set.
On Unity->Publish, if path and filename attributes are set, publish directly to this location without prompting user.

* Maya Integration: Unity->Import creates export set containing imported objects

If an export set already exists, replace its contents with newly imported objects

* Maya Integration: Unity->Publish exports what is in the export set

Export contents of export set, or if there is no export set, then the current selection will be exported.

* Maya Integration: Fbx export options are set from a file in the Unity project

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