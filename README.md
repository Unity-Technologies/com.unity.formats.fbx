# FbxExporters

Copyright (c) 2017 Unity Technologies. All rights reserved.

Licensed under the ##LICENSENAME##.
See LICENSE.md file in the project root for full license information.

Requirements
------------

* [FBX SDK C# Bindings v0.0.4a or higher](https://github.com/Unity-Technologies/FbxSharp)

Packaging
---------

**On OSX and Linux**

```
# clone the source
git clone https://github.com/Unity-Technologies/FbxExporters.git

export PROJECT_PATH=~/Development/FbxExporters
export UNITY3D_PATH=/Applications/Unity\ 2017.1.0f3/Unity.app/Contents/MacOS/Unity
export PACKAGE_NAME=FbxExporters
export PACKAGE_VERSION=0.0.5a
export FBXSDK_PACKAGE_PATH=~/Development/FbxSharpBuild/FbxSdk_0.0.4a.unitypackage

"${UNITY3D_PATH}" -projectPath "${PROJECT_PATH}" -importPackage ${FBXSDK_PACKAGE_PATH} -quit
"${UNITY3D_PATH}" -batchmode -projectPath "${PROJECT_PATH}" -exportPackage Assets/FbxExporters Assets/FbxSdk  ${PROJECT_PATH}/${PACKAGE_NAME}_${PACKAGE_VERSION}.unitypackage -quit
```

**On Windows**

```
# clone the source
git clone https://github.com/Unity-Technologies/FbxExporters.git

set PROJECT_PATH=%cd%/FbxExporters
set UNITY3D_PATH="C:/Program Files/Unity/Editor/Unity.exe"
set PACKAGE_NAME=FbxExporters
set PACKAGE_VERSION=0.0.5a
set FBXSDK_PACKAGE_PATH=/path/to/FbxSdk.unitypackage

%UNITY3D_PATH% -projectPath "%PROJECT_PATH%" -importPackage %FBXSDK_PACKAGE_PATH% -quit
%UNITY3D_PATH% -batchmode -projectPath "%PROJECT_PATH%" -exportPackage Assets/FbxExporters Assets/FbxSdk %PROJECT_PATH%/%PACKAGE_NAME%_%PACKAGE_VERSION%.unitypackage -quit
```
