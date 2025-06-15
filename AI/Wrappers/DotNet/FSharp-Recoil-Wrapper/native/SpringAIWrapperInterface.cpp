/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#include "DotNetInterface.h"
#include "CUtils/SimpleLog.h"
#include "System/StringUtil.h"
#include "ExternalAI/Interface/AISEvents.h"

#include <cassert>
#include <sstream>

// Static instance for callbacks
CDotNetInterface* CDotNetInterface::instance = nullptr;

CDotNetInterface::CDotNetInterface(int interfaceId, const SAIInterfaceCallback* callback)
    : interfaceId(interfaceId)
    , callback(callback)
    , dotnetRuntimeLoaded(false)
    , dotnetRuntimeHandle(nullptr)
{
    instance = this;
    
    // Initialize .NET runtime
    if (LoadDotNetRuntime() != 0) {
        ReportError("Failed to load .NET runtime");
    }
}

CDotNetInterface::~CDotNetInterface() {
    UnloadAllSkirmishAILibraries();
    UnloadDotNetRuntime();
    instance = nullptr;
}

const SSkirmishAILibrary* CDotNetInterface::LoadSkirmishAILibrary(
    const char* const shortName,
    const char* const version)
{
    if (!shortName || !version) {
        ReportError("Invalid AI name or version");
        return nullptr;
    }

    // Find the AI assembly
    std::string assemblyPath = FindAIAssembly(shortName, version);
    if (assemblyPath.empty()) {
        std::ostringstream msg;
        msg << "Could not find .NET AI assembly for " << shortName << " version " << version;
        ReportError(msg.str());
        return nullptr;
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
