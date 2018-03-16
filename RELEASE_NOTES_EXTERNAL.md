RELEASE NOTES

**Version**: 1.3.0a1

NEW FEATURES
* FbxExporter: Added support for exporting Blendshapes
* FbxExporter: Added support for exporting SkinnedMeshes with legacy and generic animation
* FbxExporter: Added support for exporting Lights with animatable properties (Intensity, Spot Angle, Color)
* FbxExporter: Added support for exporting Cameras with animatable properties (Field of View)
* FbxExporter: added ability to export animation on transforms

FIXES
* fix Universal Windows Platform build errors

Error caused by UnityFbxSdk.dll being set as compatible with any platform instead of editor only.

**Version**: 1.2.0b1

NEW FEATURES
* Added Maya LT one button import/export
* Added Camera export support 

**Version**: 1.1.0b1

NEW FEATURES
* Added 3ds Max one button import/export

FIXES
* Fix so Object references aren't lost when using Convert to Linked Prefab Instance
* Fix Maya Integration dropdown not appearing in the Export Settings

**Version**: 1.0.0b1

NEW FEATURES
* Ability to export fbx files from Unity
* Convert to linked prefab to create a prefab that auto-updates with the linked fbx
* Maya one button import/export