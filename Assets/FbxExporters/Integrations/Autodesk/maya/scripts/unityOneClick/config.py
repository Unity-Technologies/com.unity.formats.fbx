#-
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
#         forbidden unless prior written permission Fis obtained from
#         Unity Technology Aps.
#
########################################################################
#+
"""
@package config
@author  Simon Inwood <simon.cf.inwood@gmail.com>
@defgroup UnityConfigurationUtils Configuration Utilities
@ingroup UnityUtils
"""

from logger import LoggerMixin
import version

import maya.cmds
import types
import os

class Settings(LoggerMixin):
    """
    Plugin Configuration Utility
    - access & change plugin settings
    - change project
    """
    _project = "UnityProject"

    def __init__(self):
        LoggerMixin.__init__(self)

    def __del__(self):
        LoggerMixin.__del__(self)

    @property
    def installPath(self):
        head, tail = os.path.split(maya.cmds.pluginInfo("unityOneClickPlugin", q=True, path=True))
        head, tail = os.path.split(head)
        return head

    @property
    def modulePath(self):
        return maya.cmds.moduleInfo( moduleName=version.moduleName(), d=True)

    @property
    def values(self):
        result = {}
        for a in maya.cmds.optionVar(list=True):
            if str(a).startswith('Unity'):
                result[a] = maya.cmds.optionVar(q=a)
        return result

    @values.setter
    def values(self, value):
        for k,v in value.iteritems():
            if str(k).startsWith('Unity'):
                if type(v) in [types.string, types.UnicodeType]:
                    maya.cmds.optionVar(sv=(k,v))
                elif type(v) in [types.IntType, types.LongType]:
                    maya.cmds.optionVar(iv=(k,v))

    @property
    def unityProjectPath(self):
        return maya.cmds.optionVar(q=self._project)

    @unityProjectPath.setter
    def unityProjectPath(self, value):
        maya.cmds.optionVar(sv=(self._project, value))

    @property
    def currentModelPath(self):
        try :
            return os.path.join(maya.cmds.getAttr("UnityFbxExportSet.unityFbxFilePath"), maya.cmds.getAttr("UnityFbxExportSet.unityFbxFileName"))
        except :
            return ""

    @currentModelPath.setter
    def currentModelPath(self, value):
        try:
            head, tail = os.path.split(value)
            maya.cmds.setAttr("UnityFbxExportSet.unityFbxFilePath", head, type="string")
            maya.cmds.setAttr("UnityFbxExportSet.unityFbxFileName", tail, type="string")
        except :
            pass
