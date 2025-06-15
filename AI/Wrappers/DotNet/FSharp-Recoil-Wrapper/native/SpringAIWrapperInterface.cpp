/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#include "SpringAIWrapperInterface.h"
#include <cstring>
#include <cmath>
#include <algorithm>

// Mock data for testing - in real implementation these would call Spring Engine APIs
static Unit mockUnits[] = {
    {1, 101, 100.0f, 0.0f, 100.0f, 100.0f, 100.0f, 0, 1},  // Commander
    {2, 102, 150.0f, 0.0f, 150.0f, 80.0f, 80.0f, 0, 1},    // Builder
    {3, 103, 200.0f, 0.0f, 200.0f, 60.0f, 60.0f, 0, 1},    // Scout
    {4, 104, 250.0f, 0.0f, 250.0f, 120.0f, 120.0f, 0, 1},  // Fighter
    {5, 105, 300.0f, 0.0f, 300.0f, 200.0f, 200.0f, 0, 1}   // Factory
};

static ResourceState mockResources = {
    1000.0f,    // metal
    500.0f,     // energy
    5000.0f,    // metalStorage
    2500.0f,    // energyStorage
    10.0f,      // metalIncome
    5.0f,       // energyIncome
    1           // currentFrame
};

static int currentFrame = 1;
static const int MOCK_UNIT_COUNT = sizeof(mockUnits) / sizeof(mockUnits[0]);

// Core array filling functions
EXPORT int FillUnitArray(Unit* units, int maxCount) {
    if (!units || maxCount <= 0) {
        return 0;
    }
    
    int copyCount = std::min(maxCount, MOCK_UNIT_COUNT);
    memcpy(units, mockUnits, copyCount * sizeof(Unit));
    
    return copyCount;
}

EXPORT int FillResourceState(ResourceState* resources) {
    if (!resources) {
        return 0;
    }
    
    // Update mock resources slightly each frame
    mockResources.currentFrame = currentFrame++;
    mockResources.metal += mockResources.metalIncome;
    mockResources.energy += mockResources.energyIncome;
    
    *resources = mockResources;
    return 1;
}

EXPORT int ExecuteCommandBatch(const Command* commands, int commandCount) {
    if (!commands || commandCount <= 0) {
        return 0;
    }
    
    // Mock implementation - just count successful commands
    int successCount = 0;
    for (int i = 0; i < commandCount; i++) {
        // In real implementation, would validate and execute each command
        // For now, just increment success count
        successCount++;
    }
    
    return successCount;
}

// Basic information queries
EXPORT int GetUnitCount() {
    return MOCK_UNIT_COUNT;
}

EXPORT float GetMetal() {
    return mockResources.metal;
}

EXPORT float GetEnergy() {
    return mockResources.energy;
}

EXPORT int GetCurrentFrame() {
    return mockResources.currentFrame;
}

// Spatial queries
EXPORT int GetUnitsInRadius(const Unit* allUnits, int unitCount, 
                           float centerX, float centerY, float centerZ, 
                           float radius, int* resultIds, int maxResults) {
    if (!allUnits || !resultIds || unitCount <= 0 || maxResults <= 0) {
        return 0;
    }
    
    int foundCount = 0;
    float radiusSquared = radius * radius;
    
    for (int i = 0; i < unitCount && foundCount < maxResults; i++) {
        const Unit& unit = allUnits[i];
        
        // Calculate distance squared to avoid sqrt
        float dx = unit.x - centerX;
        float dy = unit.y - centerY;
        float dz = unit.z - centerZ;
        float distanceSquared = dx*dx + dy*dy + dz*dz;
        
        if (distanceSquared <= radiusSquared) {
            resultIds[foundCount++] = unit.id;
        }
    }
    
    return foundCount;
}

// Map information
EXPORT float GetMapWidth() {
    return 2048.0f;  // Mock map size
}

EXPORT float GetMapHeight() {
    return 2048.0f;  // Mock map size
}

EXPORT bool IsPositionValid(float x, float y, float z) {
    // Simple bounds check for mock map
    return (x >= 0.0f && x <= 2048.0f && 
            z >= 0.0f && z <= 2048.0f &&
            y >= -100.0f && y <= 1000.0f);
}

    // Create AI library structure
    static SSkirmishAILibrary aiLibrary;
    aiLibrary.getLevelOfSupportFor = nullptr;  // Optional
    aiLibrary.init = InitAI;
    aiLibrary.release = ReleaseAI;
    aiLibrary.handleEvent = HandleEvent;

    simpleLog_logL(SIMPLELOG_LEVEL_INFO, 
        "Loaded .NET AI: %s-%s from %s", shortName, version, assemblyPath.c_str());

    return &aiLibrary;
}

