# Manage objects to export from 3ds Max

Manage the list of 3ds Max objects to export within Selection Sets.

>[!NOTE]
>This page provides instructions to perform tasks in Autodesk 3ds Max using the Unity Integration tool, which [requires to be installed first](integration-setup.md).

Unity export uses the selection sets created on import to determine which objects to export. If you add a new object to the Model, you must also add this new object to the Model’s *UnityExportSet* set.

![UnityExportSets in Autodesk® 3ds Max®](images/FBXExporter_MaxMultipleUnityExportSets.png)

* To edit a *UnityExportSet* set, select **Manage Selection Sets**.

  ![Manage Selection Sets in Autodesk® 3ds Max®](images/FBXExporter_ManageSelectionSets.png)

* To add an object to a set, select the set, select an object and select **Add Selected Objects**.

* To remove an object from a set, select the object in the set and select **Subtract Selected Objects**.

> **TIP:** You can also right-click the UnityExportSets and add or remove objects through the context menu.

![Named Selection Sets in Autodesk® 3ds Max®](images/FBXExporter_MaxNamedSelectionSets.png)

In Autodesk® 3ds Max®, use the **Add Selected Objects** button (red outline) to add objects to the *Wolf_UnityExportSet* set.
