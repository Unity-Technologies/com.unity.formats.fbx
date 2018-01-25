RELEASE NOTES

**Version**: sprint42

NEW FEATURES
* FbxExporter: Added support for exporting SkinnedMeshes with legacy animation
* FbxExporter: Added support for exporting Lights with animated properties (Intensity, Spot Angle, Color)
* FbxExporter: Added support for exporting Cameras with animated properties (Field of View)

FIXES
* FbxExporter: fixed issue where animations would sometimes be exported before their components, causing errors
* FbxExporter: fixed bug where skinning weights were incorrect on export

KNOWN ISSUES
* When exporting with an animated transform for a Camera or a Light, the resulting rotation does not take the forward direction into account and is off by 90 degrees
* Key tangents are not exported and the default key tangent setting is different between Unity, FBXSDK and Maya. This cause the curve shape to change between Unity and Maya.
* Animated continuous rotations are not maintained
* Animated rotations with Euler Angles (Quaternion) or Quaternion interpolation are not converted to the correct Euler equivalent.

**Version**: sprint41

NEW FEATURES
* Added support for exporting lights
* FbxExporter: added ability to export animation on transforms
* FbxExporter: added ability to export animation on lights

Supports exporting animation of a light component's Intensity, SpotAngle, and Color

* FbxExporter: Added support for exporting Skinned Meshes

FIXES
* Export Settings: Added back support for MAYA_LOCATION
* Export Settings: fixed dropdown preference for Mayalt
* FbxPrefabAutoUpdater: fixed so RectTransforms update correctly in Unity 2017.3
* ConvertToPrefab: fixed null reference exception when converting missing components

**Version**: 1.2.0b1

NEW FEATURES

* Updated User Guide documentation
* Updated meta files to match original asset store release (1.0.0b1)

FIXES

* Revert to shipping DLLs not source
* Export Settings: Moved browse ("...") buttons for 3D Application/Export Path next to dropdown/path fields
* Export Settings: Made "Keep Open" and "Hide Native Menu" labels camel case
* Exporter: Fix so normals/binormals/tangents/vertex colors are exported if they exist

Weren't being exported for primitives or meshes that had less vertices than triangles.

* Added script to fix FbxPrefab component links when updating from forum release (1.1.0b1)

A "Run Component Updater" button will appear in the FBX export settings inspector. 
Clicking the button will repair all prefabs and scene files serialized as text.
To repair binary files, first convert asset serialization mode to "Force Text" in Editor Settings (Edit->Project Settings->Editor).

**Version**: sprint36

NEW FEATURES
*FbxExporter: Don't export visibility

FIXES
*FbxPrefabAutoUpdater: Now accepts updates to RectTransforms
*FbxExporter: Fix so camera exports with correct rotation
*MayaIntegration: Fix so the "SendToUnity" button in Maya is hidden on startup
*FbxPrefabAutoUpdater: Fix updating gameObjects with missing components
*UnityIntegration: Catch and print installation errors from 3D applications
*FbxExporter: Fix incorrect scaling when importing into Maya
*FbxExportSettings: Fix vendor location environmnet variable pointing to empty folder
*MaxIntegration: Reset export path on new scene

**Version**: sprint35

NEW FEATURES

* Fbx Exporter: Added camera export support

Export game camera as film camera, with filmback settings set to 35 mm TV Projection (0.816 x 0.612).
The camera aperture with always have a height of 0.612 inches, while the width will depend on the aspect of the Unity camera,
as camera width = aspectRatio * height.
The projection type (perspective/orthogonal), aspect ratio, focal length, field of view, near plane, and far plane are also
exported. Background color and clear flags are exported as custom properties.
The last camera exported is set to the default camera in the FBX file.
NOTE: the field of view will show up as a different value in Maya. This is because Unity display's the vertical FOV,
      Maya displays the horizontal FOV.
NOTE: for GameObjects that have both a mesh and a camera component, only the mesh will be exported.

* Export Settings: Grouped settings visually into 2 categories

Categories are: Export Options and Integration

* Maya Unity Integration: Added export setting option to hide native "File->Send To Unity" menu

* Unity 3D application Integration: Different installation popup message if "Keep open" checked

To avoid misleading successful installation message popping up before installation completes, instead of 
"Enjoy the new Unity menu in {3DApp}", show "Installing Unity menu in {3DApp}, application will open once installation is complete",
if user selected to launch the 3D application after installation.

* Maya Unity Integration: Added Unity plugin version to File->Unity menu item's tooltip

* Fbx Exporter: Export GameObject visibility

