########################################################################
# 9939962 CANADA INC.
# [2016] -  . All Rights Reserved.
# NOTICE: All information contained herein is, and remains
#         the property of 93. and its suppliers,
#         if any.  The intellectual and technical concepts contained
#         herein are proprietary to 9939962 Canada Inc. and its
#         suppliers and may be covered by Canadian, U.S. and/or
#         Foreign Patents, patents in process, and are protected
#         by trade secret or copyright law. Dissemination of this
#         information or reproduction of this material is strictly
#         forbidden unless prior written permission is obtained from
#         9939962 Canada Inc.
#
########################################################################
"""
Maya Unity Integration
@package unityOneClick
@author  Simon Inwood <simon.cf.inwood@gmail.com>
@defgroup unityOneClickPlugin Unity Plugin

@brief

@details

@defgroup UnityUtils Utilities
@defgroup UnityUI User Interface
@defgroup UnityUnitTests Unit Tests
"""

# list of public modules for package
__all__ = ["commands", "ui"]

try:             
    import maya.standalone             
    maya.standalone.initialize()         
    print "Unity standalone"
except: 
    pass

