# FbxExporters

Copyright (c) 2017 Unity Technologies. All rights reserved.

Licensed under the ##LICENSENAME##.
See LICENSE.md file for full license information.

**Version**: 0.0.6a-sprint17

Requirements
------------

* [FBX SDK C# Bindings](https://github.com/Unity-Technologies/FbxSharp)

Command-line Installing Maya2017 Integration
--------------------------------------------

You can install the package and integrations from the command-line using the following script:

MacOS:

# Configure where Unity is installed
# UNITY3D_VERSION=" 2017.1.0f3"
UNITY3D_PATH="/Applications/Unity${UNITY3D_VERSION}/Unity.app/Contents/MacOS/Unity"

# Configure where unitypackage is located
PACKAGE_PATH=`ls -t ~/Development/FbxExporters/FbxExporters_*.unitypackage | head -1`

# Configure which Unity project to install package
PROJECT_PATH=~/Development/FbxExporters

if [ ! -f "${UNITY3D_PATH}" ]; then
    echo "Unity is not installed"
else
    # Install FbxExporters package
    "${UNITY3D_PATH}" -projectPath "${PROJECT_PATH}" -importPackage ${PACKAGE_PATH} -quit

    # Install Maya2017 Integration
    "${UNITY3D_PATH}" -batchMode -projectPath "${PROJECT_PATH}" -executeMethod FbxExporters.Integrations.InstallMaya2017 -quit

    # Configuring Maya2017 to auto-load integration
    MAYA_PATH=/Applications/Autodesk/maya2017/Maya.app/Contents/bin/maya

    if [ ! -f "${MAYA_PATH}" ]; then
        echo "Maya2017 not installed"
    else
        # To configure without user interface change the last argument to 1 instead of 0
        "${MAYA_PATH}" -command "configureUnityOneClick \"${PROJECT_PATH}\" \"${UNITY3D_PATH}\" 0; scriptJob -idleEvent quit;"
    fi
fi

