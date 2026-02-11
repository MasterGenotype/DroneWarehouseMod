# AGENTS.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

A SMAPI mod for Stardew Valley 1.6+ that adds a Drone Warehouse building with autonomous drones:
- **HarvestDrone** - Collects crops, forageables, bush berries, and tree fruits
- **WaterDrone** - Waters dry tilled soil
- **PetDrone** - Pets animals and pets
- **FarmerDrone** - Tills, clears debris, and plants seeds in designated zones

## Build Commands

```bash
# Build the mod
dotnet build

# Watch mode (rebuilds on file changes)
dotnet watch build --project ./DroneWarehouseMod.csproj

# Publish
dotnet publish
```

**Note**: Set `STARDEW_MODS_DIR` environment variable to your Stardew Valley Mods folder, or edit the path in `DroneWarehouseMod.csproj`.

## Testing

No automated test framework. Testing is done manually:
1. Build the mod
2. Copy output to Stardew Valley Mods folder (handled automatically by `Pathoschild.Stardew.ModBuildConfig`)
3. Launch game via SMAPI

## Architecture

### Entry Point
- `ModEntry.cs` - SMAPI mod entry, event subscriptions, input handling, rendering hooks

### Core Systems
- `Game/DroneManager.cs` - Central orchestrator: drone lifecycle, A* pathfinding, task reservation, landing queues, no-fly zone calculation, zone selection mode
- `Game/Drones/DroneBase.cs` - Abstract base class with state machine (Docked→Launching→Idle→MovingToTarget→WaitingAtTarget→ReturningDock→Landing)
- `Game/Drones/Types.cs` - Enums: `DroneKind`, `DroneState`, `DroneAnimMode`, `WorkKind`, and `DroneAnimSet` for animation frames

### Drone Implementations
Each extends `DroneBase` and implements:
- `TryAcquireWork()` - Find and reserve work targets
- `DoWorkAt()` - Execute the actual work
- `WorkAnimMode()` / `WorkDurationTicks()` - Animation configuration

### Data Layer
- `Core/ModDataKeys.cs` - String constants for `Building.modData` persistence (drone counts, farmer jobs, levels)
- `Core/GameDataPatcher.cs` - Hooks SMAPI Content API to register "DroneWarehouse" in `Data/Buildings` and swap lid textures
- `Core/DataCache.cs` - Caches crop/seed data from game assets

### UI
- `UI/DroneConsoleMenu.cs` - Management interface (create/scrap drones, upgrade warehouse)
- `UI/FarmerQueuesOverlay.cs` - Shows farmer drone task queues (F3 toggle)

### Assets
- `assets/data/drone_warehouse.json` - Building definition for Stardew's content system
- `assets/hub/` - Warehouse building textures (open/closed lid states)
- `assets/ui/*/` - Drone sprite animations (fly, launch, land, work states)
- `assets/audio/` - Custom sound effects

### Localization
- `i18n/` - Translation files (en.json, es.json, ru.json)
- Access via `Helper.Translation.Get("key")`

## Key Patterns

### Building State Persistence
Warehouse state stored in `Building.modData` using keys from `ModDataKeys`:
```csharp
building.modData[MD.Level] = "2";
building.modData[MD.CountHarvest] = "3";
building.modData[MD.FarmerJob0] = serializedJobData;
```

### Tile Actions
Custom tile actions registered in `OnGameLaunched`:
- `Jenya.DroneWarehouseMod_ChestTile` - Opens storage chest
- `Jenya.DroneWarehouseMod_ConsoleTile` - Opens management console

### Drone Work Flow
1. `DroneManager.Update()` ticks all active drones
2. Idle drones call `TryAcquireWork()` to claim a target tile
3. Pathfinding routes drone avoiding no-fly zones (building footprints)
4. At target, drone enters `WaitingAtTarget` state, plays work animation
5. `DoWorkAt()` executes game effect (harvest, water, etc.)
6. Drone returns to dock via landing queue

### Configuration
`ModConfig.cs` exposes all tunable parameters (speeds, capacities, timings). Users edit `config.json`.
