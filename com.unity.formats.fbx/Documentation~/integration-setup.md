# Set up Unity Integration for your 3D modeling application

The Unity Integration tool is a plug-in that installs on top of **Autodesk® Maya®**, **Autodesk® Maya LT™**, or **Autodesk® 3ds Max®**.

## Requirements

* The Unity Integration tool is **only supported on Windows and MacOS**.
* The 3D modeling application to integrate with must be installed on the same machine as the Unity Editor.

## Automatic setup from Unity

To install the Unity Integration tool and make it available in your 3D modeling application:

1. **In Unity**, open the [Fbx Export Settings](ref-project-settings.md): from the Unity Editor main menu, select **Edit > Project Settings > Fbx Export**.

   ![FBX Export Settings window](images/FBXExporter_FBXExportSettingsWindow.png)

2. Use the __3D Application__ property to choose the 3D modeling software and version where you want to install the Unity Integration.

3. To select a version of Autodesk® Maya®, Autodesk® Maya LT™, or Autodesk® 3ds Max® installed outside the default location, click the **[...]** (Browse) button.

   ![3D Application property with Browse button (red outline)](images/FBXExporter_3DApplicationOption.png)

4. Before you install Unity Integration, close all instances of the selected 3D modeling software that matches the specified version.

5. Select __Install Unity Integration__ to install the Unity Integration for the selected 3D modeling software.

   **Note:** If you already unpacked a previous integration in the selected folder, Unity prompts you to either use the existing integration or to overwrite it with the newer version.

   ![](images/FBXExporter_AlreadyExist.png)

6. Unity Integration comes packaged in several zip files (one zip file per supported application). When prompted, select a target folder where you want to extract the Unity Integration.

   **Note:** The target folder can be outside of your current Project. Autodesk® Maya® and Autodesk® Maya LT™ both use the same zip folder.

   The application starts, configures the plug-in, and automatically exits. Unity reports whether the installation was a success.

   ![](images/FBXExporter_IntegrationSuccessMsg.png)

If an error occurs during startup, Autodesk® Maya® or Autodesk® Maya LT™ may not close. If this happens, check the Autodesk® Maya® or Autodesk® Maya LT™ console to see if you can resolve the issue, and then manually close Autodesk® Maya® or Autodesk® Maya LT™.

If you enabled the __Keep Open__ option in the [Fbx Export Settings](ref-project-settings.md) window, then Autodesk® Maya® or Autodesk® Maya LT™ remains open after installation completes.

## Customize FBX import/export settings in the 3D modeling application

   To customize the FBX Importer or Exporter settings in Autodesk® Maya® or Autodesk® Maya LT™, use the `unityFbxImportSettings.mel` and `unityFbxExportSettings.mel` files. Both files are located in the `Integrations/Autodesk/maya/scripts` folder.

   For Autodesk® 3ds Max®, use the `unityFbxImportSettings.ms` and `unityFbxExportSettings.ms` files located in the `Integrations/Autodesk/max/scripts` folder.

## Manual setup (workaround for Maya/Maya LT only)

In some cases, you have to install your integration manually. For example, you might be using an unsupported version of Autodesk® Maya® or Autodesk® Maya LT™.

To manually install an Autodesk® Maya® or Autodesk® Maya LT™ Integration:

1. Locate the `UnityFbxForMaya.zip` file. You can find it in Unity's Project view, under the `Packages/FBX Exporter/Editor/Integrations` folder.

2. Extract the archive to a folder where you have write permission. This can be in or outside of your Unity Project.

3. Copy the contents of `Integrations/Autodesk/maya/UnityFbxForMaya.txt` from the unzipped folder to the following file:

   * On Windows: `C:\Users\\{username}\Documents\maya\modules\UnityFbxForMaya.mod`
   * On Mac: `$HOME/Library/Preferences/Autodesk/Maya/modules/UnityFbxForMaya.mod`
   <br />

4. In `UnityFbxForMaya.mod`, modify the following line (mel code):

   `UnityFbxForMaya {Version} {UnityIntegrationsPath}/Integrations/Autodesk/maya`

   **Note:** You need to replace:
   * `{Version}` by the version number of your FBX Exporter package (for example, `4.0.1`)
   * `{UnityIntegrationsPath}` by the location where you unzipped `UnityFbxForMaya.zip` in first step.
   <br />

5. Locate the following file (if it doesn't exist, create the file):

   * On Windows: `C:\Users\{username}\Documents\maya\scripts\userSetup.mel`
   * On Mac: `$HOME/Library/Preferences/Autodesk/Maya/scripts/userSetup.mel`
   <br />

6. Add this line (mel code):

   ``if(`exists unitySetupUI`){ unitySetupUI; }``

7. Open Autodesk® Maya® or Autodesk® Maya LT™, and then open the Script Editor:

  ![](images/FBXExporter_MayaAccessScriptEditor.png)

8. Run the following (mel code):

   `unityConfigure "{UnityProjectPath}" "{ExportSettingsPath}" "{ImportSettingsPath}" 0 0;`

   **Note:** You need to replace:
   * `{UnityProjectPath}` with the path to your Unity Project
   * `{ExportSettingsPath}` with the path to `Integrations/Autodesk/maya/scripts/unityFbxExportSettings.mel`
   * `{ImportSettingsPath}` with the path to `Integrations/Autodesk/maya/scripts/unityFbxImportSettings.mel`.
