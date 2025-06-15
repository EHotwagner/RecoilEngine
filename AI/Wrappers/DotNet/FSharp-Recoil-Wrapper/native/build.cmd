@echo off
REM build.cmd - Windows build script for SpringAI Native Library

echo === SpringAI Native C++ Stub Library Build ===
echo.
echo Directory structure:
echo native/
echo ├── SpringAIWrapperInterface.h      # Core data structures and function declarations
echo ├── SpringAIWrapperInterface.cpp    # Implementation with mock data
echo ├── SpringAIWrapperExports.cpp      # P/Invoke export functions
echo ├── CMakeLists.txt                  # CMake build configuration
echo ├── test_wrapper.cpp               # Validation test program
echo ├── README.md                      # Documentation
echo ├── build.sh                       # Unix build script
echo └── build.cmd                      # This Windows build script
echo.

REM Create build directory
echo Creating build directory...
if not exist build mkdir build
cd build

REM Configure with CMake
echo Configuring with CMake...
cmake -G "Visual Studio 17 2022" ..
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ CMake configuration failed. Trying with different generator...
    cmake -G "Visual Studio 16 2019" ..
    if %ERRORLEVEL% neq 0 (
        echo ❌ CMake configuration failed with multiple generators.
        echo.
        echo Common solutions:
        echo - Install Visual Studio 2019 or 2022 with C++ tools
        echo - Install CMake and add to PATH
        echo - Try: cmake -G "MinGW Makefiles" .. (if MinGW installed)
        goto :error
    )
)

REM Build with CMake
echo Building with Visual Studio...
cmake --build . --config Release
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ Build failed. Please check the error messages above.
    goto :error
)

echo.
echo ✅ Build completed successfully!
echo.
echo Testing the library...
echo === Test Output ===

REM Run test
if exist Release\SpringAIWrapperTest.exe (
    Release\SpringAIWrapperTest.exe
) else if exist SpringAIWrapperTest.exe (
    SpringAIWrapperTest.exe
) else (
    echo ❌ Test executable not found
    goto :error
)

echo === End Test Output ===
echo.
echo 🎉 Native C++ stub library is ready for F# integration!
echo.
echo Next steps:
echo 1. Update F# Interop.fs to use these P/Invoke functions
echo 2. Test F# array filling from native code
echo 3. Implement WorldState management in F#
goto :end

:error
echo.
echo ❌ Build failed. Please check the error messages above.
echo.
echo Common issues:
echo - Visual Studio not installed or missing C++ tools
echo - CMake not installed or not in PATH
echo - Missing Windows SDK

:end
pause
