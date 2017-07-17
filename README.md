# FbxExporters

Copyright (c) 2017 Unity Technologies. All rights reserved.

Licensed under the ##LICENSENAME##.
See LICENSE.md file in the project root for full license information.

Requirements
------------

* [FBX SDK C# Bindings v0.0.3a or higher](https://github.com/Unity-Technologies/FbxSharp)

Packaging
---------

**On OSX and Linux**

```
PROJECT_PATH=`pwd`
if test `uname -s` = 'Darwin' ; then
  UNITY_EDITOR_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity
else
  UNITY_EDITOR_PATH=/opt/Unity/Editor/Unity/Unity
fi
PACKAGE_NAME=FbxExporters
FBXSDK_PACKAGE_PATH=`ls -t ../FbxSharpBuild/FbxSdk_*.unitypackage | head -1`
PACKAGE_VERSION=`echo "${FBXSDK_PACKAGE_PATH}" | sed -e 's/.*\([0-9]\{1,\}\.[0-9]*\.[0-9]*[a-z]*\).*/\1/'`

"${UNITY_EDITOR_PATH}" -projectPath "${PROJECT_PATH}" -importPackage "${FBXSDK_PACKAGE_PATH}" -quit
"${UNITY_EDITOR_PATH}" -batchmode -projectPath "${PROJECT_PATH}" -exportPackage Assets/FbxExporters Assets/FbxSdk  "${PROJECT_PATH}/${PACKAGE_NAME}_${PACKAGE_VERSION}.unitypackage" -quit
```

**On Windows**

```
set PROJECT_PATH=/path/to/FbxExporters
set UNITY3D_PATH="C:/Program Files/Unity/Editor/Unity.exe"
set PACKAGE_NAME=FbxExporters
set PACKAGE_VERSION=0.0.3a
set FBXSDK_PACKAGE_PATH=/path/to/FbxSdk.unitypackage

%UNITY3D_PATH% -projectPath "%PROJECT_PATH%" -importPackage %FBXSDK_PACKAGE_PATH% -quit
%UNITY3D_PATH% -batchmode -projectPath "%PROJECT_PATH%" -exportPackage Assets/FbxExporters Assets/FbxSdk %PROJECT_PATH%/%PACKAGE_NAME%_%PACKAGE_VERSION%.unitypackage -quit
```
