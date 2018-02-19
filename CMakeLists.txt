# ***********************************************************************
# Copyright (c) 2017 Unity Technologies. All rights reserved.
#
# Licensed under the ##LICENSENAME##.
# See LICENSE.md file in the project root for full license information.
# ***********************************************************************

cmake_minimum_required (VERSION 3.8)

SET(CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/FbxExporterProject/Assets" CACHE PATH "Install path prefix")
MESSAGE(STATUS "CMAKE_INSTALL_PREFIX is: " ${CMAKE_INSTALL_PREFIX})

project (UnityFbxExporterEditorDLL)

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
    set(PACKAGE_VERSION "sprint46")
endif()
message(STATUS "Using Package Version: ${PACKAGE_VERSION}")

if (NOT DEFINED PACKAGE_PATH OR "${PACKAGE_PATH}" STREQUAL "")
    set(PACKAGE_PATH "${CMAKE_BINARY_DIR}/FbxExporter_${PACKAGE_VERSION}.unitypackage")
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
find_package(CSharpAssemblies REQUIRED)
include(deps/cmake/BuildCSharp.cmake)

set(Python_ADDITIONAL_VERSIONS 2.7)

###########################################################################
# Import FbxSharp package

set(FBXSDK_PACKAGE_TARGET import_fbxsdk)
add_custom_target(
    ${FBXSDK_PACKAGE_TARGET}
    # remove FbxSdk folder
    COMMAND ${CMAKE_COMMAND} -E remove_directory "${CMAKE_SOURCE_DIR}/Assets/FbxSdk"
    COMMAND ${CMAKE_COMMAND} -E remove_directory "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxSdk"
    COMMAND "${UNITY_EDITOR_PATH}" -projectPath "${CMAKE_SOURCE_DIR}" -importPackage ${FBXSDK_PACKAGE_PATH} -quit
    COMMAND ${CMAKE_COMMAND} -E rename "${CMAKE_SOURCE_DIR}/Assets/FbxSdk" "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxSdk"
)

###########################################################################
# Add target to replace the unity package version number in files
set(README_TARGET readme_replace)
add_custom_target(
    ${README_TARGET}
    COMMAND ${PYTHON_EXECUTABLE}
            "${CMAKE_SOURCE_DIR}/scripts/file-search-replace.py"
            "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/README.txt"
            ".*VERSION.*:.*"
            "VERSION: ${PACKAGE_VERSION}"
    COMMAND ${PYTHON_EXECUTABLE}
            "${CMAKE_SOURCE_DIR}/scripts/file-search-replace.py"
            "${CMAKE_SOURCE_DIR}/Assets/Integrations/Autodesk/maya/scripts/unitySetupUI.mel"
            "{Version}"
            "${PACKAGE_VERSION}"        
    DEPENDS "${CMAKE_SOURCE_DIR}/scripts/file-search-replace.py"
    COMMENT "Replacing version number in files with ${PACKAGE_VERSION}"
)

###########################################################################
# Zip integrations folder
set(MAYA_INTEGRATION_TARGET zip_maya_integration)
set(MAYA_INTEGRATION_ZIP_NAME "UnityFbxForMaya.zip")

set(MAX_INTEGRATION_TARGET zip_max_integration)
set(MAX_INTEGRATION_ZIP_NAME "UnityFbxForMax.zip")

# remove existing zip files
file(REMOVE "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/${MAYA_INTEGRATION_ZIP_NAME}")
file(REMOVE "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/${MAX_INTEGRATION_ZIP_NAME}")

# remove .meta files from Integrations
file(GLOB_RECURSE META_FILES "${CMAKE_SOURCE_DIR}/Assets/Integrations/*.meta")

add_custom_command(OUTPUT ${MAYA_INTEGRATION_ZIP_NAME}
    IF(META_FILES)
        COMMAND ${CMAKE_COMMAND} -E remove ${META_FILES}
    ENDIF
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_SOURCE_DIR}/Assets/FbxExporters/${MAYA_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_SOURCE_DIR}/Assets/Integrations/Autodesk/maya" 
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}/Assets
    COMMENT "Zipping Maya Integration folder"
    DEPENDS ${README_TARGET}
)
add_custom_target(${MAYA_INTEGRATION_TARGET} DEPENDS ${MAYA_INTEGRATION_ZIP_NAME})


