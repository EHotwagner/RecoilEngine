# CMakeLists.txt for F# Data-Oriented Spring AI Wrapper

cmake_minimum_required(VERSION 3.16)
project(SpringAIWrapper)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Platform-specific settings
if(WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    add_definitions(-DNOMINMAX)
    set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
endif()

# Build type settings
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

# Compiler flags
if(MSVC)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /W3")
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /O2")
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /Od /Zi")
else()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wextra")
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -O3")
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} -g")
endif()

# Source files
set(SOURCES
    SpringAIWrapperInterface.cpp
    SpringAIWrapperExports.cpp
)

set(HEADERS
    SpringAIWrapperInterface.h
)

# Create shared library
add_library(SpringAIWrapper SHARED ${SOURCES} ${HEADERS})

# Set output name
set_target_properties(SpringAIWrapper PROPERTIES
    OUTPUT_NAME "SpringAIWrapper"
    VERSION 1.0.0
    SOVERSION 1
)

# Platform-specific output settings
if(WIN32)
    set_target_properties(SpringAIWrapper PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
        ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    )
else()
    set_target_properties(SpringAIWrapper PROPERTIES
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    )
endif()

# Install rules
install(TARGETS SpringAIWrapper
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

install(FILES ${HEADERS}
    DESTINATION include/SpringAI
)

# Create a simple test executable for validation
add_executable(SpringAIWrapperTest
    test_wrapper.cpp
)

target_link_libraries(SpringAIWrapperTest SpringAIWrapper)

# Print build information
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "C++ flags: ${CMAKE_CXX_FLAGS}")
message(STATUS "Platform: ${CMAKE_SYSTEM_NAME}")
