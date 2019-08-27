cmake_minimum_required (VERSION 3.8)

SET(CMAKE_INSTALL_PREFIX "${CMAKE_SOURCE_DIR}" CACHE PATH "Install path prefix")
MESSAGE(STATUS "CMAKE_INSTALL_PREFIX is: " ${CMAKE_INSTALL_PREFIX})

project (UnityFbxExporterEditorPackage NONE)

###########################################################################
# Zip integrations folder

set(MAYA_INTEGRATION_COPY_TARGET CopyMayaIntegration)
set(MAYA_INTEGRATION_ZIP_TARGET ZipMayaIntegration)
set(MAYA_INTEGRATION_ZIP_NAME "UnityFbxForMaya.zip")

set(MAX_INTEGRATION_ZIP_TARGET ZipMaxIntegration)
set(MAX_INTEGRATION_ZIP_NAME "UnityFbxForMax.zip")

configure_file("${CMAKE_SOURCE_DIR}/Integrations/Autodesk/maya/scripts/unitySetupUI.mel.in"
    "${CMAKE_BINARY_DIR}/Integrations/Autodesk/maya/scripts/unitySetupUI.mel"
    @ONLY
)

file(GLOB_RECURSE MAYA_INTEGRATION_FILES RELATIVE "${CMAKE_SOURCE_DIR}" "${CMAKE_SOURCE_DIR}/Integrations/Autodesk/maya/*")
list(FILTER MAYA_INTEGRATION_FILES EXCLUDE REGEX ".*/unitySetupUI.mel.in$")

add_custom_command(OUTPUT ${MAYA_INTEGRATION_FILES}
    COMMAND ${CMAKE_COMMAND} -E copy_directory
        "${CMAKE_SOURCE_DIR}/Integrations/Autodesk/maya"
        "${CMAKE_BINARY_DIR}/Integrations/Autodesk/maya"
    COMMAND ${CMAKE_COMMAND} -E remove 
        "${CMAKE_BINARY_DIR}/Integrations/Autodesk/maya/scripts/unitySetupUI.mel.in"
    COMMENT "Copying Maya Integration folder"
    WORKING_DIRECTORY "${CMAKE_SOURCE_DIR}"
)

add_custom_target(${MAYA_INTEGRATION_COPY_TARGET} ALL DEPENDS ${MAYA_INTEGRATION_FILES})

add_custom_command(OUTPUT ${MAYA_INTEGRATION_ZIP_NAME}
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_BINARY_DIR}/${MAYA_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_BINARY_DIR}/Integrations/Autodesk/maya"
    MAIN_DEPENDENCY "${CMAKE_SOURCE_DIR}/Integrations/Autodesk/maya" 
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
    COMMENT "Zipping Maya Integration folder"
)
add_custom_target(${MAYA_INTEGRATION_ZIP_TARGET} ALL DEPENDS ${MAYA_INTEGRATION_ZIP_NAME})
add_dependencies(${MAYA_INTEGRATION_ZIP_TARGET} ${MAYA_INTEGRATION_COPY_TARGET})

add_custom_command(OUTPUT ${MAX_INTEGRATION_ZIP_NAME}
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_BINARY_DIR}/${MAX_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_SOURCE_DIR}/Integrations/Autodesk/max"
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
    COMMENT "Zipping 3DsMax Integration folder"
)
add_custom_target(${MAX_INTEGRATION_ZIP_TARGET} ALL DEPENDS ${MAX_INTEGRATION_ZIP_NAME})

############################################################################
# Install project into build folder

install(
    FILES "${CMAKE_BINARY_DIR}/${MAYA_INTEGRATION_ZIP_NAME}" "${CMAKE_BINARY_DIR}/${MAX_INTEGRATION_ZIP_NAME}"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/com.unity.formats.fbx/Editor/Integrations~"
)
install(
    FILES "${CMAKE_SOURCE_DIR}/CHANGELOG.md" "${CMAKE_SOURCE_DIR}/LICENSE.md" "${CMAKE_SOURCE_DIR}/.npmignore"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/com.unity.formats.fbx"
)

