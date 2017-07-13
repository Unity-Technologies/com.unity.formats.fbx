########################################################################
# Unity Technology Aps.
# [2017] -  . All Rights Reserved.
# NOTICE: All information contained herein is, and remains
#         the property of Unity Technology Aps. and its suppliers,
#         if any.  The intellectual and technical concepts contained
#         herein are proprietary to Unity Technology Aps. and its
#         suppliers and may be covered by Canadian, U.S. and/or
#         Foreign Patents, patents in process, and are protected
#         by trade secret or copyright law. Dissemination of this
#         information or reproduction of this material is strictly
#         forbidden unless prior written permission is obtained from
#         Unity Technology Aps.
#
########################################################################

Installation
============

1. copy unityoneclick.mod to 

    MacOS & Ubuntu: ~/MayaProjects/modules
    Windows:        C:\Program Files\Autodesk\Maya2017\modules
    
2. configure path to integration 

    {UnityProject}/Assets/Integrations/Autodesk/maya2017


Running Unit Tests
==================

MacOS / Ubuntu

export MAYAPY_PATH=/Applications/Autodesk/maya2017/Maya.app/Contents/bin/mayapy
export UNITY_PROJECT_PATH=~/Development/FbxExporters
export MAYA_INTEGRATION_PATH=${UNITY_PROJECT_PATH}/Assets/Integrations/Autodesk/maya2017
export PYTHONPATH=${MAYA_INTEGRATION_PATH}/scripts

# run all tests
${MAYAPY_PATH} ${MAYA_INTEGRATION_PATH}/scripts/run_all_tests.py

# run one test
${MAYAPY_PATH} ${MAYA_INTEGRATION_PATH}/scripts/unityOneClick/commands.py