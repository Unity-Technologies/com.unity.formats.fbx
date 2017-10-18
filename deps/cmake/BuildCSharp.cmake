find_package(CSharpAssemblies REQUIRED)

macro(FBXEXPORTERS_COMPILE_CSHARP)
  cmake_parse_arguments(_csharpCompile
      ""
      "OUTPUT"
      "SOURCES;REFERENCES;DEPENDS;EXTRA_ARGS"
      ${ARGN})

  string(CONCAT _csharpCompile_compiler $<$<PLATFORM_ID:"Darwin">:"${MONO_COMPILER} "> "${CSHARP_COMPILER}")

  if(${CMAKE_SYSTEM_NAME} STREQUAL "Windows")
    set(_csharpCompile_warnings="/nowarn:1701,1702,2008")
  else()
    set(_csharpCompile_warnings="/nowarn:1701,1702")
  endif()

  file(TO_NATIVE_PATH "${_csharpCompile_SOURCES}" _csharpCompile_SOURCES)

  foreach(_csharpCompileRef ${_csharpCompile_REFERENCES})
    list(APPEND _csharpCompile_REFERENCE_ARGS "/reference:${_csharpCompileRef}")
  endforeach()

  add_custom_command(OUTPUT ${_csharpCompile_OUTPUT}
        COMMAND "${_csharpCompile_compiler}"
                /noconfig
                /langversion:4
                /nostdlib+
                /warn:4
                ${_csharpCompile_warnings}
                /optimize+
                /out:${_csharpCompile_OUTPUT}
                ${_csharpCompile_EXTRA_ARGS}
                ${_csharpCompile_REFERENCE_ARGS}
                ${_csharpCompile_SOURCES}
        DEPENDS ${_csharpCompile_DEPENDS})
endmacro()