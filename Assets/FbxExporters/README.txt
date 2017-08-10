# FbxExporters

Copyright (c) 2017 Unity Technologies. All rights reserved.

See LICENSE.txt file for full license information.

**Version**: 0.0.9a

Requirements
------------

* [FBX SDK C# Bindings](https://github.com/Unity-Technologies/FbxSharp)

Command-line Installing Maya2017 Integration
--------------------------------------------

You can install the package and integrations from the command-line using the following script:

MacOS:

# Configure where Unity is installed
if [ ! -f "${UNITY_EDITOR_PATH}" ]; then
    UNITY_EDITOR_PATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
fi

# Configure where unitypackage is located
if [ ! -f "${PACKAGE_PATH}" ]; then
    PACKAGE_PATH=`ls -t ~/Development/FbxExporters/FbxExporters_*.unitypackage | head -1`
fi

# Configure which Unity project to install package
if [ ! -f "${PROJECT_PATH}" ]; then
    PROJECT_PATH=~/Development/FbxExporters
fi

if [ ! -f "${UNITY_EDITOR_PATH}" ]; then
    echo "Unity is not installed"
else
    # Install FbxExporters package
    "${UNITY_EDITOR_PATH}" -projectPath "${PROJECT_PATH}" -importPackage ${PACKAGE_PATH} -quit

    # Install Maya2017 Integration
    "${UNITY_EDITOR_PATH}" -batchMode -projectPath "${PROJECT_PATH}" -executeMethod FbxExporters.Integrations.InstallMaya2017 -quit

    # Configuring Maya2017 to auto-load integration
    MAYA_PATH=/Applications/Autodesk/maya2017/Maya.app/Contents/bin/maya

    if [ ! -f "${MAYA_PATH}" ]; then
        echo "Maya2017 not installed"
    else
        # To configure without user interface change the last argument to 1 instead of 0
        "${MAYA_PATH}" -command "configureUnityOneClick \"${PROJECT_PATH}\" \"${UNITY_EDITOR_PATH}\" 0; scriptJob -idleEvent quit;"
    fi
fi

