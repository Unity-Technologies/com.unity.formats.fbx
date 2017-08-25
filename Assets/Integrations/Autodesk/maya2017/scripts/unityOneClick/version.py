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
@package version
@brief version file for UnityOneClick
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
    Return version string for the unityOneClick plugin
    @ingroup UnityOneClickPluginVersion
    """
    return '0.0.11a'

def pluginName():
    """
    Return name of unityOneClick plugin
    @ingroup UnityOneClickPluginVersion
    """
    return '{}.unityOneClick'.format(pluginPrefix())

def vendorName():
    """
    Return vendor name of unityOneClick plugin
    @ingroup UnityOneClickPluginVersion
    """
    return 'Unity Technology Aps.'
