# Troubleshooting

* When installing a new version of the FBX Exporter package after using version 1.3.0f1 or earlier, the link between Assets and FbxPrefab components may be lost. See [Updating from 1.3.0f1 or earlier](index#Repairs_1_3_0f_1) for repairing instructions.

* Animated skinned meshes may not export with the correct skinning if they are not in the bind pose on export (that is, not being previewed in the Animation or Timeline windows, and the original Rig's FBX must not contain animation)

* For skinned meshes all bones must be descendants of the root bone. For example, if the root bone is "hips" and the right leg for the same skinned mesh is not a descendant of hips, export will fail.

* Converting hierarchies with UI components will break UI
    * FIX: prepare hierarchy so that it has no UI elements before converting then add it back afterwards
    
* In some situations, when overwriting an FBX file on export. If the FBX file is used by an FBX Linked Prefab instance, it may lead to unexpected results such as additional objects added to the hierarchy.
    * FIX: recommended not to overwrite an FBX unless certain that it is not used by a Prefab Variant
    
* Converting a Tree primitive makes the Tree no longer editable.
    * FIX: make sure to only convert when finished editing the tree. Otherwise click undo after converting to return to previous state where tree was editable.
    
* Trail and line particles lose material after being converted
    * FIX: re-apply the lost materials after converting