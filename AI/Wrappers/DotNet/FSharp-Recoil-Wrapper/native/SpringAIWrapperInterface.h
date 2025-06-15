/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#ifndef SPRING_AI_WRAPPER_INTERFACE_H
#define SPRING_AI_WRAPPER_INTERFACE_H

#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Data structures for F# interop
typedef struct {
    int id;
    int defId;
    float x, y, z;              // Position
    float health;
    float maxHealth;
    int teamId;
    int state;                  // UnitState enum value
} Unit;

typedef struct {
    float metal;
    float energy;
    float metalStorage;
    float energyStorage;
    float metalIncome;
    float energyIncome;
    int currentFrame;
} ResourceState;

typedef struct {
    int commandType;            // Command type enum
    int unitId;                 // Unit to command (for Move, Attack, etc.)
    int targetUnitId;           // Target unit (for Attack, Repair, etc.)
    float x, y, z;              // Position (for Move, Build, etc.)
    char buildUnitName[64];     // Unit type to build
    int priority;
} Command;

// Core array filling functions - these are the main interface points
EXPORT int FillUnitArray(Unit* units, int maxCount);
EXPORT int FillResourceState(ResourceState* resources);
EXPORT int ExecuteCommandBatch(const Command* commands, int commandCount);

// Basic information queries
EXPORT int GetUnitCount();
EXPORT float GetMetal();
EXPORT float GetEnergy();
EXPORT int GetCurrentFrame();

// Spatial queries for efficient AI processing  
EXPORT int GetUnitsInRadius(const Unit* allUnits, int unitCount, 
                           float centerX, float centerY, float centerZ, 
                           float radius, int* resultIds, int maxResults);

// Map information
EXPORT float GetMapWidth();
EXPORT float GetMapHeight();
EXPORT bool IsPositionValid(float x, float y, float z);

#ifdef __cplusplus
}
#endif

#endif // SPRING_AI_WRAPPER_INTERFACE_H
