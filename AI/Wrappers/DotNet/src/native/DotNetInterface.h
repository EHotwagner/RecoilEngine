/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#ifndef DOTNET_INTERFACE_H
#define DOTNET_INTERFACE_H

#include "ExternalAI/Interface/SSkirmishAILibrary.h"
#include "ExternalAI/Interface/SAIInterfaceCallback.h"
#include "CUtils/SharedLibrary.h"

#include <map>
#include <string>

/**
 * @brief .NET AI Interface
 * 
 * This class manages .NET AI instances and handles the bridge between
 * the C AI interface and .NET managed code.
 */
class CDotNetInterface {
public:
    CDotNetInterface(int interfaceId, const SAIInterfaceCallback* callback);
    ~CDotNetInterface();

    // AI library management
    const SSkirmishAILibrary* LoadSkirmishAILibrary(
        const char* const shortName,
        const char* const version
    );
    
    int UnloadSkirmishAILibrary(
        const char* const shortName,
        const char* const version
    );
    
    int UnloadAllSkirmishAILibraries();

    // Static callback functions for the C interface
    static int HandleEvent(int skirmishAIId, int topicId, const void* data);
    static int InitAI(int skirmishAIId, const struct SSkirmishAICallback* callback);
    static int ReleaseAI(int skirmishAIId);

private:
    struct AIInfo {
        std::string shortName;
        std::string version;
        std::string assemblyPath;
        void* dotnetHandle;  // Handle to .NET AI instance
    };

    // Instance management
    int LoadDotNetRuntime();
    int UnloadDotNetRuntime();
    int CreateAIInstance(int skirmishAIId, const std::string& shortName, const std::string& version);
    int DestroyAIInstance(int skirmishAIId);

    // Helper functions
    std::string FindAIAssembly(const std::string& shortName, const std::string& version);
    void ReportError(const std::string& msg);

    // Data members
    const int interfaceId;
    const SAIInterfaceCallback* callback;
    
    std::map<int, AIInfo> loadedAIs;  // skirmishAIId -> AI info
    static CDotNetInterface* instance;  // Singleton for static callbacks
    
    bool dotnetRuntimeLoaded;
    void* dotnetRuntimeHandle;
};

// Function pointer types for .NET interop
typedef int (*DotNetInit_t)(int skirmishAIId, const char* assemblyPath, const struct SSkirmishAICallback* callback);
typedef int (*DotNetRelease_t)(int skirmishAIId);
typedef int (*DotNetHandleEvent_t)(int skirmishAIId, int topicId, const void* data);

#endif // DOTNET_INTERFACE_H
