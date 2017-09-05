# ***********************************************************************
# Copyright (c) 2017 Unity Technologies. All rights reserved.
#
# Licensed under the ##LICENSENAME##.
# See LICENSE.md file in the project root for full license information.
# ***********************************************************************

# Find Unity and set things up to be able to compile Unity.

# If UNITY is set to the root of the Unity install, we use it, otherwise we set
# it to the default install location.

if (NOT DEFINED UNITY)
    if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin")
      set(UNITY "/Applications/Unity")
    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Windows")
      set(CMAKE_FIND_LIBRARY_SUFFIXES ".dll")
      set(UNITY "c:/Program Files/Unity")
    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
      set(UNITY "/opt/Unity")
    endif()
endif()

# Be generous about how to interpret UNITY:
# it can be the directory that includes the Unity executable,
# or the root of the Unity installation. On mac it can be the app bundle.
list(APPEND UNITY_EXECUTABLE_PATHS "${UNITY}")
if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin")
  list(APPEND UNITY_EXECUTABLE_PATHS "${UNITY}/Contents/MacOS")
  list(APPEND UNITY_EXECUTABLE_PATHS "${UNITY}/Unity.app/Contents/MacOS")
elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Windows")
  list(APPEND UNITY_EXECUTABLE_PATHS "${UNITY}/Editor")
elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
  list(APPEND UNITY_EXECUTABLE_PATHS "${UNITY}/Editor")
endif()

find_program(UNITY_EDITOR_PATH Unity PATHS ${UNITY_EXECUTABLE_PATHS})


# Check whether we found everything we needed.
include(FindPackageHandleStandardArgs)
FIND_PACKAGE_HANDLE_STANDARD_ARGS(Unity DEFAULT_MSG UNITY_EDITOR_PATH)