add_custom_command(OUTPUT ${MAX_INTEGRATION_ZIP_NAME}
    IF(META_FILES)
        COMMAND ${CMAKE_COMMAND} -E remove ${META_FILES}
    ENDIF
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_SOURCE_DIR}/Assets/FbxExporters/${MAX_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_SOURCE_DIR}/Assets/Integrations/Autodesk/max"
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}/Assets
    COMMENT "Zipping 3DsMax Integration folder"
)
add_custom_target(${MAX_INTEGRATION_TARGET} DEPENDS ${MAX_INTEGRATION_ZIP_NAME})

###########################################################################
# Compile editor scripts into a multi-platform DLL
if("${CMAKE_BUILD_TYPE}" STREQUAL "Release")
    set(CLASS_LIBRARY_DEST "${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}")

    add_custom_command(OUTPUT ${CLASS_LIBRARY_DEST}
        COMMAND ${CMAKE_COMMAND} -E make_directory ${CLASS_LIBRARY_DEST}
    )
    
    set(RUNTIME_CLASS_LIBRARY_NAME "UnityFbxPrefab.dll")
    fbxexporters_compile_csharp(OUTPUT ${CLASS_LIBRARY_DEST}/${RUNTIME_CLASS_LIBRARY_NAME}
            EXTRA_ARGS
                "/target:library"
            SOURCES
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/FbxPrefab.cs"
            REFERENCES
                "${CSHARP_MSCORLIB_LIBRARY}"
                "${CSHARP_SYSTEM_CORE_LIBRARY}"
                "${CSHARP_SYSTEM_LIBRARY}"
                "${CSHARP_UNITYEDITOR_LIBRARY}"
                "${CSHARP_UNITYENGINE_LIBRARY}"
            DEPENDS
                "${CLASS_LIBRARY_DEST}"
    )
    add_custom_target(UnityFbxExporterRuntimeDLL ALL DEPENDS ${CLASS_LIBRARY_DEST}/${RUNTIME_CLASS_LIBRARY_NAME})
    
    set(EDITOR_CLASS_LIBRARY_NAME "UnityFbxExporterEditor.dll")
    fbxexporters_compile_csharp(OUTPUT ${CLASS_LIBRARY_DEST}/${EDITOR_CLASS_LIBRARY_NAME}
            EXTRA_ARGS
                "/target:library"
            SOURCES
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxExporter.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxExportSettings.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/InstallIntegration.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxPrefabAutoUpdater.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxPrefabInspector.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/ConvertToModel.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxExporterRepairMissingScripts.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/ManualUpdateEditorWindow.cs"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxRotationCurve.cs"
            REFERENCES
                "${CSHARP_MSCORLIB_LIBRARY}"
                "${CSHARP_SYSTEM_CORE_LIBRARY}"
                "${CSHARP_SYSTEM_LIBRARY}"
                "${CSHARP_UNITYEDITOR_LIBRARY}"
                "${CSHARP_UNITYENGINE_LIBRARY}"
                "${CSHARP_UNITYEDITOR_TIMELINE_LIBRARY}"
                "${CSHARP_UNITYENGINE_TIMELINE_LIBRARY}"
                "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Editor/FbxSdk/Plugins/UnityFbxSdk.dll"
                ${CLASS_LIBRARY_DEST}/${RUNTIME_CLASS_LIBRARY_NAME}
            DEPENDS
                "${CLASS_LIBRARY_DEST}"
                ${FBXSDK_PACKAGE_TARGET}
                UnityFbxExporterRuntimeDLL
    )
    add_custom_target(UnityFbxExporterEditorDLL ALL DEPENDS ${CLASS_LIBRARY_DEST}/${EDITOR_CLASS_LIBRARY_NAME})
    
endif()

###########################################################################
# Add target for creating a package

