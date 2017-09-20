#! /bin/sh
########################################################################
# Copyright (c) 2017 Unity Technologies. All rights reserved.
# NOTICE: All information contained herein is, and remains
#         the property of Unity Technologies Aps. and its suppliers,
#         if any.  The intellectual and technical concepts contained
#         herein are proprietary to Unity Technologies Aps. and its
#         suppliers and may be covered by Canadian, U.S. and/or
#         Foreign Patents, patents in process, and are protected
#         by trade secret or copyright law. Dissemination of this
#         information or reproduction of this material is strictly
#         forbidden unless prior written permission is obtained from
#         Unity Technologies Aps.
#
########################################################################

# Configure where Unity is installed
if [ ! -f "${UNITY_EDITOR_PATH}" ]; then
    UNITY_EDITOR_PATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
fi
echo "Using UNITY_EDITOR_PATH=${UNITY_EDITOR_PATH}"

# Configure which Unity project to install package
if [ ! -d "${PROJECT_PATH}" ]; then
    PROJECT_PATH=~/Development/FbxExporters
fi
echo "Using PROJECT_PATH=${PROJECT_PATH}"

# Configure where unitypackage is located
if [ ! -f "${PACKAGE_PATH}" ]; then
    PACKAGE_PATH=`ls -t ${PROJECT_PATH}/FbxExporters_*.unitypackage | head -1`
fi
echo "Using PACKAGE_PATH=${PACKAGE_PATH}"

# Configure where Maya is installed
if [ ! -d "${MAYA_LOCATION}" ]; then
    MAYA_LOCATION=/Applications/Autodesk/maya2017/Maya.app
fi
echo "Using MAYA_LOCATION=${MAYA_LOCATION}"

if [ ! -f "${UNITY_EDITOR_PATH}" ]; then
    echo "Unity is not installed in default location"
    exit -1
fi    

# Install FbxExporters package
"${UNITY_EDITOR_PATH}" -projectPath "${PROJECT_PATH}" -importPackage ${PACKAGE_PATH} -quit

# Install Maya Integration
"${UNITY_EDITOR_PATH}" -batchMode -projectPath "${PROJECT_PATH}" -executeMethod FbxExporters.Integrations.InstallMaya -quit

# Configuring Maya2017 to auto-load integration
MAYA_PATH=${MAYA_LOCATION}/Contents/bin/maya

if [ ! -f "${MAYA_PATH}" ]; then
    echo "Maya not installed at ${MAYA_PATH}"
else
    # Configure Maya Integration
    TEMP_SAVE_PATH="_safe_to_delete"
    EXPORT_SETTINGS_PATH="Integrations/Autodesk/maya/scripts/unityFbxExportSettings.mel"
    MAYA_INSTRUCTION_PATH="_safe_to_delete/_temp.txt"
    HEADLESS=1

    # NOTE: we need start Maya in UI mode so that we can correctly configure the auto-load of the plugin.
    "${MAYA_PATH}" -command "configureUnityOneClick \"${PROJECT_PATH}\" \"${UNITY_EDITOR_PATH}\" \"${TEMP_SAVE_PATH}\" \"${EXPORT_SETTINGS_PATH}\" \"${MAYA_INSTRUCTION_PATH}\" ${HEADLESS}; scriptJob -idleEvent quit;"
fi

exit 0