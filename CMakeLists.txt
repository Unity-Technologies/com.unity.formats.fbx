cmake_minimum_required (VERSION 3.8)

SET(CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/install" CACHE PATH "Install path prefix")
MESSAGE(STATUS "CMAKE_INSTALL_PREFIX is: " ${CMAKE_INSTALL_PREFIX})

project (UnityFbxExporterEditorPackage NONE)

###########################################################################
# Zip integrations folder

set(MAYA_INTEGRATION_TARGET zip_maya_integration)
set(MAYA_INTEGRATION_ZIP_NAME "UnityFbxForMaya.zip")

set(MAX_INTEGRATION_TARGET zip_max_integration)
set(MAX_INTEGRATION_ZIP_NAME "UnityFbxForMax.zip")

add_custom_command(OUTPUT ${MAYA_INTEGRATION_ZIP_NAME}
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_BINARY_DIR}/${MAYA_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_SOURCE_DIR}/Integrations/Autodesk/maya" 
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
    COMMENT "Zipping Maya Integration folder"
)
add_custom_target(${MAYA_INTEGRATION_TARGET} ALL DEPENDS ${MAYA_INTEGRATION_ZIP_NAME})

add_custom_command(OUTPUT ${MAX_INTEGRATION_ZIP_NAME}
    COMMAND ${CMAKE_COMMAND} -E tar "cfv" ${CMAKE_BINARY_DIR}/${MAX_INTEGRATION_ZIP_NAME} --format=zip
       "${CMAKE_SOURCE_DIR}/Integrations/Autodesk/max"
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
    COMMENT "Zipping 3DsMax Integration folder"
)
add_custom_target(${MAX_INTEGRATION_TARGET} ALL DEPENDS ${MAX_INTEGRATION_ZIP_NAME})

############################################################################
# Install project into build folder

install(
    DIRECTORY "${CMAKE_SOURCE_DIR}/proto.com.unity.formats.fbx/"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/com.unity.formats.fbx"
)
install(
    FILES "${CMAKE_BINARY_DIR}/${MAYA_INTEGRATION_ZIP_NAME}" "${CMAKE_BINARY_DIR}/${MAX_INTEGRATION_ZIP_NAME}"
    DESTINATION "${CMAKE_INSTALL_PREFIX}/com.unity.formats.fbx/Editor/Integrations~"
)
install(
    FILES "${CMAKE_SOURCE_DIR}/CHANGELOG.md" "${CMAKE_SOURCE_DIR}/LICENSE.md" 
    DESTINATION "${CMAKE_INSTALL_PREFIX}/com.unity.formats.fbx"
)
