/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#include "DotNetInterface.h"
#include "ExternalAI/Interface/aidefines.h"
#include "ExternalAI/Interface/SAIInterfaceLibrary.h"
#include "ExternalAI/Interface/ELevelOfSupport.h"

#include <memory>

// check if the correct defines are set by the build system
#if !defined BUILDING_AI_INTERFACE
#	error BUILDING_AI_INTERFACE should be defined when building AI Interfaces
#endif
#if !defined BUILDING_AI
#	error BUILDING_AI should be defined when building AI Interfaces
#endif
#if defined BUILDING_SKIRMISH_AI
#	error BUILDING_SKIRMISH_AI should not be defined when building AI Interfaces
#endif

static std::unique_ptr<CDotNetInterface> g_interface;

#ifdef __cplusplus
extern "C" {
#endif

// Static AI interface library functions
EXPORT(int) initStatic(int interfaceId, const struct SAIInterfaceCallback* callback) {
    if (g_interface) {
        return -1;  // Already initialized
    }

    try {
        g_interface = std::make_unique<CDotNetInterface>(interfaceId, callback);
        return 0;
    } catch (...) {
        return -2;  // Initialization failed
    }
}

EXPORT(int) releaseStatic() {
    if (!g_interface) {
        return -1;  // Not initialized
    }

    g_interface.reset();
    return 0;
}

EXPORT(enum LevelOfSupport) getLevelOfSupportFor(
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
