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
"""
@package commands
@defgroup UnityCommands Commands
@ingroup UnityOneClickPlugin
@author  Simon Inwood <simon.cf.inwood@gmail.com>
"""

from unityOneClick.logger import LoggerMixin

import maya.OpenMaya as OpenMaya        # @UnresolvedImport
import maya.OpenMayaMPx as OpenMayaMPx  # @UnresolvedImport
import maya.mel
import maya.cmds

import unityOneClick.version as version

import ctypes
ctypes.pythonapi.PyCObject_AsVoidPtr.restype = ctypes.c_void_p
ctypes.pythonapi.PyCObject_AsVoidPtr.argtypes = [ctypes.py_object]

class BaseCommand(OpenMayaMPx.MPxCommand, LoggerMixin):
    """
    Base class for UnityOneClick Plugin Commands.
    """
    def __init__(self):
        OpenMayaMPx.MPxCommand.__init__(self)
        LoggerMixin.__init__(self)
        
    def __del__(self):
        LoggerMixin.__del__(self)
        # Note: MPxCommand does not define __del__

    def loadPlugin(self, plugin):
        if not maya.cmds.pluginInfo( plugin, query=True, loaded=True ):
            maya.cmds.loadPlugin( plugin )
            if not maya.cmds.pluginInfo( plugin, query=True, loaded=True ):
                self.displayDebug("Error: Failed to load {0} plugin".format(plugin))
                return False
        return True

    def loadDependencies(self):
          return self.loadPlugin('GamePipeline.mll')
    
class importCmd(BaseCommand):
    """
    Import FBX file from Unity Project and autoconfigure for publishing
    
    @ingroup UnityCommands
    """
    kLabel = 'Import FBX file from Unity Project and auto-configure for publishing'
    kShortLabel = 'Import'
    kCmdName = "{}Import".format(version.pluginPrefix())
    kScriptCommand = 'import maya.cmds;maya.cmds.{0}()'.format(kCmdName)
    kRuntimeCommand = "UnityOneClickImport"

    def __init__(self):
        super(self.__class__, self).__init__()

    @classmethod
    def creator(cls):
        return OpenMayaMPx.asMPxPtr(cls())

    @classmethod
    def syntaxCreator(cls):
        syntax = OpenMaya.MSyntax()
        return syntax

    @classmethod
    def scriptCmd(cls):
        return
    
    def doIt(self, args):
        strCmd = 'Import'
        self.displayDebug('doIt {0}'.format(strCmd))
        maya.mel.eval(strCmd)
        
    @classmethod
    def invoke(cls):
        """
        Invoke command using mel so that it is executed and logged to script editor log
        @return: void
        """
        strCmd = '{0};'.format(cls.kCmdName)
        maya.mel.eval(strCmd)   # @UndefinedVariable

class reviewCmd(BaseCommand):
    """
    Review Model in Unity
        
    @ingroup UnityCommands
    """
    kLabel = 'Review Model in Unity'
    kShortLabel = 'Review'
    kCmdName = "{}Review".format(version.pluginPrefix())
    kScriptCommand = 'import maya.cmds;maya.cmds.{0}()'.format(kCmdName)
    kRuntimeCommand = "UnityOneClickReview"
    
    def __init__(self):
        super(self.__class__, self).__init__()
    
    @classmethod
    def creator(cls):
        return OpenMayaMPx.asMPxPtr(cls())
    
    @classmethod
    def syntaxCreator(cls):
        syntax = OpenMaya.MSyntax()
        return syntax
    
    @classmethod
    def scriptCmd(cls):
        return
    
    def doIt(self, args):

        unityAppPath = maya.cmds.optionVar(q='UnityApp')
        unityProjectPath = maya.cmds.optionVar(q='UnityProject')
        unityCommand = "FbxExporters.Review.TurnTable.LastSavedModel"

        if maya.cmds.about(macOS=True):
            # Use 'open -a' to bring app to front if it has already been started.
            # Note that the unity command will not get called.
            melCommand = r'system("open -a \"{0}\" --args -projectPath {1} -executeMethod {2}");'\
                .format(unityAppPath, unityProjectPath, unityCommand)

        elif maya.cmds.about(linux=True):
            melCommand = r'system("\"{0}\" -projectPath {1} -executeMethod {2}");'\
                .format(unityAppPath, unityProjectPath, unityCommand)

        elif maya.cmds.about(windows=True):
            melCommand = r'system("start \"{0}\" \"{1}\" \"-projectPath {2} -executeMethod {3}\"");'\
                .format(unityProjectPath + "Assets/Integrations/BringToFront.exe", unityAppPath, unityProjectPath, unityCommand)

        else:
            raise NotImplementedError("missing platform implementation for {0}".format(maya.cmds.about(os=True)))

        self.displayDebug('doIt({0})'.format(melCommand))

        maya.mel.eval(melCommand)

    @classmethod
    def invoke(cls):
        """
            Invoke command using mel so that it is executed and logged to script editor log
            @return: void
            """
        strCmd = '{0};'.format(cls.kCmdName)
        maya.mel.eval(strCmd)   # @UndefinedVariable