Set FbxNode visibility based on whether a GameObject is enabled.
NOTE: a disabled FBX node will be imported into Unity as an enabled GameObject with a disabled Mesh Renderer.
NOTE: in 3ds Max disabled objects will still be visible

FIXES
* Export Settings: Changed "Launch 3D Application" to "Keep open"
* Fbx Exporter: cleaned up code: removed TODO's, unused, and commented out code
* Export Settings: Fix settings giving error when updating to sprint34 package
* Fbx Exporter: fix error when exporting meshes with missing normals, tangents, binormals, or vertex colors
* Export Settings: Fix empty dropdown selection when uninstalling 3D applications
* Convert to Linked Prefab: fix prefab instance name differing from prefab file name when filename is incremented

**Version**: sprint34

NEW FEATURES

* Ship all C# scripts as source code

* Added Maya LT Integration

Replaced Maya python implementation with MEL script, which is used by both Maya and Maya LT.

NOTE: it is no longer possible to unload the plugin using Maya's plugin manager.

* Export Settings: Added option to launch 3D application after installing integration

* Export Settings: Added option to export FBX as ASCII or Binary

* Export Settings: Added integration unzip location field

Use new field to select where to unzip the integration zip file, instead of being asked each time
"Install Unity Integration" button is clicked.

FIXES
* Export Settings: Moved "Browse" button out of the dropdown
* Unity 3ds Max Integration: Added tooltips to the import/export menu items
* Export Settings: Align checkboxes, text fields, and dropdown
* Fbx Prefab: Add tooltip to "Source Fbx Asset" field
* Export Settings: Search for 3D applicaitons in multiple vendor locations (e.g. C:/ and D:/ drive)

**Version**: 1.1.0b1

NEW FEATURES

* Export Settings: Set application with latest version as default selection in 3D application dropdown

In case of a tie, use the following preference order: Maya > Maya LT > 3ds Max > Blender.

* Updated user documentation

FIXES

* Exporter: Fix FBX exported from Unity causing crash when imported in 3ds Max.
* Export Settings: Fix hang when adding multiple installations of the same version of a 3D application to the dropdown

**Version**: sprint32

NEW FEATURES

* 3DsMax Unity Integration: Added popup suggesting user set system units to centimeters

Will only show up if system units are not already centimeters.
Click "yes" to change system units to centimeters, "no" to leave units as is.
If "no" is clicked, popup will not show up again for this session or .max file.

FIXES

* 3DsMax Unity Integration: In 3ds Max 2017 move the Unity menu before the Help menu in the main menu bar
* 3DsMax Unity Integration: Fix so file units are always exported as cm. Adjust scaling according to system units
                            e.g. a 3 meter cube in Max will export as a 300 cm cube in Unity


**Version**: sprint31

NEW FEATURES

* Added 3ds Max 2017 support

Import/Export menu items added into a Unity menu item on the main menu bar.

FIXES
*FbxPrefab: Avoid trying to update Rect Transforms with the fbx's Transform component
*Export Settings: Rename "DCC Application" to "3D Application"
*Unity 3ds Max integration: Fix model rotated by 90 degrees along x when importing into Unity


**Version**: sprint30

NEW FEATURES

* Added 3DsMax Integration

Install the same way as the Maya integration from the export settings.
3DsMax 2017 or earlier not supported.
Plugin menu items can be found in the File menu:
File->Import->Import from Unity
File->Export->Export to Unity

FIXES

* Maya Unity Integration: lock export set so it doesn't accidentally get deleted
* Convert to Prefab: Fix so convert to prefab doesn't lose Object references in scripts
* Export Settings: Fix so MayaLT cannot be selected using "Browse" on Mac

**Version**: 1.0.0b1

NEW FEATURES

* Ship all C# scripts as DLLs

* Maya Unity Integration: Remove Preview option from menu

* Enforce Exporter only works with Unity 2017.1+

FIXES
* Export Settings: prevent user from selecting Maya LT with "Browse" option in dropdown
* Fbx Export: fix game won't compile with package installed (move FbxSdk to editor folder)
* Convert to Prefab: fix particle system component causing convert to fail 
* Fbx Prefab: Properly handle updating Linked Prefab Instances that get nested inside other Prefabs.

**Version**: sprint26

NEW FEATURES

* Fbx Export: add support for installing a zipped version of the Maya Unity Integration

FIXES

* Fbx Prefab: Properly handle updating Linked Prefab Instances that get nested inside other Prefabs.

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
* Convert to Prefab: fix running "Convert to Prefab" multiple times on same object adds multiple FbxPrefab components
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
