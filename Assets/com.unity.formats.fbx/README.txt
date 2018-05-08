FBX Exporter Package
====================

Copyright (c) 2017 Unity Technologies. All rights reserved.

See LICENSE.txt file for full license information.

VERSION: sprint45

Requirements
------------

Installing Unity Integration
----------------------------

The easiest way to install the Unity integration is from the Fbx Export Settings in Unity.

        MenuBar -> Edit -> Project Settings -> Fbx Export -> Install Unity Integration

It will use the 3D application specified in the "3D Application" dropdown located above the
button. The dropdown will show all supported Maya, Maya LT and 3ds Max versions located in the default installation location.
To handle non-default installation locations, either select the browse option in the dropdown
and browse to the desired executable location, or set the MAYA_LOCATION environment variable.


===================
FBX SDK C# Bindings
===================

Autodesk FBX SDK. Copyright (c) 2016 Autodesk, Inc. All rights reserved.<br/>
Use of the FBX SDK requires agreeing to and complying with the FBX SDK License and Service Agreement terms 
accessed at https://damassets.autodesk.net/content/dam/autodesk/www/Company/docs/pdf/legal-notices-&-trademarks/Autodesk_FBX_SDK_2015_License_and_Services_Agreement.pdf"

**Version**: 1.3.0a1

This package contains only a subset of the FbxSdk, and is designed to work in the Unity Editor only.

How to Access Bindings in Code
-------------------------------
All the bindings are located under the FbxSdk namespace,
and are accessed almost the same way as in C++.
e.g. FbxManager::Create() in C++ becomes FbxSdk.FbxManager.Create() in C#


How to Access Global Variables and Functions
--------------------------------------------
All global variables and functions are in Globals.cs, in the Globals class under the FbxSdk namespace.
e.g. if we want to access the IOSROOT variable, we would do FbxSdk.Globals.IOSROOT

   
How to Access Documentation for Bindings
----------------------------------------
1. Unzip docs.zip outside of the Assets folder
2. Open docs/html/index.html
