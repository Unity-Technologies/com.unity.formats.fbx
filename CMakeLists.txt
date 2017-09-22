# ***********************************************************************
# Copyright (c) 2017 Unity Technologies. All rights reserved.
#
# Licensed under the ##LICENSENAME##.
# See LICENSE.md file in the project root for full license information.
# ***********************************************************************

cmake_minimum_required (VERSION 3.8)

# Default is a release build.
if (NOT CMAKE_BUILD_TYPE)
  # CMAKE_BUILD_TYPE is special, so we have to CACHE FORCE to actually set it,
  # or else our 'set' has very wonky scope.
  set(CMAKE_BUILD_TYPE "Release" CACHE STRING "" FORCE)
elseif (CMAKE_BUILD_TYPE STREQUAL "Debug")
  list(APPEND CMAKE_CXX_FLAGS "-DMEMORY_DEBUG")
endif()
message(STATUS "Building for ${CMAKE_BUILD_TYPE}")

if (NOT DEFINED PACKAGE_VERSION OR "${PACKAGE_VERSION}" STREQUAL "")
    set(PACKAGE_VERSION "0.0.15a")
endif()
message(STATUS "Using Package Version: ${PACKAGE_VERSION}")

if (NOT DEFINED PACKAGE_PATH OR "${PACKAGE_PATH}" STREQUAL "")
    set(PACKAGE_PATH "${CMAKE_BINARY_DIR}/FbxExporters_${PACKAGE_VERSION}.unitypackage")
endif()
message(STATUS "Creating Exporter Package at: ${PACKAGE_PATH}")

if (NOT DEFINED FBXSDK_PACKAGE_PATH)
    # TODO: store the FbxSdk directly in this project
    set(FBXSDK_PACKAGE_PATH "${CMAKE_SOURCE_DIR}/../FbxSharpBuild")
    
    # find the most recent package at the path
    file(GLOB FBXSDK_PACKAGES "${FBXSDK_PACKAGE_PATH}/FbxSdk_*.*.*.unitypackage")
    set(NEWEST_PACKAGE "")
    foreach(fbxsdk_package ${FBXSDK_PACKAGES})
       if(fbxsdk_package IS_NEWER_THAN NEWEST_PACKAGE)
            set(NEWEST_PACKAGE ${fbxsdk_package})
       endif()
    endforeach(fbxsdk_package)
    
    if("${NEWEST_PACKAGE}" STREQUAL "")
        message(FATAL_ERROR "Failed to find FbxSdk Unity Package at: ${FBXSDK_PACKAGE_PATH}")
    else()
        set(FBXSDK_PACKAGE_PATH ${NEWEST_PACKAGE})
    endif()
endif()

IF(EXISTS ${FBXSDK_PACKAGE_PATH})
    message(STATUS "Using FbxSdk Package: ${FBXSDK_PACKAGE_PATH}")
ELSE()
    message(FATAL_ERROR "Cannot find FbxSdk Package: ${FBXSDK_PACKAGE_PATH}")
ENDIF()

# promote warnings to errors
if(MSVC)
set(PROJECT_COMPILE_FLAGS "/WX")
else()
# requires gcc 6 or higher
set(PROJECT_COMPILE_FLAGS "-Werror -Wno-error=null-dereference")
endif()

add_definitions(${PROJECT_COMPILE_FLAGS})

list(APPEND CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/deps/cmake)

###########################################################################
# Find packages that we need.

find_package(Unity REQUIRED)

set(Python_ADDITIONAL_VERSIONS 2.7)

###########################################################################
# Import FbxSharp package

set(FBXSDK_PACKAGE_TARGET import_fbxsdk)
add_custom_target(
    ${FBXSDK_PACKAGE_TARGET}
    # remove FbxSdk folder
    COMMAND ${CMAKE_COMMAND} -E remove_directory "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/FbxSdk"
    COMMAND "${UNITY_EDITOR_PATH}" -projectPath "${CMAKE_SOURCE_DIR}" -importPackage ${FBXSDK_PACKAGE_PATH} -quit
    COMMAND ${CMAKE_COMMAND} -E rename "${CMAKE_SOURCE_DIR}/Assets/FbxSdk" "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/FbxSdk"
)

###########################################################################
# TODO: Add target to replace the version number in README.txt

###########################################################################
# Zip integrations folder
set(MAYA_INTEGRATION_TARGET zip_maya_integration)
set(MAYA_INTEGRATION_ZIP_NAME "unityoneclick_for_maya.zip")

# remove existing zip file
file(REMOVE "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/${MAYA_INTEGRATION_ZIP_NAME}")
# remove .pyc files that we don't want to ship
file(GLOB PYC_FILES "${CMAKE_SOURCE_DIR}/Assets/Integrations/Autodesk/maya/scripts/unityOneClick/*.pyc")
IF( PYC_FILES )
    file(REMOVE ${PYC_FILES})
ENDIF()

add_custom_command(OUTPUT ${MAYA_INTEGRATION_ZIP_NAME}
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_SOURCE_DIR}/Assets/FbxExporters/${MAYA_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_SOURCE_DIR}/Assets/Integrations"
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}/Assets
    COMMENT "Zipping Maya Integration folder"
)
add_custom_target(${MAYA_INTEGRATION_TARGET} DEPENDS ${MAYA_INTEGRATION_ZIP_NAME})

###########################################################################
# Add target for creating a package
add_custom_command(
        OUTPUT ${PACKAGE_PATH}
        COMMAND "${UNITY_EDITOR_PATH}" -batchmode -projectPath ${CMAKE_SOURCE_DIR} -exportPackage Assets/FbxExporters ${PACKAGE_PATH} -quit
        COMMENT "Creating Unity Package ${PACKAGE_PATH}"
        DEPENDS ${MAYA_INTEGRATION_TARGET}
    )
add_custom_target(unitypackage ALL DEPENDS ${PACKAGE_PATH} ${FBXSDK_PACKAGE_TARGET} ${MAYA_INTEGRATION_TARGET})

enable_testing()
add_test(NAME run-all COMMAND "${UNITY_EDITOR_PATH}" -batchmode -projectPath ${CMAKE_SOURCE_DIR} runEditorTests -quit)
