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
"""
@package debug
@author  Simon Inwood <simon.cf.inwood@gmail.com>
@defgroup DebugUtils Debug Utilities
@ingroup UnityUtils
"""
from _collections import defaultdict
import maya.OpenMaya as OpenMaya        # @UnresolvedImport
import maya.cmds              

""" Set this variable to True to display debug messages in the scripting history window.   
@var gDebug
@brief Enable or disable the displaying of debug messages thru bool package variable.  
@ingroup DebugUtils
"""
gDebug = True

def debug_info(self, *args, **kwargs):
    """
    Format and print an debug message to stdout.
    @note this is used by LoggerMixin::displayDebug
    @ingroup DebugUtils
    """
    global gDebug
    
    if gDebug:
        try:
            classname = self.__name__
        except:
            classname = type(self).__name__
            
        prefix= kwargs['prefix'] if kwargs.has_key('prefix') else ''
        tabs= '\t' * int(kwargs['tabs']) if kwargs.has_key('tabs') else ''
        print '{3}{4}{0}.{1} {2}'.format(classname, args[:1], [args[1:]], tabs, prefix) 
    


