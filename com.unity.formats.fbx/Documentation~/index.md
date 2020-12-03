# FBX Exporter 

The FBX Exporter package provides round-trip workflows between Unity and 3D modeling software. Use this workflow to send geometry, Lights, Cameras, and animation from Unity to Autodesk® Maya®, Autodesk® Maya LT™, or Autodesk® 3ds Max®, and back again, with minimal effort.

The FBX Exporter package includes the following features:

* [FBX Exporter](exporting.md): Export geometry, animation, Lights, and Cameras as FBX files so you can transfer game data to any 3D modeling software. Record gameplay and export it to make cinematics. Start grey-boxing with [ProBuilder](https://docs.unity3d.com/Packages/com.unity.probuilder@latest/), then export your GameObjects to FBX until you can replace them with the final Assets.

* [FBX Prefab Variants](prefabs.md): The FBX Importer allows you to import an FBX file as a **Model Prefab** and create **Prefab Variants** from them. Since Prefab Variants can override properties and children without affecting the original Prefab, you can use them in Unity without breaking the link to the file, and bring in updates.
	
* [Unity Integration for 3D modeling software](integration.md): Effortlessly import and export Assets between Unity and Autodesk® Maya®, Autodesk® Maya LT™, or Autodesk® 3ds Max®. The 3D modeling software remembers where the files go, and what objects to export back to Unity.

## Requirements

The FBX Exporter package is compatible with the following versions of the Unity Editor:

* 2019.4 and later

The Unity Integration for Autodesk® Maya® and Autodesk® Maya LT™ feature supports the following versions:

* Autodesk® Maya® and Autodesk® Maya LT™ 2017
* Autodesk® Maya® and Autodesk® Maya LT™ 2018
* Autodesk® Maya® and Autodesk® Maya LT™ 2019
* Autodesk® Maya® and Autodesk® Maya LT™ 2020

The Unity Integration for Autodesk® 3ds Max® feature supports the following versions of Autodesk® 3ds Max®:

* Autodesk® 3ds Max® 2017
* Autodesk® 3ds Max® 2018
* Autodesk® 3ds Max® 2019
* Autodesk® 3ds Max® 2020

## Installing the FBX Exporter

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Manual/upm-ui-actions.html).

Verify that the FBX Exporter is correctly installed by opening it (from the top menu: **GameObject** > **Export To FBX**).