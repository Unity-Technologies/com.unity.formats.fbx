#-
########################################################################
# Copyright (c) 2017 Unity Technologies. All rights reserved.
# NOTICE: All information contained herein is, and remains
#         the property of Unity Technology Aps. and its suppliers,
#         if any.  The intellectual and technical concepts contained
#         herein are proprietary to Unity Technologies Aps. and its
#         suppliers and may be covered by Canadian, U.S. and/or
#         Foreign Patents, patents in process, and are protected
#         by trade secret or copyright law. Dissemination of this
#         information or reproduction of this material is strictly
#         forbidden unless prior written permission is obtained from
#         Unity Technology Aps.
#
########################################################################
#+
"""
@package ui
@author  Simon Inwood <simon.cf.inwood@gmail.com>
@defgroup UnityUI User Interface
@ingroup UnityUtils
"""

import maya.cmds              

from unityOneClick import (commands)

# ======================================================================'
# User Interface
# ======================================================================'

kMayaVersionAdded = '2017'
kMenuName = 'UnityOneClick'
kMenuDivider = 'UnityOneClickDivider'
kMenuLabel = 'UNITY'
kMenuInsertAfter = 'exportActiveFileOptions'

def register(pluginFn):
    """
    Register commands for plugin
    @param pluginFn (MFnPlugin): plugin object passed to initializePlugin
    """
    installMenu()
    
    return

def unregister(pluginFn):
    """
    Unregister commands for plugin
    @param pluginFn (MFnPlugin): plugin object passed to uninitializePlugin
    """
    uninstallMenu()
    
    return

def getParentMenu():
    result = maya.mel.eval('$tempVar = $gMainFileMenu;')
    maya.mel.eval("buildFileMenu")
    return result

def installMenu():
    """
    install menu into main window 
    @ingroup UnityUI
    """
    parentMenu = getParentMenu()

    maya.cmds.menuItem(kMenuDivider, 
                       divider=True, 
                       longDivider=False, 
                       insertAfter=kMenuInsertAfter, 
                       parent=parentMenu, 
                       version=kMayaVersionAdded)
    maya.cmds.menuItem(kMenuName, 
                       parent=parentMenu, 
                       insertAfter=kMenuDivider, 
                       image=commands.importCmd.familyIconPath(),
                       subMenu=True, 
                       label=kMenuLabel, 
                       annotation=commands.importCmd.kFamilyLabel, 
                       tearOff=True, 
                       version=kMayaVersionAdded)

    maya.cmds.menuItem(parent=kMenuName, 
                       label=commands.importCmd.kShortLabel, 
                       annotation=commands.importCmd.kLabel, 
                       command=commands.importCmd.kScriptCommand,
                       image=commands.importCmd.iconPath(),
                       version=kMayaVersionAdded)
    maya.cmds.menuItem(parent=kMenuName, 
                       label=commands.reviewCmd.kShortLabel, 
                       annotation=commands.reviewCmd.kLabel, 
                       command=commands.reviewCmd.kScriptCommand, 
                       image=commands.importCmd.iconPath(),
                       version=kMayaVersionAdded)
    maya.cmds.menuItem(parent=kMenuName, 
                       label=commands.publishCmd.kShortLabel, 
                       annotation=commands.publishCmd.kLabel, 
                       command=commands.publishCmd.kScriptCommand, 
                       image=commands.importCmd.iconPath(),
                       version=kMayaVersionAdded)

def uninstallMenu():
    """
    uninstall the unityOneClick menu from main window
    @ingroup UnityUI
    """
    if maya.cmds.menu(kMenuName, exists=True):     # @UndefinedVariable
        maya.cmds.deleteUI(kMenuDivider, menuItem=True)
        maya.cmds.deleteUI(kMenuName, menuItem=True)
        maya.cmds.deleteUI(kMenuName, menu=True)   # @UndefinedVariable
