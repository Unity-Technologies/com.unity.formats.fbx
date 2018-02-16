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
      set(UNITY "c:/Program Files/Unity2017.1.1f1")
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

if (DEFINED UNITY_EDITOR_DLL_PATH)
    message("Using ${UNITY_EDITOR_DLL_PATH}")
else()
    if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin")
        # The editor is   Unity.app/Contents/MacOS/Unity
        # The dlls are in Unity.app/Contents/Managed/*.dll
        # Monodevelop is  Monodevelop.app/Contents/
        get_filename_component(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_PATH}" PATH)
        get_filename_component(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_DLL_PATH}" DIRECTORY)
        set(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_DLL_PATH}/Managed")

        list(APPEND MONO_ROOT_PATH "${UNITY_EDITOR_DLL_PATH}/../../../Monodevelop.app/Contents/Frameworks/Mono.framework/Versions/Current")

    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Windows")
        # The editor is   .../Unity.exe
        # The dlls are in .../Data/Managed/*.dll
        get_filename_component(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_PATH}" PATH)
        set(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_DLL_PATH}/Data/Managed")

    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
        # The editor is   .../Unity
        # The dlls are in .../Data/Managed/*.dll
        get_filename_component(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_PATH}" PATH)
        set(UNITY_EDITOR_DLL_PATH "${UNITY_EDITOR_DLL_PATH}/Data/Managed")
    endif()
endif()

if (DEFINED UNITY_EXTENSION_PATHS)
    message("Using ${UNITY_EXTENSION_PATHS}")
else()
    if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin")
        # The editor is   Unity.app/Contents/MacOS/Unity
        # The dlls are in Unity.app/Contents/UnityExtensions/.../*.dll
        get_filename_component(UNITY_EXTENSION_ROOT "${UNITY_EDITOR_PATH}" PATH)
        get_filename_component(UNITY_EXTENSION_ROOT "${UNITY_EXTENSION_ROOT}" DIRECTORY)
		list(APPEND UNITY_EXTENSION_PATHS "${UNITY_EXTENSION_ROOT}/UnityExtensions/Unity/Timeline/Editor")
		list(APPEND UNITY_EXTENSION_PATHS "${UNITY_EXTENSION_ROOT}/UnityExtensions/Unity/Timeline/RuntimeEditor")
    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Windows")
        # The editor is   .../Unity.exe
        # The dlls are in .../Editor/Data/.../*.dll
        get_filename_component(UNITY_EXTENSION_ROOT "${UNITY_EDITOR_PATH}" PATH)
        set(UNITY_EXTENSION_ROOT "${UNITY_EXTENSION_ROOT}/Editor/Data")
		list(APPEND UNITY_EXTENSION_PATHS "${UNITY_EXTENSION_ROOT}/UnityExtensions/Unity/Timeline/Editor")
		list(APPEND UNITY_EXTENSION_PATHS "${UNITY_EXTENSION_ROOT}/UnityExtensions/Unity/Timeline/RuntimeEditor")
    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
        # The editor is   .../Unity
        # The dlls are in .../Editor/Data/.../*.dll
        get_filename_component(UNITY_EXTENSION_ROOT "${UNITY_EDITOR_PATH}" PATH)
        set(UNITY_EXTENSION_ROOT "${UNITY_EXTENSION_ROOT}/Editor/Data")
		list(APPEND UNITY_EXTENSION_PATHS "${UNITY_EXTENSION_ROOT}/UnityExtensions/Unity/Timeline/Editor")
		list(APPEND UNITY_EXTENSION_PATHS "${UNITY_EXTENSION_ROOT}/UnityExtensions/Unity/Timeline/RuntimeEditor")
    endif()
endif()


# Look for a dll on all platforms.
message("Looking for Unity*.dll in ${UNITY_EDITOR_DLL_PATH}

${UNITY_EXTENSION_PATHS}")
set(_platformLibrarySuffix ${CMAKE_FIND_LIBRARY_SUFFIXES})
set(CMAKE_FIND_LIBRARY_SUFFIXES ".dll")
find_library(CSHARP_UNITYEDITOR_LIBRARY UnityEditor.dll PATH ${UNITY_EDITOR_DLL_PATH})
find_library(CSHARP_UNITYENGINE_LIBRARY UnityEngine.dll PATH ${UNITY_EDITOR_DLL_PATH})
find_library(CSHARP_UNITYEDITOR_TIMELINE_LIBRARY UnityEditor.Timeline.dll  ${UNITY_EXTENSION_PATHS})
find_library(CSHARP_UNITYENGINE_TIMELINE_LIBRARY UnityEngine.Timeline.dll  ${UNITY_EXTENSION_PATHS})
set(CMAKE_FIND_LIBRARY_SUFFIXES ${_platformLibrarySuffix})

# Check whether we found everything we needed.
include(FindPackageHandleStandardArgs)
FIND_PACKAGE_HANDLE_STANDARD_ARGS(Unity DEFAULT_MSG UNITY_EDITOR_PATH CSHARP_UNITYEDITOR_LIBRARY)
