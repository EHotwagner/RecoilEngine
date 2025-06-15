/* Test file for SpringAI Wrapper - validates the C++ stub library */

#include "SpringAIWrapperInterface.h"
#include <iostream>
#include <cassert>

int main() {
    std::cout << "Testing SpringAI Wrapper Native Library..." << std::endl;
    
    // Test basic queries
    std::cout << "Unit count: " << GetUnitCount() << std::endl;
    std::cout << "Metal: " << GetMetal() << std::endl;
    std::cout << "Energy: " << GetEnergy() << std::endl;
    std::cout << "Current frame: " << GetCurrentFrame() << std::endl;
    
    // Test map info
    std::cout << "Map size: " << GetMapWidth() << " x " << GetMapHeight() << std::endl;
    
    // Test unit array filling
    Unit units[10];
    int unitCount = FillUnitArray(units, 10);
    std::cout << "Filled " << unitCount << " units" << std::endl;
    
    for (int i = 0; i < unitCount; i++) {
        std::cout << "Unit " << units[i].id 
                  << " at (" << units[i].x << ", " << units[i].y << ", " << units[i].z << ")"
                  << " health: " << units[i].health << "/" << units[i].maxHealth
                  << std::endl;
    }
    
    // Test resource state
    ResourceState resources;
    if (FillResourceState(&resources)) {
        std::cout << "Resources - Metal: " << resources.metal 
                  << ", Energy: " << resources.energy 
                  << ", Frame: " << resources.currentFrame << std::endl;
    }
    
    // Test spatial query
    int nearbyIds[5];
    int nearbyCount = GetUnitsInRadius(units, unitCount, 150.0f, 0.0f, 150.0f, 100.0f, nearbyIds, 5);
    std::cout << "Found " << nearbyCount << " units near (150, 0, 150) within radius 100" << std::endl;
    
    // Test commands
    Command commands[2];
    commands[0] = {1, 1, -1, 120.0f, 0.0f, 120.0f, "", 1};  // Move command
    commands[1] = {2, 2, -1, 200.0f, 0.0f, 200.0f, "factory", 1};  // Build command
    
    int executedCount = ExecuteCommandBatch(commands, 2);
    std::cout << "Executed " << executedCount << " commands successfully" << std::endl;
    
    std::cout << "All tests completed successfully!" << std::endl;
    return 0;
}
