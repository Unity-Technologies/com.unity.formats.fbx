# FbxExporters

Copyright (c) 2017 Unity Technologies. All rights reserved.

Licensed under the ##LICENSENAME##.
See LICENSE.md file in the project root for full license information.

**Version**: 0.0.2a

Requirements
------------

* [FBX SDK C# Bindings v0.0.1 or higher](https://github.com/Unity-Technologies/FbxSharp)

Packaging
---------

**On OSX and Linux**

```
export PROJECT_PATH=~/Development/FbxExporters
export UNITY3D_PATH=/Applications/Unity\ 5.6.1f1/Unity.app/Contents/MacOS/Unity
export PACKAGE_NAME=FbxExporters
export PACKAGE_VERSION=0.0.2

"${UNITY3D_PATH}" -batchmode -projectPath "${PROJECT_PATH}" -exportPackage Assets/FbxExporters ${PROJECT_PATH}/${PACKAGE_NAME}_${PACKAGE_VERSION}.unitypackage -quit
```