class publishCmd(BaseCommand):
    """
    Publish Model in Unity
        
    @ingroup UnityCommands
    """
    kLabel = 'Publish Model to Unity'
    kShortLabel = 'Publish'
    kCmdName = "{}Publish".format(version.pluginPrefix())
    kScriptCommand = 'import maya.cmds;maya.cmds.{0}()'.format(kCmdName)
    kRuntimeCommand = "UnityOneClickPublish"
    
    def __init__(self):
        super(self.__class__, self).__init__()
    
    @classmethod
    def creator(cls):
        return OpenMayaMPx.asMPxPtr(cls())
    
    @classmethod
    def syntaxCreator(cls):
        syntax = OpenMaya.MSyntax()
        return syntax
    
    @classmethod
    def scriptCmd(cls):
        return
    
    def doIt(self, args):
        
        # make sure the GamePipeline plugin is loaded
        if not self.loadDependencies():
            return

        strCmd = 'SendToUnitySelection'
        self.displayDebug('doIt {0}'.format(strCmd))
        maya.mel.eval(strCmd)
        
    @classmethod
    def invoke(cls):
        """
            Invoke command using mel so that it is executed and logged to script editor log
            @return: void
            """
        strCmd = '{0};'.format(cls.kCmdName)
        maya.mel.eval(strCmd)   # @UndefinedVariable

class configureCmd(BaseCommand):
    """
    Configure Maya Scene for Reviewing and Publishing to Unity
    
    @ingroup UnityCommands
    """
    kLabel = 'Configure Maya to publish and review to a Unity Project'
    kShortLabel = 'Configure'
    kCmdName = "{}Configure".format(version.pluginPrefix())
    kScriptCommand = 'import maya.cmds;maya.cmds.{0}()'.format(kCmdName)
    kRuntimeCommand = "UnityOneClickConfigure"

    def __init__(self):
        super(self.__class__, self).__init__()

    @classmethod
    def creator(cls):
        return OpenMayaMPx.asMPxPtr(cls())

    @classmethod
    def syntaxCreator(cls):
        syntax = OpenMaya.MSyntax()
        return syntax

    @classmethod
    def scriptCmd(cls):
        return
    
    def doIt(self, args):
        # make sure the GamePipeline plugin is loaded
        if not self.loadDependencies():
            return
        
        strCmd = 'SendToUnitySetProject'
        self.displayDebug('doIt {0}'.format(strCmd))
        maya.mel.eval(strCmd)
        
    @classmethod
    def invoke(cls):
        """
        Invoke command using mel so that it is executed and logged to script editor log
        @return: void
        """
        strCmd = '{0};'.format(cls.kCmdName)
        maya.mel.eval(strCmd)   # @UndefinedVariable

def register(pluginFn):
    """
    Register commands for plugin
    @param pluginFn (MFnPlugin): plugin object passed to initializePlugin
    """
    pluginFn.registerCommand(importCmd.kCmdName, importCmd.creator, importCmd.syntaxCreator)
    pluginFn.registerCommand(reviewCmd.kCmdName, reviewCmd.creator, reviewCmd.syntaxCreator)
    pluginFn.registerCommand(publishCmd.kCmdName, publishCmd.creator, publishCmd.syntaxCreator)
    pluginFn.registerCommand(configureCmd.kCmdName, configureCmd.creator, configureCmd.syntaxCreator)
    
    return

def unregister(pluginFn):
    """
    Unregister commands for plugin
    @param pluginFn (MFnPlugin): plugin object passed to uninitializePlugin
    """
    pluginFn.deregisterCommand(importCmd.kCmdName)
    pluginFn.deregisterCommand(reviewCmd.kCmdName)
    pluginFn.deregisterCommand(publishCmd.kCmdName)
    pluginFn.deregisterCommand(configureCmd.kCmdName)
    return

#===============================================================================
# UNIT TESTS
#===============================================================================
import unittest
from unityOneClick.basetestcase import BaseTestCase

class BaseCmdTest(BaseTestCase):
    """Base class for command UnitTests
    @ingroup UnityUnitTests
    """
    __cmd__ = None
    
    def setUp(self):
        super(BaseCmdTest,self).setUp()
        maya.cmds.loadPlugin( 'unityOneClickPlugin.py', quiet=True )  # @UndefinedVariable
        
    # test routine 
    def test_invoke(self):
        if self.__cmd__:
            self.__cmd__.invoke()

class importCmdTestCase(BaseCmdTest):
    """UnitTest for testing the importCmd command
    """
    __cmd__ = importCmd

class reviewCmdTestCase(BaseCmdTest):
    """UnitTest for testing the reviewCmd command
    """
    __cmd__ = reviewCmd

class publishCmdTestCase(BaseCmdTest):
    """UnitTest for testing the publishCmd command
    """
    __cmd__ = publishCmd

class configureCmdTestCase(BaseCmdTest):
    """UnitTest for testing the configureCmd command
    """
    __cmd__ = configureCmd

# NOTE: update this for test discovery
test_cases = (importCmdTestCase, reviewCmdTestCase, publishCmdTestCase, configureCmdTestCase,)

def load_tests(loader, tests, pattern):
    suite = unittest.TestSuite()
    for test_class in test_cases:
        tests = loader.loadTestsFromTestCase(test_class)
        suite.addTests(tests)
    return suite

if __name__ == '__main__':
    unittest.main()