int CDotNetInterface::UnloadSkirmishAILibrary(
    const char* const shortName,
    const char* const version)
{
    // In this implementation, we don't need to do much here
    // as the actual cleanup happens in ReleaseAI
    return 0;
}

int CDotNetInterface::UnloadAllSkirmishAILibraries() {
    // Clean up all loaded AI instances
    for (auto& pair : loadedAIs) {
        DestroyAIInstance(pair.first);
    }
    loadedAIs.clear();
    return 0;
}

// Static callback functions
int CDotNetInterface::InitAI(int skirmishAIId, const struct SSkirmishAICallback* callback) {
    if (!instance) {
        return -1;
    }

    // For now, we'll need to determine which AI to load based on the skirmishAIId
    // In a real implementation, this information would come from the engine
    // For demonstration, we'll use a default AI
    return instance->CreateAIInstance(skirmishAIId, "ExampleDotNetAI", "1.0");
}

int CDotNetInterface::ReleaseAI(int skirmishAIId) {
    if (!instance) {
        return -1;
    }

    return instance->DestroyAIInstance(skirmishAIId);
}

int CDotNetInterface::HandleEvent(int skirmishAIId, int topicId, const void* data) {
    if (!instance) {
        return -1;
    }

    // Find the AI instance
    auto it = instance->loadedAIs.find(skirmishAIId);
    if (it == instance->loadedAIs.end()) {
        return -2;  // AI not found
    }

    // TODO: Forward event to .NET AI instance
    // This would involve marshalling the event data and calling into .NET
    
    return 0;  // Success
}

// Private helper methods
int CDotNetInterface::LoadDotNetRuntime() {
    // TODO: Load .NET runtime (hostfxr/nethost)
    // For now, assume it's loaded
    dotnetRuntimeLoaded = true;
    return 0;
}

int CDotNetInterface::UnloadDotNetRuntime() {
    if (dotnetRuntimeLoaded && dotnetRuntimeHandle) {
        // TODO: Unload .NET runtime
        dotnetRuntimeHandle = nullptr;
        dotnetRuntimeLoaded = false;
    }
    return 0;
}

int CDotNetInterface::CreateAIInstance(int skirmishAIId, const std::string& shortName, const std::string& version) {
    // Check if AI is already loaded
    if (loadedAIs.find(skirmishAIId) != loadedAIs.end()) {
        return -1;  // Already loaded
    }

    // Find assembly path
    std::string assemblyPath = FindAIAssembly(shortName, version);
    if (assemblyPath.empty()) {
        return -2;  // Assembly not found
    }

    // Create AI info
    AIInfo aiInfo;
    aiInfo.shortName = shortName;
    aiInfo.version = version;
    aiInfo.assemblyPath = assemblyPath;
    aiInfo.dotnetHandle = nullptr;  // TODO: Create actual .NET instance

    loadedAIs[skirmishAIId] = aiInfo;

    simpleLog_logL(SIMPLELOG_LEVEL_INFO, 
        "Created .NET AI instance %d: %s-%s", skirmishAIId, shortName.c_str(), version.c_str());

    return 0;
}

int CDotNetInterface::DestroyAIInstance(int skirmishAIId) {
    auto it = loadedAIs.find(skirmishAIId);
    if (it == loadedAIs.end()) {
        return -1;  // AI not found
    }

    // TODO: Clean up .NET instance
    if (it->second.dotnetHandle) {
        // Dispose .NET AI instance
    }

    loadedAIs.erase(it);

    simpleLog_logL(SIMPLELOG_LEVEL_INFO, 
        "Destroyed .NET AI instance %d", skirmishAIId);

    return 0;
}

std::string CDotNetInterface::FindAIAssembly(const std::string& shortName, const std::string& version) {
    // TODO: Implement proper AI assembly discovery
    // For now, return a placeholder path
    return "SpringAI.Wrapper.dll";
}

void CDotNetInterface::ReportError(const std::string& msg) {
    simpleLog_logL(SIMPLELOG_LEVEL_ERROR, ".NET Interface: %s", msg.c_str());
}
