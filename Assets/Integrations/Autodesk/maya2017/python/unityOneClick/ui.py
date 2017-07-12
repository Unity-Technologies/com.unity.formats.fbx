#-
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
#+

import maya.cmds              

from unityOneClick import (commands)

# ======================================================================'
# User Interface
# ======================================================================'

kMainWndMenuName = 'UnityOneClick'
kMainWndMenuLabel = 'Unity'

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

def installMenu():
    """
    install menu into main window 
    @ingroup UnityUI
    """
    maya.cmds.menu (kMainWndMenuName, parent='MayaWindow', label=kMainWndMenuLabel, tearOff=True) # @UndefinedVariable
    maya.cmds.menuItem(parent=kMainWndMenuName, label=commands.configureCmd.kShortLabel, command=commands.configureCmd.kScriptCommand)        # @UndefinedVariable
    maya.cmds.menuItem(parent=kMainWndMenuName, label=commands.reviewCmd.kShortLabel, command=commands.reviewCmd.kScriptCommand)    # @UndefinedVariable
    maya.cmds.menuItem(parent=kMainWndMenuName, label=commands.publishCmd.kShortLabel, command=commands.publishCmd.kScriptCommand)    # @UndefinedVariable

def uninstallMenu():
    """
    uninstall the sequencer menu from main window
    @ingroup UnityUI
    """
    if maya.cmds.menu(kMainWndMenuName, exists=True):     # @UndefinedVariable
        maya.cmds.deleteUI(kMainWndMenuName, menu=True)   # @UndefinedVariable
