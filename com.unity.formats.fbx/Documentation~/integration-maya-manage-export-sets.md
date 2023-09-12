# Manage Export Sets in Maya

Create Export Sets to export Maya objects that are not part of an existing export set. Edit export options of existing Export Sets.

>[!NOTE]
>This page provides instructions to perform tasks in Autodesk Maya using the Unity Integration tool, which [requires to be installed first](integration-setup.md).

## Create a new Export Set

To export objects that are not part of an existing export set, you need to prepare a new export set. To do this, follow these steps:

1. Select the desired objects for export.

2. Select **File > Unity** and then select one of the following menu items:

   ![](images/FBXExporter_MayaUnityMenuItems.png)

   * Select **Create Export Set** OR **Export** to create an export set with settings for exporting animation and model FBX files,
     <br />OR
   * Select **Export Model Only** OR **Export Animation Only** to setup an export set for exporting a model or animation file.

3. Use the window that opens to set the options for the export set:

   ![](images/FBXExporter_MayaCreateExportSetDialog.png)

   | Property | Function |
   |:---------|:---------|
   | **Model File Path** | The file path to export to when you select **File > Unity > Export** or **File > Unity > Export Model Only**. |
   | **Model File Name** | The name of the file to export to when you select **File > Unity > Export** or **File > Unity > Export Model Only**. |
   | **Anim File Path** | The file path to export to when you select **File > Unity > Export Animation Only**. |
   | **Anim File Name** | The name of the file to export to when you select **File > Unity > Export Animation Only**. |
   | **Strip Namespaces on Export** | Enable this option to automatically strip the most common namespace in the set on export.<br /><br />For example, if you enable this option and most objects are in namespace `model:`, then `model:` is stripped on export. |

   | Action button | Function |
   |:--------------|:---------|
   | **Create Set And Export** | Creates the set and immediately exports the selection. |
   | **Create Set** | Creates the set without exporting. |
   | **Cancel** | Cancels the set creation and closes the window. |

## Edit Export Set Attributes

After you created an export set (through **File > Unity > Import** or **File > Unity > Create Export Set**, or one of the export options), you can edit the export options
on the set.

To do so, select the set, and in the **Attribute Editor**, in the **Extra Attributes** section, modify the Unity attributes.

> **NOTE:** If Arnold is installed, the attributes might appear in the **Arnold > Extra Attributes** section.

![](images/FBXExporter_MayaExportSetAttributes.png)

| Property | Function |
| :------- | :------- |
| **Unity Fbx Model File Path** | The file path to export to when you select **File > Unity > Export** or **File > Unity > Export Model Only**. |
| **Unity Fbx Model File Name** | The name of the file to export to when you select **File > Unity > Export** or **File > Unity > Export Model Only**. |
| **Unity Fbx Anim File Path**  | The file path to export to when you select **File > Unity > Export Animation Only**. |
| **Unity Fbx Anim File Name**  | The name of the file to export to when you select **File > Unity > Export Animation Only**. |
| **Unity Fbx Strip Namespace** | Enable this option to automatically strip the most common namespace in the set on export.<br /><br />For example, if you enable this option and most objects are in namespace `model:`, then the export strips `model:`.<br /><br />If you want to strip another namespace instead of the most common one, you can type it in the **Strip Specific Namespace** field. |
| **Strip Specific Namespace** | (Optional) Use this property to specify the namespace to strip on export when you enable the **Unity Fbx Strip Namespace** option, instead of the most common namespace.<br /><br />If you leave this field empty, the **Unity Fbx Strip Namespace** option uses by default the most common namespace in the set.|
