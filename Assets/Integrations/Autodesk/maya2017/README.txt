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

Automatic Installation
===================

The easiest installation method is to launch Unity and use the
        FbxExporters -> Install Maya Integration
option.

It will choose the most recent version of Maya installed in the default
installation location. To choose a particular version of Maya or to handle
non-default installation locations, set the MAYA_LOCATION environment variable.

Manual Installation
===================

Instructions for installing if you don't use the unity package installer
and your installing in a non-default location.

1. copy unityoneclick.mod to user folder

    MacOS & Ubuntu: ~/MayaProjects/modules
    Windows:        C:\Program Files\Autodesk\Maya2017\modules

2. configure path within unityoneclick.mod to point to integration installation folder

    {UnityProject}/Assets/Integrations/Autodesk/maya2017


Running Unit Tests
==================

MacOS

export MAYAPY_PATH=/Applications/Autodesk/maya2017/Maya.app/Contents/bin/mayapy
export MAYA_INTEGRATION_PATH=${UNITY_PROJECT_PATH}/Assets/Integrations/Autodesk/maya2017
export PYTHONPATH=${MAYA_INTEGRATION_PATH}/scripts

# run all tests
${MAYAPY_PATH} ${MAYA_INTEGRATION_PATH}/scripts/run_all_tests.py

# run one test
${MAYAPY_PATH} ${MAYA_INTEGRATION_PATH}/scripts/unityOneClick/commands.py
