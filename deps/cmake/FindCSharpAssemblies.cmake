# ***********************************************************************
# Copyright (c) 2017 Unity Technologies. All rights reserved.
#
# Licensed under the ##LICENSENAME##.
# See LICENSE.md file in the project root for full license information.
# ***********************************************************************

# We need to have Unity available to compile against it.
find_package(Unity REQUIRED)

# The list of .NET versions to look for.
# The first of these is the most-preferred.
set(NET_COMPILER_VERSIONS "4.5" "4.0.30319" "4.0")
set(NET_REFERENCE_ASSEMBLIES_VERSIONS "3.5" "2.0")
set(NET_LIB_VERSIONS "2.0.50727" "2.0")

# Platform-specific code.
if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin")
  SET(CMAKE_FIND_LIBRARY_SUFFIXES ".dll")

  # MONO_ROOT_PATH is from FindUnity (on Darwin)
  list(APPEND CSHARP_COMPILER_PATHS "${MONO_ROOT_PATH}/bin")
  foreach(VERSION ${NET_REFERENCE_ASSEMBLIES_VERSIONS})
      list(APPEND CSHARP_DLL_PATHS "${MONO_ROOT_PATH}/lib/mono/${VERSION}")
      list(APPEND REFERENCE_ASSEMBLIES_PATHS "${MONO_ROOT_PATH}/lib/mono/${VERSION}")
  endforeach()

  find_program(MONO_COMPILER mono PATH ${CSHARP_COMPILER_PATH} NO_DEFAULT_PATH)
  find_program(MONO_COMPILER mono PATH ${CSHARP_COMPILER_PATH})

elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Windows")
  message("Using .Net versions ${NET_COMPILER_VERSIONS}")
  SET(CMAKE_FIND_LIBRARY_SUFFIXES ".dll")

  set(DOT_NET_PATH "C:/Windows/Microsoft.NET/Framework")
  set(REFERENCE_ASSEMBLIES_PATH "C:/Program Files \(x86\)/Reference Assemblies/Microsoft/Framework")

  foreach(VERSION ${NET_COMPILER_VERSIONS})
    list(APPEND CSHARP_COMPILER_PATHS "${DOT_NET_PATH}/v${VERSION}")
  endforeach()

  foreach(VERSION ${NET_REFERENCE_ASSEMBLIES_VERSIONS})
    list(APPEND REFERENCE_ASSEMBLIES_PATHS "${REFERENCE_ASSEMBLIES_PATH}/v${VERSION}")
  endforeach()

  foreach(VERSION ${NET_LIB_VERSIONS})
    list(APPEND CSHARP_DLL_PATHS "${DOT_NET_PATH}/v${VERSION}")
  endforeach()

elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
  message(WARNING "Linux: Not Implemented")
endif()

message("Looking for C# compiler in ${CSHARP_COMPILER_PATHS}")
find_program(CSHARP_COMPILER mcs csc PATHS ${CSHARP_COMPILER_PATHS} NO_DEFAULT_PATH)
find_program(CSHARP_COMPILER mcs csc PATHS ${CSHARP_COMPILER_PATHS})
message("Found: ${CSHARP_COMPILER}\n")

message("Looking for mscorlib.dll in ${CSHARP_DLL_PATHS}")
find_library(CSHARP_MSCORLIB_LIBRARY mscorlib.dll PATHS ${CSHARP_DLL_PATHS} NO_DEFAULT_PATH)
find_library(CSHARP_MSCORLIB_LIBRARY mscorlib.dll PATHS ${CSHARP_DLL_PATHS})
message("Found: ${CSHARP_MSCORLIB_LIBRARY}\n")

message("Looking for System.dll in ${CSHARP_DLL_PATHS}")
find_library(CSHARP_SYSTEM_LIBRARY System.dll PATHS ${CSHARP_DLL_PATHS} NO_DEFAULT_PATH)
find_library(CSHARP_SYSTEM_LIBRARY System.dll PATHS ${CSHARP_DLL_PATHS})
message("Found: ${CSHARP_SYSTEM_LIBRARY}\n")

message("Looking for System.Core.dll in ${REFERENCE_ASSEMBLIES_PATHS}")
find_library(CSHARP_SYSTEM_CORE_LIBRARY System.Core.dll PATHS ${REFERENCE_ASSEMBLIES_PATHS} NO_DEFAULT_PATH)
find_library(CSHARP_SYSTEM_CORE_LIBRARY System.Core.dll PATHS ${REFERENCE_ASSEMBLIES_PATHS})
message("Found: ${CSHARP_SYSTEM_CORE_LIBRARY}\n")

# Standard code to report whether we found the package or not.
#include(${CMAKE_CURRENT_LIST_DIR}/FindPackageHandleStandardArgs.cmake)
FIND_PACKAGE_HANDLE_STANDARD_ARGS(CSHARP_ASSEMBLIES DEFAULT_MSG CSHARP_COMPILER CSHARP_MSCORLIB_LIBRARY CSHARP_SYSTEM_LIBRARY CSHARP_SYSTEM_CORE_LIBRARY)
