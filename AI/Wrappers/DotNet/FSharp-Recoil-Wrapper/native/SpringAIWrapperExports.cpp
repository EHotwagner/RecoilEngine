/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#include "SpringAIWrapperInterface.h"

// Simple exports for F# P/Invoke - no complex AI interface management needed
// This is a minimal stub library for testing the data-oriented architecture

#ifdef __cplusplus
extern "C" {
#endif

// Re-export the core functions for P/Invoke
// These will be called directly from F# code

EXPORT int GetUnitCount_Export() {
    return GetUnitCount();
}

EXPORT int FillUnitArray_Export(Unit* units, int maxCount) {
    return FillUnitArray(units, maxCount);
}

EXPORT int FillResourceState_Export(ResourceState* resources) {
    return FillResourceState(resources);
}

EXPORT int ExecuteCommandBatch_Export(const Command* commands, int commandCount) {
    return ExecuteCommandBatch(commands, commandCount);
}

EXPORT float GetMetal_Export() {
    return GetMetal();
}

EXPORT float GetEnergy_Export() {
    return GetEnergy();
}

EXPORT int GetCurrentFrame_Export() {
    return GetCurrentFrame();
}

EXPORT int GetUnitsInRadius_Export(const Unit* allUnits, int unitCount, 
                                  float centerX, float centerY, float centerZ, 
                                  float radius, int* resultIds, int maxResults) {
    return GetUnitsInRadius(allUnits, unitCount, centerX, centerY, centerZ, radius, resultIds, maxResults);
}

EXPORT float GetMapWidth_Export() {
    return GetMapWidth();
}

EXPORT float GetMapHeight_Export() {
    return GetMapHeight();
}

EXPORT bool IsPositionValid_Export(float x, float y, float z) {
    return IsPositionValid(x, y, z);
}

#ifdef __cplusplus
}
#endif
    const char* engineVersionString, 
    int engineVersionNumber,
    const char* aiInterfaceShortName, 
    const char* aiInterfaceVersion) {
    
    // TODO: Implement proper version checking
    // For now, assume working support
    return LOS_Working;
}

// Skirmish AI related methods
EXPORT(const struct SSkirmishAILibrary*) loadSkirmishAILibrary(
    const char* const shortName,
    const char* const version) {
    
    if (!g_interface) {
        return nullptr;
    }

    return g_interface->LoadSkirmishAILibrary(shortName, version);
}

EXPORT(int) unloadSkirmishAILibrary(
    const char* const shortName,
    const char* const version) {
    
    if (!g_interface) {
        return -1;
    }

    return g_interface->UnloadSkirmishAILibrary(shortName, version);
}

EXPORT(int) unloadAllSkirmishAILibraries() {
    if (!g_interface) {
        return -1;
    }

    return g_interface->UnloadAllSkirmishAILibraries();
}

#ifdef __cplusplus
} // extern "C"
#endif
