# Changes in Fbx Exporter

RELEASE NOTES
## [2.0.0] - 2018-06-18
NEW FEATURES
* Added support for physical cameras
* FBX Exporter is now distributed via the Package Manager
* Now compatible with Unity 2018.2
* The roundtrip of assets can now be started from Maya from assets that have not been exported from Unity
* DCC integration plug-in sources have been moved away from the package
* Now using FBX SDK version 2018.1
* Streamlined the API public interface
* Added support for exporting constraints
* ConvertToPrefab: Add ability to convert an fbx or prefab asset from the Project view
  * Right click on an fbx in the project view then select Convert to Linked Prefab to create a linked prefab asset for the fbx. It will not create an instance in the scene.
  * Right click on a prefab in the project view and select Convert to Linked Prefab to export the prefab to an fbx file and link the existing prefab to the newly created fbx.

FIXES
* Fixed skinned mesh bone update
* Last frame was sometimes not exported
* FBX export dialog hard to read in Unity Pro's dark theme

KNOWN ISSUES
* ConvertToPrefab: UI doesn't provide feedback about whether it will be converting an existing file or creating new files.
  * When converting an existing FBX file, the fbx filename and fbx export options are ignored (but not greyed out).
  * When converting an existing prefab, the prefab filename is ignored (but not greyed out)

## [1.3.0f1] - 2018-04-17
NEW FEATURES
* Unity3dsMaxIntegration: Allow multi file import
* Unity3dsMaxIntegration: Allow multi file export by scene selection
* FbxExporter: Export animation clips from Timeline
* FbxExportSettings: Added new UI to set export settings
* FbxExportSettings: Added option to transfer transform animation on export
* FbxExporterSettings: Added option to export model only
* FbxExporterSettings: Added option to export animation only
* FbxExporterSettings: Added option not to export animation on skinned meshes
* FbxExportSettings: Added option to export meshes without renderers
* FbxExportSettings: Added LOD export option
* UnityMayaIntegration: Allow multi file import
* UnityMayaIntegration: Allow multi file export by scene selection
* FbxPrefabAutoUpdater: new UI to help manage name changes
* FbxExporter: Added support for exporting Blendshapes
* FbxExporter: Added support for exporting SkinnedMeshes with legacy and generic animation
* FbxExporter: Added support for exporting Lights with animatable properties (Intensity, Spot Angle, Color)
* FbxExporter: Added support for exporting Cameras with animatable properties (Field of View)
* FbxExporter: added ability to export animation on transforms
* Added Maya LT one button import/export
* Added Camera export support 
* Added 3ds Max one button import/export
* Ability to export fbx files from Unity
* Convert to linked prefab to create a prefab that auto-updates with the linked fbx
* Maya one button import/export

FIXES
* ConvertToPrefab: fix Mesh Collider not pointing to exported mesh after converting
* FbxExporter: fix so "Compatible Naming" doesn't modify scene on export
* FbxExporter: link materials to objects connected to mesh instances
* FbxExporter: export meshes in model prefab instances as mesh instances in fbx
* ConvertToPrefab: Don't re-export fbx model instances
* FbxExportSettings: fix console error on Mac when clicking "Install Unity Integration"
* FbxExporter: fix so animating spot angle in Unity animates cone angle in Maya (not penumbra)
* FbxExporter: export correct rotation order (xyz) for euler rotation animations (previously would export as zxy)
* Fix Universal Windows Platform build errors caused by UnityFbxSdk.dll being set as compatible with any platform instead of editor only.
* Fix so Object references aren't lost when using Convert to Linked Prefab Instance
* Fix Maya Integration dropdown not appearing in the Export Settings