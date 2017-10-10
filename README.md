# FbxExporters

Copyright (c) 2017 Unity Technologies. All rights reserved.

Licensed under the ##LICENSENAME##.
See LICENSE.md file in the project root for full license information.

Requirements
------------

* [FBX SDK C# Bindings](https://github.com/Unity-Technologies/FbxSharp)

Packaging
---------

**On OSX and Linux**

Get the source the first time.
```
# clone the source
git clone https://github.com/Unity-Technologies/FbxExporters.git
cd FbxExporters
```

Update the source and package:
```
git pull
FBXSDK_PACKAGE_PATH=`ls -t ../FbxSharpBuild/FbxSdk_*.unitypackage | head -1`
PACKAGE_VERSION=`echo "${FBXSDK_PACKAGE_PATH}" | sed -e 's/.*\([0-9]\{1,\}\.[0-9]*\.[0-9]*[a-z]*\).*/\1/'`

mkdir FbxExporterBuild
cd FbxExporterBuild
cmake ../FbxExporters -DPACKAGE_VERSION=${PACKAGE_VERSION} -DFBXSDK_PACKAGE_PATH=${FBXSDK_PACKAGE_PATH}
make
```

**On Windows**

```
# clone the source
git clone https://github.com/Unity-Technologies/FbxExporters.git

set PACKAGE_VERSION=0.0.14a
set FBXSDK_PACKAGE_PATH=/path/to/FbxSdk.unitypackage

mkdir FbxExportersBuild
cd FbxExportersBuild
cmake ../FbxExporters -G"Visual Studio 14 2015 Win64" -DPACKAGE_VERSION=%PACKAGE_VERSION% -DFBXSDK_PACKAGE_PATH=%FBXSDK_PACKAGE_PATH%
cmake --build . --target ALL_BUILD --config Release
```
**On macOS**

```
# clone the source
git clone https://github.com/Unity-Technologies/FbxExporters.git

export PACKAGE_VERSION=0.0.14a
export FBXSDK_PACKAGE_PATH=/path/to/FbxSdk.unitypackage

if [ ! -f "${UNITY_EDITOR_PATH}" ]; then
    UNITY_EDITOR_PATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
fi

if [ -d FbxExportersBuild ]; then
    rm -rf FbxExportersBuild
fi

mkdir FbxExportersBuild
cd FbxExportersBuild
cmake ../FbxExporters -DPACKAGE_VERSION=${PACKAGE_VERSION} \
  -DFBXSDK_PACKAGE_PATH=${FBXSDK_PACKAGE_PATH} \
  -DUNITY_EDITOR_PATH="${UNITY_EDITOR_PATH}"
make
```

