# Changes From Upstream

This document lists the changes made in this fork (MasterGenotype/DroneWarehouseMod) compared to the upstream repository by Jenya (LodGvedeon).

## Generic Mod Config Menu (GMCM) Integration

- Added full [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) support so all mod settings can be configured in-game via a GUI instead of editing `config.json` manually
- Added `Core/IGenericModConfigMenuApi.cs` interface for GMCM API integration
- Registered all mod config options across 8 sections:
  - **General** — Work Off-Farm toggle
  - **Rendering** — Draw Under Trees, Draw In Front Near Hatch, Near Hatch Radius, Hatch X/Y Offsets
  - **Harvester** — Skip Flower Crops, Skip Fruit Trees
  - **Waterer** — Refill At Hatch toggle
  - **Pathing** — Scan Interval, No-Fly Padding, Line-of-Sight Padding
  - **Capacities** — Harvest Capacity, Water Max Charges, Pet Max Charges
  - **Speeds** — Harvester, Waterer, Petter, and Farmer drone speeds
  - **Farmer Timings** — Work Duration, Clear Duration
  - **Audio** — Enable Custom SFX, Custom SFX Volume

## Zone Selection Overlay (Touch/Mobile UI)

- Added `UI/ZoneSelectionOverlay.cs` — a new overlay with on-screen buttons during zone selection mode for Android/touch users who cannot use keyboard shortcuts
- Four buttons displayed at the top of the screen: **Size**, **Undo**, **Start**, **Cancel**
- Includes a hint label ("Tap to place zones") for discoverability
- Buttons are resolution-aware and reposition on viewport changes

## Building Tile Actions

- Added tile action registrations to `drone_warehouse.json` so the warehouse building responds to direct clicks/taps:
  - `DefaultAction` set to `Jenya.DroneWarehouseMod_Interact`
  - Left tiles (`X:0`) open the warehouse chest
  - Right tiles (`X:1`) open the drone management console
- Added `HandleBuildingAction`, `HandleChestAction`, `HandleConsoleAction` methods with fallback nearest-warehouse lookup
- Registered tile actions via `GameLocation.RegisterTileAction` in a new `OnGameLaunched` handler

## Refactored Selection Input Handling

- Extracted selection mode actions (cycle size, undo, start, cancel) into a shared `HandleOverlayAction` method
- Both keyboard shortcuts and overlay button clicks route through the same action handler, reducing code duplication
- Added `SelectionAction` enum to represent the four selection actions

## Localization

- Added 70+ new English translation keys in `i18n/en.json` covering:
  - Zone selection overlay button labels and hint text
  - All GMCM config section headers, option names, and tooltips

## Documentation

- Added `AGENTS.md` with project overview, build commands, architecture guide, key patterns, and drone work flow documentation for AI-assisted development
