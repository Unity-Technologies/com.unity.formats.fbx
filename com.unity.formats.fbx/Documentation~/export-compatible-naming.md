# Compatible Naming rules

During an export or a conversion to a Prefab Variant, if you enabled **Compatible Naming** in the [Export Options](ref-export-options.md), the FBX Exporter manages characters in the names of the exported or converted elements to ensure naming compatibility with other software.

## Character management in names

The **Compatible Naming** option manages characters as follows:

* Replaces invalid characters with underscores ("\_"). Invalid characters are all non-alphanumeric characters, except for the colon (":").
* Adds an underscore ("\_") to names that begin with a number.
* Replaces diacritics. For example, replaces "é" with "e".

## Materials with a space in their name

If a Material has a space in its name, the FBX Exporter replaces this space by an underscore ("_") on export. As a result, if you re-import the same GameObject in Unity, the FBX Exporter creates a new Material based on the modified name.

For example, if the Material name is "Default Material", the FBX Exporter exports this Material as "Default_Material". After you re-import the same GameObject, you get twice the same material, respectively named "Default Material" and "Default_Material".

If you want to keep an exact match of Materials through your export/import iterations, you must rename the involved Materials to remove any space characters before exporting.

## Additional resources

* [FBX Export Options](ref-export-options.md)
* [FBX Export Project Settings](ref-project-settings.md)
