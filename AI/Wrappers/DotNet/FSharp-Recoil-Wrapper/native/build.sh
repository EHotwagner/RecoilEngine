#!/bin/bash
# build.sh - Build script for SpringAI Native Library

echo "=== SpringAI Native C++ Stub Library Build ==="
echo ""
echo "Directory structure:"
echo "native/"
echo "├── SpringAIWrapperInterface.h      # Core data structures and function declarations"
echo "├── SpringAIWrapperInterface.cpp    # Implementation with mock data" 
echo "├── SpringAIWrapperExports.cpp      # P/Invoke export functions"
echo "├── CMakeLists.txt                  # CMake build configuration"
echo "├── test_wrapper.cpp               # Validation test program"
echo "├── README.md                      # Documentation"
echo "└── build.sh                       # This build script"
echo ""

# Create build directory
echo "Creating build directory..."
mkdir -p build
cd build

# Configure with CMake
echo "Configuring with CMake..."
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    # Windows
    cmake -G "Visual Studio 17 2022" ..
    if [ $? -eq 0 ]; then
        echo "Building with Visual Studio..."
        cmake --build . --config Release
    fi
else
    # Linux/Mac
    cmake ..
    if [ $? -eq 0 ]; then
        echo "Building with make..."
        make -j$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)
    fi
fi

# Check if build succeeded
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Build completed successfully!"
    echo ""
    echo "Testing the library..."
    echo "=== Test Output ==="
    
    # Run test
    if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
        ./Release/SpringAIWrapperTest.exe 2>/dev/null || ./SpringAIWrapperTest.exe
    else
        ./SpringAIWrapperTest
    fi
    
    echo "=== End Test Output ==="
    echo ""
    echo "🎉 Native C++ stub library is ready for F# integration!"
    echo ""
    echo "Next steps:"
    echo "1. Update F# Interop.fs to use these P/Invoke functions"
    echo "2. Test F# array filling from native code"
    echo "3. Implement WorldState management in F#"
else
    echo ""
    echo "❌ Build failed. Please check the error messages above."
    echo ""
    echo "Common issues:"
    echo "- CMake not installed or not in PATH"
    echo "- Missing C++ compiler (Visual Studio, gcc, clang)"
    echo "- Missing build tools"
fi
