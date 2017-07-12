########################################################################
# Unity Technologes Aps.
# [2017] -  . All Rights Reserved.
# NOTICE: All information contained herein is, and remains
#         the property of Unity Technologes Aps. and its suppliers,
#         if any.  The intellectual and technical concepts contained
#         herein are proprietary to Unity Technologes Aps. and its
#         suppliers and may be covered by Canadian, U.S. and/or
#         Foreign Patents, patents in process, and are protected
#         by trade secret or copyright law. Dissemination of this
#         information or reproduction of this material is strictly
#         forbidden unless prior written permission is obtained from
#         Unity Technologes Aps.
#
########################################################################
"""
@package version
@brief version file for UnityOneClick editor
@author  Simon Inwood <simon.cf.inwood@gmail.com>
@defgroup UnityOneClickPluginVersion Plugin Version
@ingroup UnityOneClickPlugin
"""

def pluginPrefix():
    """
    Return prefix to use for commands and Maya Object names
    @ingroup UnityOneClickPluginVersion
    """
    return 'unity'

def versionName():
    """
    Return version string for the sequencer plugin
    @ingroup UnityOneClickPluginVersion
    """
    return '0.04a-sprint16'

def pluginName():
    """
    Return name of sequencer plugin
    @ingroup UnityOneClickPluginVersion
    """
    return '{}.unityOneClick'.format(pluginPrefix())

def vendorName():
    """
    Return vendor name of sequencer plugin
    @ingroup UnityOneClickPluginVersion
    """
    return 'Unity Technology Aps.'
