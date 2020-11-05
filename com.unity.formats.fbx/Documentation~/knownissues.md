# Limitations

* The FBX Exporter package does not support exporting `.asset` files.

* The bind poses of animated skinned Meshes are lost on export. For example, if you export an animated skinned Mesh from Unity and import it into Autodesk® Maya® and Autodesk® Maya LT™, you will not be able to set the character into the bind pose using the **Rigging** > **Skin** > **Go to Bind Pose** command.

* The FBX Exporter package ignores name or path changes when converting a Model instance.

* The FBX Exporter package does not support exporting animation only for animated Lights and Cameras from Autodesk® Maya® and Maya LT™.

* Exporting an empty mesh (MeshFilter with null sharedMesh), fails to export with ArgumentNullException.