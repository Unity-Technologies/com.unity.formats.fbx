# Quality Report - FBX Exporter Package
## Version tested: 2.0.1-preview

#### QA Owner: Alexis Morin
### Tested On: Windows, MacOS

### Test strategy

[Ran Test Plan](https://docs.google.com/document/d/1W_RYL6j--nASVlxwJ4QWZpiZLPseKoxP5WAooANdlNU/edit?usp=sharing)

#####  New Features QA:
- In Unity 2018.3 Prefab Variants replaces Linked Prefabs. The "Convert To Linked Prefab" menu items have been removed.
- Updated documentation

#####  Fixes QA:
- Fixed error when exporting SkinnedMesh with bones that are not descendants of the root bone
- Fixed animation only export not exporting animation in 2.0.0
- Fixed calculating center of root objects when exporting "Local Pivot"/"Local Centered"

### Package Status

- No issues from current test plan or bugfixes.
- Mesh offset bug when exporting the "Lu" character from the Adam character pack still present - card logged.
- "Fbx Export" settings still in their own tab whereas the other project settings have been integrated into their own dockable window.
- A few errors in 2018.1 but we've deprecated support for it so it's fine