# remove .pyc files that we don't want to ship
file(GLOB PYC_FILES "${CMAKE_SOURCE_DIR}/Assets/FbxExporters/Integrations/Autodesk/maya/scripts/UnityFbxForMaya/*.pyc")
IF( PYC_FILES )
    file(REMOVE ${PYC_FILES})
ENDIF()

add_custom_command(
        OUTPUT ${PACKAGE_PATH}
        COMMAND "${CMAKE_COMMAND}" -E remove_directory ${CMAKE_BINARY_DIR}/FbxExporterProject
        COMMAND "${CMAKE_COMMAND}" --build . --target install --config Release
        COMMAND "${CMAKE_COMMAND}" -E remove ${PACKAGE_PATH}
        COMMAND "${UNITY_EDITOR_PATH}" -batchmode -projectPath ${CMAKE_BINARY_DIR}/FbxExporterProject -exportPackage Assets/FbxExporters ${PACKAGE_PATH} -quit
        COMMENT "Creating Unity Package ${PACKAGE_PATH}"
    )
add_custom_target(unitypackage DEPENDS ${PACKAGE_PATH} ${FBXSDK_PACKAGE_TARGET} ${MAYA_INTEGRATION_TARGET} ${MAX_INTEGRATION_TARGET} ${README_TARGET})

enable_testing()
add_test(NAME run-all COMMAND "${UNITY_EDITOR_PATH}" -batchmode -projectPath ${CMAKE_SOURCE_DIR} runEditorTests -quit)


############################################################################
# Install project into build folder

if(CMAKE_BUILD_TYPE STREQUAL "Release")
    install(DIRECTORY ${CMAKE_SOURCE_DIR}/Assets/FbxExporters
            DESTINATION .
            PATTERN "Editor/UnitTests" EXCLUDE
            PATTERN "FbxTurnTableBase.cs" EXCLUDE
            PATTERN "FbxPrefab.cs" EXCLUDE
            PATTERN "Editor/FbxExporter.cs" EXCLUDE
            PATTERN "Editor/FbxExportSettings.cs" EXCLUDE
            PATTERN "Editor/InstallIntegration.cs" EXCLUDE
            PATTERN "Editor/ReviewLastSavedModel.cs" EXCLUDE
            PATTERN "Editor/EditorRotate.cs" EXCLUDE
            PATTERN "Editor/FbxPrefabAutoUpdater.cs" EXCLUDE
            PATTERN "Editor/ConvertToModel.cs" EXCLUDE
            PATTERN "Editor/ManualUpdateEditorWindow.cs" EXCLUDE
            PATTERN "Editor/FbxPrefabInspector.cs" EXCLUDE
            PATTERN "Editor/FbxExporterRepairMissingScripts.cs" EXCLUDE
            PATTERN "Editor/FbxRotationCurve.cs" EXCLUDE)
    install(FILES ${CLASS_LIBRARY_DEST}/${EDITOR_CLASS_LIBRARY_NAME} DESTINATION FbxExporters/Editor)
    install(FILES ${CLASS_LIBRARY_DEST}/${RUNTIME_CLASS_LIBRARY_NAME} DESTINATION FbxExporters)
else()
    install(DIRECTORY ${CMAKE_SOURCE_DIR}/Assets/FbxExporters
            DESTINATION .
            PATTERN "Editor/UnitTests" EXCLUDE
            PATTERN "FbxTurnTableBase.cs" EXCLUDE
            PATTERN "Editor/ReviewLastSavedModel.cs" EXCLUDE
            PATTERN "Editor/EditorRotate.cs" EXCLUDE)
endif()
install(DIRECTORY ${CMAKE_SOURCE_DIR}/meta/FbxExporters
        DESTINATION .)
install(FILES "${CMAKE_SOURCE_DIR}/meta/FbxExporters.meta" DESTINATION .)
install(FILES "${CMAKE_SOURCE_DIR}/RELEASE_NOTES_EXTERNAL.md" DESTINATION FbxExporters RENAME "RELEASE_NOTES.txt")
