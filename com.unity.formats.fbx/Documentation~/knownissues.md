# Known Issues

* The FBX Exporter package does not support exporting `.asset` files.

* Bind pose of animated skinned mesh is lost on export. For example, if you export an animated skinned mesh from Unity and import it into Autodesk® Maya® and Autodesk® Maya LT™ you will not be able to set the character into the bind pose using the **Rigging** > **Skin** > **Go to Bind Pose** command.

* Name or path changes are ignored when converting a Model instance

* Exporting animation only for animated lights and cameras from Autodesk® Maya® and Maya LT™ is not currently supported