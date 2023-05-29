## Upgrading from Asset Store version of the FBX Exporter

If your previous version of the FBX Exporter package was version 1.3.0f1 or earlier, follow these steps:

Before you install the FBX Exporter Package, follow these steps (recommended):

1. Back up your Project.

2. Restart Unity.

3. Delete the *FbxExporters* folder.

4. Install the FBX Exporter from the [Package Manager](https://docs.unity3d.com/Manual/upm-ui-install.html).

If you were using an older version of the FBX Exporter, some Assets in your Project may have missing scripts where the obsolete FbxPrefab component was used. To fix these issues, follow these steps:

1. If your Project Assets are serialized as Binary, select __Edit__ > __Project Settings__ > __Editor__ to view the Editor Settings.

2. Change the __Asset Serialization__ mode to __Force Text__. The __Force Text__ option converts all Project Assets to text.

3. Before continuing, back up your Project.

4. Select __Edit__ > __Project Settings__ > __FBX Export__ to view the [FBX Export Settings](ref-project-settings.md).

  ![Run Component Updated button](images/FBXExporter_RunComponentUpdater.png)

5. Click the __Run Component Updater__ button to repair all text serialized Prefab and Scene Assets in the Project containing the obsolete FbxPrefab component.
