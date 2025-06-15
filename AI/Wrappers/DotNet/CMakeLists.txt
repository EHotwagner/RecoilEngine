### .NET AI Wrapper for RecoilEngine
#
# Global variables set in this file:
# * BUILD_DotNet_AIWRAPPER
# * DotNet_AIWRAPPER_TARGET
#

set(myName "DotNet")
set(mySourceDirRel "src")

# Check if the user wants to compile the wrapper
if("${AI_TYPES}" STREQUAL "ALL" OR "${AI_TYPES}" STREQUAL "DOTNET")
    set(AIWRAPPERS_DOTNET TRUE)
else("${AI_TYPES}" STREQUAL "ALL" OR "${AI_TYPES}" STREQUAL "DOTNET")
    set(AIWRAPPERS_DOTNET FALSE)
endif("${AI_TYPES}" STREQUAL "ALL" OR "${AI_TYPES}" STREQUAL "DOTNET")

# Check for .NET SDK
find_program(DOTNET_EXECUTABLE dotnet)
if(DOTNET_EXECUTABLE)
    set(DOTNET_FOUND TRUE)
    execute_process(
        COMMAND ${DOTNET_EXECUTABLE} --version
        OUTPUT_VARIABLE DOTNET_VERSION
        OUTPUT_STRIP_TRAILING_WHITESPACE
    )
    message(STATUS "Found .NET SDK version: ${DOTNET_VERSION}")
else()
    set(DOTNET_FOUND FALSE)
    message(WARNING ".NET SDK not found")
endif()

# Check dependencies of the wrapper are met
if(AIWRAPPERS_DOTNET AND DOTNET_FOUND AND NOT myName MATCHES "${AI_EXCLUDE_REGEX}")
    set_global(BUILD_${myName}_AIWRAPPER TRUE)
else(AIWRAPPERS_DOTNET AND DOTNET_FOUND AND NOT myName MATCHES "${AI_EXCLUDE_REGEX}")
    set_global(BUILD_${myName}_AIWRAPPER FALSE)
    message("warning: .NET AI Wrapper will not be built!")
endif(AIWRAPPERS_DOTNET AND DOTNET_FOUND AND NOT myName MATCHES "${AI_EXCLUDE_REGEX}")

# Build
if(BUILD_${myName}_AIWRAPPER)
    set(myDir "${CMAKE_CURRENT_SOURCE_DIR}")
    get_last_path_part(dirName ${myDir})
    set(myName "${dirName}")
    set(myTarget "${myName}-AIWrapper")
    make_absolute(mySourceDir "${myDir}" "${mySourceDirRel}")
    
    ai_wrapper_message(STATUS "Found AI Wrapper: ${myTarget}")
    
    set_global(${myName}_AIWRAPPER_TARGET ${myTarget})
    
    # Build native interop library
    set(myNativeTarget "${myTarget}-Native")
    set(myNativeSources
        "${mySourceDir}/native/DotNetExport.cpp"
        "${mySourceDir}/native/DotNetInterface.cpp"
    )
    
    add_library(${myNativeTarget} MODULE ${myNativeSources})
    target_link_libraries(${myNativeTarget} CUtils-static)
    target_include_directories(${myNativeTarget} PRIVATE
        ${CMAKE_SOURCE_DIR}/rts
        ${CMAKE_SOURCE_DIR}/rts/ExternalAI/Interface
        ${CMAKE_BINARY_DIR}/src-generated/engine
        ${CMAKE_SOURCE_DIR}/AI/Wrappers
    )
    
    # Set output directory
    set_target_properties(${myNativeTarget} PROPERTIES
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/AI/Wrappers/${myName}"
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/AI/Wrappers/${myName}"
    )
    
    # Build .NET managed assembly
    set(myManagedTarget "${myTarget}-Managed")
    set(myManagedProjectFile "${mySourceDir}/managed/SpringAI.Wrapper.csproj")
    
    add_custom_target(${myManagedTarget}
        COMMAND ${DOTNET_EXECUTABLE} build "${myManagedProjectFile}" 
            --configuration Release 
            --output "${CMAKE_BINARY_DIR}/AI/Wrappers/${myName}"
        WORKING_DIRECTORY "${myDir}"
        COMMENT "Building .NET AI Wrapper managed assembly"
        DEPENDS ${myNativeTarget}
    )
    
    # Main target that builds both native and managed components
    add_custom_target(${myTarget} DEPENDS ${myManagedTarget})
    
    # Install
    install(TARGETS ${myNativeTarget}
        DESTINATION ${AI_LIBS_DIR}/AI/Wrappers/${myName}
    )
    
    install(FILES 
        "${CMAKE_BINARY_DIR}/AI/Wrappers/${myName}/SpringAI.Wrapper.dll"
        DESTINATION ${AI_LIBS_DIR}/AI/Wrappers/${myName}
        OPTIONAL
    )
    
endif(BUILD_${myName}_AIWRAPPER)
