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

if [ ! -f "${UNITY_EDITOR_PATH}" ]; then
    echo "Unity is not installed in default location"
    exit -1
fi    

if [ -d "${PROJECT_PATH}/Assets/FbxExporters" ]; then
    echo "Uninstalling previous package"
    rm -rf "${PROJECT_PATH}/Assets/FbxExporters"
fi

# Install FbxExporters package
"${UNITY_EDITOR_PATH}" -projectPath "${PROJECT_PATH}" -importPackage ${PACKAGE_PATH} -quit

exit 0
