using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;
using DroneWarehouseMod.Core;
using MD = DroneWarehouseMod.Core.ModDataKeys;
using DroneWarehouseMod.UI;
using DroneWarehouseMod.Game;
using DroneWarehouseMod.Game.Drones;

namespace DroneWarehouseMod
{
    public class ModEntry : Mod
    {
        private ITranslationHelper I18n => this.Helper.Translation;

        // Иконки/текстуры UI
        private Texture2D _harvestIcon = null!;
        private Texture2D _waterIcon = null!;
        private Texture2D _petIcon = null!;
        private Texture2D _farmerIcon = null!;
        private Texture2D? _texFrame, _texButton, _texScreen, _ledGreen, _ledRed;

        private GameDataPatcher? _patcher;

        // Наборы анимаций
        private DroneAnimSet _harvestAnim = new();
        private DroneAnimSet _waterAnim = new();
        private DroneAnimSet _farmerAnim = new();
        private DroneAnimSet _petAnim = new();

        private ModConfig _config = null!;

        // Менеджер / выделение зон
        private DroneManager _manager = null!;
        private Building? _selectionOwner;
        private FarmerQueuesOverlay? _farmerOverlay;
        private ZoneSelectionOverlay? _selectionOverlay;

        private int _deferredRebuildTicks = 0;

        public override void Entry(IModHelper helper)
        {
            // Иконки
            _harvestIcon = helper.ModContent.Load<Texture2D>("assets/ui/harvest_drone/harvest_drone_base.png");
            _waterIcon   = helper.ModContent.Load<Texture2D>("assets/ui/water_drone/water_drone_base.png");
            _petIcon     = helper.ModContent.Load<Texture2D>("assets/ui/pet_drone/pet_drone_base.png");

            // Иконка фермера (fallback — сборщик)
            Texture2D? tmpFarmer = null;
            TryLoad(ref tmpFarmer, "assets/ui/farmer_drone/farmer_drone_base.png");
            _farmerIcon = tmpFarmer ?? _harvestIcon;

            _config = helper.ReadConfig<ModConfig>();

            // Анимации: сборщик
            _harvestAnim = new DroneAnimSet
            {
                FlyEmpty  = LoadFramesSeq("assets/ui/harvest_drone/fly/harvest_drone_base{0}.png", 4, _harvestIcon),
                FlyLoaded = LoadFramesSeq("assets/ui/harvest_drone/fly/harvest_drone_base_crops{0}.png", 4, _harvestIcon),
                Launch    = LoadFramesSeq("assets/ui/harvest_drone/start/harvest_drone_start{0}.png", 5, _harvestIcon),
                Land      = LoadFramesSeq("assets/ui/harvest_drone/land/harvest_drone_landing{0}.png", 5, _harvestIcon),
                WorkEmpty = LoadFramesSeq("assets/ui/harvest_drone/harvest/harvest_drone_harvesting_empty{0}.png", 5, _harvestIcon),
                WorkLoaded= LoadFramesSeq("assets/ui/harvest_drone/harvest/harvest_drone_harvesting_full{0}.png", 6, _harvestIcon),
            };

            // Анимации: поливальщик
            _waterAnim = new DroneAnimSet
            {
                FlyEmpty  = LoadFramesSeq("assets/ui/water_drone/fly/water_drone_base_empty{0}.png", 5, _waterIcon),
                FlyLoaded = LoadFramesSeq("assets/ui/water_drone/fly/water_drone_base_full{0}.png", 5, _waterIcon),
                Launch    = LoadFramesSeq("assets/ui/water_drone/start/water_drone_start{0}.png", 5, _waterIcon),
                Land      = LoadFramesSeq("assets/ui/water_drone/land/water_drone_landing{0}.png", 5, _waterIcon),
                WorkEmpty = LoadFramesSeq("assets/ui/water_drone/water/water_drone_watering{0}.png", 5, _waterIcon),
                WorkLoaded= LoadFramesSeq("assets/ui/water_drone/refill/water_drone_refill{0}.png", 8, _waterIcon),
            };

            // Анимации: «гладильщик»
            _petAnim = new DroneAnimSet
            {
                FlyLoaded = LoadFramesSeq("assets/ui/pet_drone/fly/pet_drone_happy{0}.png", 5, _petIcon),
                FlyEmpty  = LoadFramesSeq("assets/ui/pet_drone/fly/pet_drone_sad{0}.png", 5, _petIcon),
                Launch    = LoadFramesSeq("assets/ui/pet_drone/start/pet_drone_start{0}.png", 5, _petIcon),
                Land      = LoadFramesSeq("assets/ui/pet_drone/land/pet_drone_landing{0}.png", 5, _petIcon),
                WorkPetSmall = LoadFramesSeq("assets/ui/pet_drone/pet/pet_drone_pet_small{0}.png", 5, _petIcon),
                WorkPetBig   = LoadFramesSeq("assets/ui/pet_drone/pet/pet_drone_pet_big{0}.png", 5, _petIcon),
                Refill       = LoadFramesSeq("assets/ui/pet_drone/refill/pet_drone_refill{0}.png", 5, _petIcon),
            };

            // Анимации: фермер
            _farmerAnim = new DroneAnimSet
            {
                FlyEmpty  = LoadFramesSeq("assets/ui/farmer_drone/fly/farmer_drone_dry{0}.png", 6, _farmerIcon),
                FlyLoaded = LoadFramesSeq("assets/ui/farmer_drone/fly/farmer_drone_dry{0}.png", 6, _farmerIcon),
                Launch    = LoadFramesSeq("assets/ui/farmer_drone/start/farmer_drone_start{0}.png", 5, _farmerIcon),
                Land      = LoadFramesSeq("assets/ui/farmer_drone/land/farmer_drone_landing{0}.png", 5, _farmerIcon),
                FarmerWork = LoadFramesSeq("assets/ui/farmer_drone/rip_and_tear/farmer_drone_rip_and_tear{0}.png", 17, _farmerIcon),
                FarmerFail = LoadFramesSeq("assets/ui/farmer_drone/fail/farmer_drone_fail{0}.png", 9, _farmerIcon),
                FarmerClear= LoadFramesSeq("assets/ui/farmer_drone/destroy/farmer_drone_destroy{0}.png", 7, _farmerIcon),
            };

            // Консольные текстуры
            TryLoad(ref _texFrame,  "assets/ui/console/console_frame_60.png");
            TryLoad(ref _texButton, "assets/ui/console/console_button_60.png");
            TryLoad(ref _texScreen, "assets/ui/console/console_screen_16.png");
            TryLoad(ref _ledGreen,  "assets/ui/console/led_green_8.png");
            TryLoad(ref _ledRed,    "assets/ui/console/led_red_8.png");

            // События
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.Display.RenderedHud   += this.OnRenderedHud;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.World.BuildingListChanged += this.OnBuildingListChanged;

            // Локальные загрузчики кадров/текстур
            Texture2D[] LoadFramesSeq(string pattern, int count, Texture2D fallback)
            {
                var list = new List<Texture2D>(count);
                for (int i = 1; i <= count; i++)
                {
                    string path = string.Format(pattern, i);
                    try { list.Add(helper.ModContent.Load<Texture2D>(path)); }
                    catch { Monitor.Log($"[Drone] Не найден кадр '{path}' — пропускаю.", LogLevel.Trace); }
                }
                if (list.Count == 0) list.Add(fallback);
                return list.ToArray();
            }
            void TryLoad(ref Texture2D? slot, string path)
            {
                try { slot = helper.ModContent.Load<Texture2D>(path); }
                catch { slot = null; Monitor.Log($"[UI] Не найден ассет {path} — будет ваниль.", LogLevel.Trace); }
            }

            _patcher = new GameDataPatcher(this.Helper, this.Monitor, () => _manager?.WarehouseLidOpen == true);
            _patcher.Hook();

            Core.Audio.Init(this.Helper, this.Monitor, _config);
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Register custom tile actions for building interaction
            GameLocation.RegisterTileAction("Jenya.DroneWarehouseMod_Interact", this.HandleBuildingAction);
            GameLocation.RegisterTileAction("Jenya.DroneWarehouseMod_ChestTile", this.HandleChestAction);
            GameLocation.RegisterTileAction("Jenya.DroneWarehouseMod_ConsoleTile", this.HandleConsoleAction);

            // Register Generic Mod Config Menu integration
            RegisterGmcm();
        }

        private void RegisterGmcm()
        {
            var gmcm = this.Helper.ModRegistry.GetApi<Core.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
                return;

            gmcm.Register(
                mod: this.ModManifest,
                reset: () => _config = new ModConfig(),
                save: () => this.Helper.WriteConfig(_config)
            );

            // General
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.general"));

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.WorkOffFarm,
                setValue: v => _config.WorkOffFarm = v,
                name: () => I18n.Get("config.workOffFarm.name"),
                tooltip: () => I18n.Get("config.workOffFarm.tooltip")
            );

            // Rendering
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.rendering"));

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.DrawUnderTrees,
                setValue: v => _config.DrawUnderTrees = v,
                name: () => I18n.Get("config.drawUnderTrees.name"),
                tooltip: () => I18n.Get("config.drawUnderTrees.tooltip")
            );

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.DrawInFrontNearHatch,
                setValue: v => _config.DrawInFrontNearHatch = v,
                name: () => I18n.Get("config.drawInFrontNearHatch.name"),
                tooltip: () => I18n.Get("config.drawInFrontNearHatch.tooltip")
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.NearHatchRadius,
                setValue: v => _config.NearHatchRadius = v,
                name: () => I18n.Get("config.nearHatchRadius.name"),
                tooltip: () => I18n.Get("config.nearHatchRadius.tooltip"),
                min: 50f,
                max: 300f,
                interval: 10f
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.HatchXOffset,
                setValue: v => _config.HatchXOffset = v,
                name: () => I18n.Get("config.hatchXOffset.name"),
                tooltip: () => I18n.Get("config.hatchXOffset.tooltip"),
                min: -100f,
                max: 100f,
                interval: 5f
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.HatchYOffset,
                setValue: v => _config.HatchYOffset = v,
                name: () => I18n.Get("config.hatchYOffset.name"),
                tooltip: () => I18n.Get("config.hatchYOffset.tooltip"),
                min: -150f,
                max: 50f,
                interval: 5f
            );

            // Harvester Options
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.harvester"));

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.HarvesterSkipFlowerCrops,
                setValue: v => _config.HarvesterSkipFlowerCrops = v,
                name: () => I18n.Get("config.harvesterSkipFlowerCrops.name"),
                tooltip: () => I18n.Get("config.harvesterSkipFlowerCrops.tooltip")
            );

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.HarvesterSkipFruitTrees,
                setValue: v => _config.HarvesterSkipFruitTrees = v,
                name: () => I18n.Get("config.harvesterSkipFruitTrees.name"),
                tooltip: () => I18n.Get("config.harvesterSkipFruitTrees.tooltip")
            );

            // Waterer Options
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.waterer"));

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.AllowRefillAtHatchIfNoWater,
                setValue: v => _config.AllowRefillAtHatchIfNoWater = v,
                name: () => I18n.Get("config.allowRefillAtHatch.name"),
                tooltip: () => I18n.Get("config.allowRefillAtHatch.tooltip")
            );

            // Pathing
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.pathing"));

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.ScanIntervalTicks,
                setValue: v => _config.ScanIntervalTicks = v,
                name: () => I18n.Get("config.scanIntervalTicks.name"),
                tooltip: () => I18n.Get("config.scanIntervalTicks.tooltip"),
                min: 1,
                max: 30,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.NoFlyPadTiles,
                setValue: v => _config.NoFlyPadTiles = v,
                name: () => I18n.Get("config.noFlyPadTiles.name"),
                tooltip: () => I18n.Get("config.noFlyPadTiles.tooltip"),
                min: 0,
                max: 5,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.LineOfSightPadPx,
                setValue: v => _config.LineOfSightPadPx = v,
                name: () => I18n.Get("config.lineOfSightPadPx.name"),
                tooltip: () => I18n.Get("config.lineOfSightPadPx.tooltip"),
                min: 0,
                max: 50,
                interval: 2
            );

            // Capacities
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.capacities"));

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.HarvestCapacity,
                setValue: v => _config.HarvestCapacity = v,
                name: () => I18n.Get("config.harvestCapacity.name"),
                tooltip: () => I18n.Get("config.harvestCapacity.tooltip"),
                min: 1,
                max: 50,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.WaterMaxCharges,
                setValue: v => _config.WaterMaxCharges = v,
                name: () => I18n.Get("config.waterMaxCharges.name"),
                tooltip: () => I18n.Get("config.waterMaxCharges.tooltip"),
                min: 1,
                max: 50,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.PetMaxCharges,
                setValue: v => _config.PetMaxCharges = v,
                name: () => I18n.Get("config.petMaxCharges.name"),
                tooltip: () => I18n.Get("config.petMaxCharges.tooltip"),
                min: 1,
                max: 50,
                interval: 1
            );

            // Speeds
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.speeds"));

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.HarvestSpeed,
                setValue: v => _config.HarvestSpeed = v,
                name: () => I18n.Get("config.harvestSpeed.name"),
                tooltip: () => I18n.Get("config.harvestSpeed.tooltip"),
                min: 0.5f,
                max: 10f,
                interval: 0.1f
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.WaterSpeed,
                setValue: v => _config.WaterSpeed = v,
                name: () => I18n.Get("config.waterSpeed.name"),
                tooltip: () => I18n.Get("config.waterSpeed.tooltip"),
                min: 0.5f,
                max: 10f,
                interval: 0.1f
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.PetSpeed,
                setValue: v => _config.PetSpeed = v,
                name: () => I18n.Get("config.petSpeed.name"),
                tooltip: () => I18n.Get("config.petSpeed.tooltip"),
                min: 0.5f,
                max: 10f,
                interval: 0.1f
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.FarmerSpeed,
                setValue: v => _config.FarmerSpeed = v,
                name: () => I18n.Get("config.farmerSpeed.name"),
                tooltip: () => I18n.Get("config.farmerSpeed.tooltip"),
                min: 0.5f,
                max: 10f,
                interval: 0.1f
            );

            // Farmer Timings
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.farmerTimings"));

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.FarmerWorkSeconds,
                setValue: v => _config.FarmerWorkSeconds = v,
                name: () => I18n.Get("config.farmerWorkSeconds.name"),
                tooltip: () => I18n.Get("config.farmerWorkSeconds.tooltip"),
                min: 1,
                max: 10,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.FarmerClearSeconds,
                setValue: v => _config.FarmerClearSeconds = v,
                name: () => I18n.Get("config.farmerClearSeconds.name"),
                tooltip: () => I18n.Get("config.farmerClearSeconds.tooltip"),
                min: 1,
                max: 10,
                interval: 1
            );

            // Audio
            gmcm.AddSectionTitle(this.ModManifest, () => I18n.Get("config.section.audio"));

            gmcm.AddBoolOption(
                mod: this.ModManifest,
                getValue: () => _config.EnableCustomSfx,
                setValue: v => _config.EnableCustomSfx = v,
                name: () => I18n.Get("config.enableCustomSfx.name"),
                tooltip: () => I18n.Get("config.enableCustomSfx.tooltip")
            );

            gmcm.AddNumberOption(
                mod: this.ModManifest,
                getValue: () => _config.CustomSfxVolume,
                setValue: v => _config.CustomSfxVolume = v,
                name: () => I18n.Get("config.customSfxVolume.name"),
                tooltip: () => I18n.Get("config.customSfxVolume.tooltip"),
                min: 0f,
                max: 1f,
                interval: 0.05f
            );
        }

        private bool HandleBuildingAction(GameLocation location, string[] args, Farmer player, Point tile)
        {
            if (_manager == null) return false;
            if (location is not Farm farm) return false;
            
            Building? warehouse = farm.getBuildingAt(new Vector2(tile.X, tile.Y));
            if (warehouse == null || warehouse.buildingType?.Value != "DroneWarehouse")
            {
                // Fallback: find nearest warehouse
                warehouse = FindNearestWarehouse(farm, tile);
            }
            if (warehouse == null) return false;

            // Determine which half was clicked
            int rightX = warehouse.tileX.Value + warehouse.tilesWide.Value - 1;
            bool rightHalf = tile.X == rightX;

            if (rightHalf)
                OpenWarehouseConsoleMenu(warehouse);
            else
                OpenWarehouseChestMenu(warehouse);

            return true;
        }

        private bool HandleChestAction(GameLocation location, string[] args, Farmer player, Point tile)
        {
            if (_manager == null) return false;
            if (location is not Farm farm) return false;
            
            Building? warehouse = farm.getBuildingAt(new Vector2(tile.X, tile.Y));
            if (warehouse == null || warehouse.buildingType?.Value != "DroneWarehouse")
                warehouse = FindNearestWarehouse(farm, tile);
            if (warehouse == null) return false;

            OpenWarehouseChestMenu(warehouse);
            return true;
        }

        private bool HandleConsoleAction(GameLocation location, string[] args, Farmer player, Point tile)
        {
            if (_manager == null) return false;
            if (location is not Farm farm) return false;
            
            Building? warehouse = farm.getBuildingAt(new Vector2(tile.X, tile.Y));
            if (warehouse == null || warehouse.buildingType?.Value != "DroneWarehouse")
                warehouse = FindNearestWarehouse(farm, tile);
            if (warehouse == null) return false;

            OpenWarehouseConsoleMenu(warehouse);
            return true;
        }

        private Building? FindNearestWarehouse(Farm farm, Point tile)
        {
            Building? nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var b in farm.buildings)
            {
                if (b?.buildingType?.Value != "DroneWarehouse") continue;

                // Check if tile is within or adjacent to building
                int bx = b.tileX.Value;
                int by = b.tileY.Value;
                int bw = b.tilesWide.Value;
                int bh = b.tilesHigh.Value;

                // Within building bounds or 1 tile adjacent
                if (tile.X >= bx - 1 && tile.X <= bx + bw &&
                    tile.Y >= by - 1 && tile.Y <= by + bh)
                {
                    float cx = bx + bw / 2f;
                    float cy = by + bh / 2f;
                    float dist = MathF.Sqrt((tile.X - cx) * (tile.X - cx) + (tile.Y - cy) * (tile.Y - cy));
                    if (dist < nearestDist)
                    {
                        nearest = b;
                        nearestDist = dist;
                    }
                }
            }

            return nearest;
        }

        private void OpenWarehouseConsoleMenu(Building warehouse)
        {
            Game1.activeClickableMenu = new DroneConsoleMenu(
                this.Helper, this.Monitor, _manager, warehouse,
                _harvestIcon, _waterIcon, _petIcon, _farmerIcon,
                _texFrame, _texButton, _texScreen, _ledGreen, _ledRed
            );
        }

        // Открыть сундук склада
        private void OpenWarehouseChestMenu(Building b)
        {
            Chest chest = _manager.GetChestFor(b);
            chest.playerChest.Value = true;

            Game1.activeClickableMenu = new ItemGrabMenu(
                inventory: chest.Items,
                reverseGrab: false,
                showReceivingMenu: true,
                highlightFunction: null,
                behaviorOnItemSelectFunction: chest.grabItemFromInventory,
                message: I18n.Get("menu.chest.title"),
                behaviorOnItemGrab: chest.grabItemFromChest,
                snapToBottom: false,
                canBeExitedWithKey: true,
                playRightClickSound: true,
                allowRightClick: true,
                showOrganizeButton: true,
                source: ItemGrabMenu.source_chest,
                sourceItem: chest,
                context: chest
            );
        }

        private static void PlayFarmOnly(GameLocation loc, string cue) => Audio.PlayFarmOnly(loc, cue);

        private void OnMenuChanged(object? s, MenuChangedEventArgs e)
        {
            // если закрыли наше меню — опустим крышку (менеджер сам синхронизирует)
            bool wasOurMenu =
                e.OldMenu is ItemGrabMenu
                || e.OldMenu is DroneConsoleMenu;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            DataCache.Refresh();
            _manager = new DroneManager(_config, this.Monitor, this.Helper,
                _harvestAnim, _waterAnim, _petAnim, _farmerAnim, _harvestIcon);
            _selectionOverlay = new ZoneSelectionOverlay(this.Helper, _manager);
            _manager.SyncWithBuildings();
            if (Game1.getFarm() is Farm farm)
            {
                _manager.RebuildDryList(farm);
                _manager.RebuildNoFly(farm);
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            
            _farmerOverlay?.Draw(e.SpriteBatch);
            
            // Draw selection overlay buttons (for Android/touch users)
            if (_manager?.IsSelectionActive == true)
            {
                _selectionOverlay?.Draw(e.SpriteBatch);
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            DataCache.Refresh();
            _manager?.SyncWithBuildings();

            _manager?.OnNewDay();
            _manager?.ResetVisualProxies();

            _deferredRebuildTicks = 20;

            _manager?.CancelBeaconSelection();
            _selectionOwner = null;
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (Game1.getFarm() is Farm farm)
            {
                _manager?.RebuildDryList(farm);
                if (e.NewTime % 100 == 0)
                {
                    _manager?.RefreshPetReservations(farm);
                    _manager?.TrimAllFarmerQueues(farm);   // ← новое: почистить завершённые пункты
                }
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            if (Game1.getFarm() is Farm farm)
            {
                _manager?.PersistChests(farm);
                _manager?.PersistFarmerJobs(farm);
            }
        }

        private void OnBuildingListChanged(object? sender, BuildingListChangedEventArgs e)
        {
            if (e.Location is not Farm farm) return;

            foreach (var removed in e.Removed)
                if (removed.buildingType?.Value == "DroneWarehouse")
                    _manager?.CleanupWarehouseOnRemoved(farm, removed);

            _manager?.SyncWithBuildings();
            _manager?.RebuildNoFly(farm);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (_deferredRebuildTicks > 0)
            {
                _deferredRebuildTicks--;
                if (_deferredRebuildTicks == 0 && Game1.getFarm() is Farm f)
                {
                    _manager?.RebuildDryList(f);
                    _manager?.RebuildNoFly(f);
                }
            }

            if (_manager?.IsSelectionActive == true && Game1.currentLocation is not Farm)
            {
                _manager.CancelBeaconSelection();
                _selectionOwner = null;
            }

            if (Game1.activeClickableMenu != null || Game1.paused || Game1.eventUp || !Context.IsPlayerFree) return;
            if (!Game1.game1.IsActive) return;

            if (Game1.currentLocation is Farm farm)
                _manager?.Update(farm, e);

            if (_config.WorkOffFarm && Game1.currentLocation is not Farm)
                _manager?.Update(Game1.getFarm(), e);
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.currentLocation is Farm)
                _manager?.Draw(e.SpriteBatch);

            // Оверлей выделения зон
            if (_manager?.IsSelectionActive == true && Game1.currentLocation is Farm farm)
            {
                var cursor = this.Helper.Input.GetCursorPosition().Tile;
                var hover = new Point((int)cursor.X, (int)cursor.Y);
                int size = _manager.SelectionSize;

                void FillTile(Point t, Color c)
                {
                    Vector2 topLeft = Game1.GlobalToLocal(Game1.viewport,
                        new Vector2(t.X * Game1.tileSize, t.Y * Game1.tileSize));
                    e.SpriteBatch.Draw(Game1.staminaRect,
                        new Rectangle((int)topLeft.X, (int)topLeft.Y, Game1.tileSize, Game1.tileSize),
                        c);
                }

                int r = size / 2;
                for (int y = hover.Y - r; y <= hover.Y + r; y++)
                    for (int x = hover.X - r; x <= hover.X + r; x++)
                        FillTile(new Point(x, y), new Color(0, 255, 120, 70));

                if (_manager.SelectionBuilding is Building sb)
                {
                    foreach (var (center, s) in _manager.GetVirtualBeaconsSnapshot(sb))
                    {
                        int rr = s / 2;
                        for (int y = center.Y - rr; y <= center.Y + rr; y++)
                            for (int x = center.X - rr; x <= center.X + rr; x++)
                                FillTile(new Point(x, y), new Color(0, 255, 120, 20));
                    }
                }
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // NEW: F3 — оверлей очередей фермеров
            if (e.Button == SButton.F3)
            {
                if (_farmerOverlay != null)
                {
                    _farmerOverlay = null; // выключить
                    Game1.playSound("bigDeSelect");
                }
                else
                {
                    var farmF3 = Game1.getFarm();
                    var wh = PickWarehouseForOverlay(farmF3);
                    if (wh == null)
                    {
                        farmF3?.localSound("cancel");
                        Game1.addHUDMessage(new HUDMessage(I18n.Get("hud.noWarehouse"), HUDMessage.error_type));
                    }
                    else
                    {
                        int cnt = 0;
                        if (wh.modData.TryGetValue(MD.FarmerCount, out var s)) int.TryParse(s, out cnt);
                        else if (wh.modData.TryGetValue(MD.HasFarmer, out var hf) && hf == "1") cnt = 1;
                        cnt = Math.Clamp(cnt, 0, 3);
                        if (cnt <= 0)
                        {
                            farmF3?.localSound("cancel");
                            Game1.addHUDMessage(new HUDMessage(I18n.Get("hud.noFarmer"), HUDMessage.error_type));
                        }
                        else
                        {
                            _farmerOverlay = new FarmerQueuesOverlay(this.Helper, this.Monitor, _manager, wh);
                            Game1.playSound("smallSelect");
                        }
                    }
                }
                this.Helper.Input.Suppress(e.Button);
                return;
            }

            // Глобальный хоткей выбора зон
            if (e.Button == SButton.F6 && Game1.currentLocation is Farm farmF6)
            {
                HandleGlobalHotkeyF6(farmF6, e);
                return;
            }

            // Управление режимом выделения
            if (_manager?.IsSelectionActive == true && Game1.currentLocation is Farm farmSel)
            {
                if (HandleSelectionModeInput(farmSel, e)) return;
            }

            // Клик по зданию: правая половина — консоль, левая — сундук
            if (!e.Button.IsActionButton()) return;
            if (Game1.currentLocation is not Farm farm) return;

            HandleWarehouseInteraction(farm, Game1.player.GetGrabTile(), e);
        }

        private Building? PickWarehouseForOverlay(Farm farm)
        {
            static bool IsWh(Building? b) => b != null && b.buildingType?.Value == "DroneWarehouse";
            var underGrab = farm.getBuildingAt(Game1.player.GetGrabTile());
            bool HasFarmers(Building b)
            {
                if (b.modData.TryGetValue(MD.FarmerCount, out var s) && int.TryParse(s, out var n)) return n > 0;
                return b.modData.TryGetValue(MD.HasFarmer, out var hf) && hf == "1";
            }
            if (IsWh(underGrab) && HasFarmers(underGrab)) return underGrab;

            var list = farm.buildings.Where(IsWh).Where(HasFarmers).ToList();
            if (list.Count == 0) return null;

            Vector2 me = Game1.player.Tile;
            float Dist(Building b)
            {
                float cx = b.tileX.Value + b.tilesWide.Value / 2f;
                float cy = b.tileY.Value + b.tilesHigh.Value / 2f;
                return Vector2.Distance(me, new Vector2(cx, cy));
            }
            return list.OrderBy(Dist).First();
        }

        private void HandleGlobalHotkeyF6(Farm farmF6, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.F6 && Game1.currentLocation is Farm _)
            {
                Building? PickWarehouseForF6(Farm farm)
                {
                    var underGrab = farm.getBuildingAt(Game1.player.GetGrabTile());
                    if (underGrab != null && underGrab.buildingType?.Value == "DroneWarehouse")
                        return underGrab;

                    var list = farm.buildings.Where(b => b?.buildingType?.Value == "DroneWarehouse").ToList();
                    if (list.Count == 0) return null;

                    Vector2 me = Game1.player.Tile;

                    float Dist(Building b)
                    {
                        float cx = b.tileX.Value + b.tilesWide.Value / 2f;
                        float cy = b.tileY.Value + b.tilesHigh.Value / 2f;
                        return Vector2.Distance(me, new Vector2(cx, cy));
                    }

                    var withFarmer = list
                        .Where(b => b.modData.TryGetValue(MD.HasFarmer, out var v) && v == "1")
                        .OrderBy(Dist)
                        .FirstOrDefault();

                    return withFarmer ?? list.OrderBy(Dist).First();
                }

                var wh = PickWarehouseForF6(farmF6);
                if (wh == null)
                {
                    farmF6.localSound("cancel");
                    Game1.addHUDMessage(new HUDMessage(I18n.Get("hud.noWarehouse"), HUDMessage.error_type));
                    this.Helper.Input.Suppress(e.Button);
                    return;
                }

                int farmerCountF6 = 0;
                if (wh.modData.TryGetValue(MD.FarmerCount, out var sCntF6)) int.TryParse(sCntF6, out farmerCountF6);
                else if (wh.modData.TryGetValue(MD.HasFarmer, out var hf) && hf == "1") farmerCountF6 = 1;
                bool hasFarmer = farmerCountF6 > 0;
                if (!hasFarmer)
                {
                    farmF6.localSound("cancel");
                    Game1.addHUDMessage(new HUDMessage(I18n.Get("hud.noFarmer"), HUDMessage.error_type));
                    this.Helper.Input.Suppress(e.Button);
                    return;
                }

                if (_manager.IsSelectionActive && _selectionOwner == wh)
                {
                    _manager.CancelBeaconSelection();
                    _selectionOwner = null;
                    Game1.currentLocation?.localSound("cancel");
                }
                else
                {
                    int startSize = _manager.SelectionSize > 0 ? _manager.SelectionSize : 3;
                    _manager.BeginBeaconSelection(wh, startSize);
                    _selectionOwner = wh;

                    farmF6.localSound("smallSelect");
                    Game1.addHUDMessage(new HUDMessage(I18n.Get("hud.plant.instructions"), HUDMessage.newQuest_type));
                }

                this.Helper.Input.Suppress(e.Button);
                return;
            }
        }

        // Хоткеи режима выделения (Q/ЛКМ/ПКМ/Enter/Esc) + touch button support
        private bool HandleSelectionModeInput(Farm farmSel, ButtonPressedEventArgs e)
        {
            // Check for overlay button clicks first (for Android/touch users)
            if (e.Button == SButton.MouseLeft && _selectionOverlay != null)
            {
                var screenPos = this.Helper.Input.GetCursorPosition().ScreenPixels;
                var action = _selectionOverlay.GetClickedAction((int)screenPos.X, (int)screenPos.Y);
                if (action != null)
                {
                    HandleOverlayAction(action.Value, farmSel);
                    this.Helper.Input.Suppress(e.Button);
                    return true;
                }
            }

            switch (e.Button)
            {
                case SButton.Q:
                    _manager.CycleSelectionSize();
                    this.Helper.Input.Suppress(e.Button);
                    return true;
                case SButton.MouseLeft:
                    var pos = this.Helper.Input.GetCursorPosition().Tile;
                    _manager.TryAddVirtualBeacon(new Point((int)pos.X, (int)pos.Y));
                    Game1.currentLocation?.localSound("coin");
                    this.Helper.Input.Suppress(e.Button);
                    return true;
                case SButton.MouseRight:
                    _manager.RemoveLastVirtualBeacon();
                    Game1.currentLocation?.localSound("cancel");
                    this.Helper.Input.Suppress(e.Button);
                    return true;
                case SButton.Escape:
                    _manager.CancelBeaconSelection();
                    _selectionOwner = null;
                    Game1.currentLocation?.localSound("cancel");
                    this.Helper.Input.Suppress(e.Button);
                    return true;
                case SButton.Enter:
                    HandleOverlayAction(SelectionAction.Start, farmSel);
                    this.Helper.Input.Suppress(e.Button);
                    return true;
            }
            return false;
        }

        private void HandleOverlayAction(SelectionAction action, Farm farm)
        {
            switch (action)
            {
                case SelectionAction.CycleSize:
                    _manager.CycleSelectionSize();
                    break;
                case SelectionAction.Undo:
                    _manager.RemoveLastVirtualBeacon();
                    Game1.currentLocation?.localSound("cancel");
                    break;
                case SelectionAction.Start:
                    if (_manager.SelectionBuilding is Building sb)
                    {
                        string msg;
                        bool ok = _manager.TryStartFarmerFromBeacons(sb, farm, out msg);
                        Game1.playSound(ok ? "smallSelect" : "cancel");
                        if (!string.IsNullOrEmpty(msg))
                            Game1.addHUDMessage(new HUDMessage(msg, ok ? HUDMessage.newQuest_type : HUDMessage.error_type));
                    }
                    _manager.CancelBeaconSelection();
                    _selectionOwner = null;
                    break;
                case SelectionAction.Cancel:
                    _manager.CancelBeaconSelection();
                    _selectionOwner = null;
                    Game1.currentLocation?.localSound("cancel");
                    break;
            }
        }

        private void HandleWarehouseInteraction(Farm farm, Vector2 grabTile, ButtonPressedEventArgs e)
        {
            Building? warehouse = TryResolveWarehouseForInteraction(farm, grabTile);
            if (warehouse == null)
                return;

            int leftX = warehouse.tileX.Value;
            int rightX = warehouse.tileX.Value + warehouse.tilesWide.Value - 1;
            int topY = warehouse.tileY.Value;
            int botY = warehouse.tileY.Value + warehouse.tilesHigh.Value - 1;

            int gx = Math.Clamp((int)grabTile.X, leftX, rightX);
            int gy = Math.Clamp((int)grabTile.Y, topY, botY);
            bool rightHalf = (gx == rightX);

            if (rightHalf)
            {
                Game1.activeClickableMenu = new DroneConsoleMenu(
                    this.Helper, this.Monitor, _manager, warehouse,
                    _harvestIcon, _waterIcon, _petIcon, _farmerIcon,
                    _texFrame, _texButton, _texScreen, _ledGreen, _ledRed
                );
            }
            else
            {
                OpenWarehouseChestMenu(warehouse);
            }
            this.Helper.Input.Suppress(e.Button);
        }
        
        private Building? TryResolveWarehouseForInteraction(Farm farm, Vector2 grabTile)
        {
            static bool IsWh(Building? b) => b != null && b.buildingType?.Value == "DroneWarehouse";

            // 1) Точный тайл (мышь)
            var b = farm.getBuildingAt(grabTile);
            if (IsWh(b)) return b;

            // Вектор шага по взгляду
            Vector2 me = Game1.player.Tile;
            int dx = Math.Sign((int)grabTile.X - (int)me.X);
            int dy = Math.Sign((int)grabTile.Y - (int)me.Y);

            // 2) +1..+2 тайла вперёд (контроллер)
            Vector2 t = grabTile;
            for (int i = 0; i < 2; i++)
            {
                t += new Vector2(dx, dy);
                b = farm.getBuildingAt(t);
                if (IsWh(b)) return b;
            }

            // 3) Фронтальная полоса: тайл ровно перед фасадом
            Building? bestFront = null;
            float bestFrontDist = float.MaxValue;
            foreach (var cand in farm.buildings)
            {
                if (!IsWh(cand)) continue;

                int leftX  = cand.tileX.Value;
                int rightX = cand.tileX.Value + cand.tilesWide.Value - 1;
                int frontY = cand.tileY.Value + cand.tilesHigh.Value; // на 1 тайл ниже низа

                if ((int)grabTile.Y == frontY && (int)grabTile.X >= leftX && (int)grabTile.X <= rightX)
                {
                    float d = Vector2.Distance(Game1.player.Tile, new Vector2(leftX + cand.tilesWide.Value / 2f, frontY));
                    if (d < bestFrontDist) { bestFrontDist = d; bestFront = cand; }
                }
            }
            if (bestFront != null) return bestFront;

            // 4) Короткий рейкаст (2.8 тайла) по направлению взгляда
            if (dx != 0 || dy != 0)
            {
                Vector2 origin = Game1.player.Position + new Vector2(Game1.tileSize / 2f, Game1.tileSize * 0.75f);
                Vector2 target = origin + new Vector2(dx, dy) * (Game1.tileSize * 2.8f);

                Building? best = null;
                float bestSq = float.MaxValue;

                foreach (var cand in farm.buildings)
                {
                    if (!IsWh(cand)) continue;

                    Rectangle rect = new Rectangle(
                        cand.tileX.Value * Game1.tileSize,
                        cand.tileY.Value * Game1.tileSize,
                        cand.tilesWide.Value * Game1.tileSize,
                        cand.tilesHigh.Value * Game1.tileSize
                    );
                    rect.Inflate(8, 8); // небольшой люфт

                    if (SegmentIntersectsRect(rect, origin, target))
                    {
                        float dsq = Vector2.DistanceSquared(origin, new Vector2(rect.Center.X, rect.Center.Y));
                        if (dsq < bestSq) { bestSq = dsq; best = cand; }
                    }
                }
                if (best != null) return best;
            }

            return null;
        }

        // Небольшой локальный помощник (копия логики из Manager, чтобы не менять модификаторы доступа)
        private static bool SegmentIntersectsRect(Rectangle r, Vector2 a, Vector2 b)
        {
            if (r.Contains((int)a.X, (int)a.Y) || r.Contains((int)b.X, (int)b.Y)) return true;

            static bool Intersects(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
            {
                float o1 = MathF.Sign((p2.X - p1.X) * (q1.Y - p1.Y) - (p2.Y - p1.Y) * (q1.X - p1.X));
                float o2 = MathF.Sign((p2.X - p1.X) * (q2.Y - p1.Y) - (p2.Y - p1.Y) * (q2.X - p1.X));
                float o3 = MathF.Sign((q2.X - q1.X) * (p1.Y - q1.Y) - (q2.Y - q1.Y) * (p1.X - q1.X));
                float o4 = MathF.Sign((q2.X - q1.X) * (p2.Y - q1.Y) - (q2.Y - q1.Y) * (p2.X - q1.X));
                return o1 != o2 && o3 != o4;
            }

            Vector2 a1 = new(r.Left, r.Top), a2 = new(r.Right, r.Top);
            Vector2 b1 = new(r.Right, r.Top), b2 = new(r.Right, r.Bottom);
            Vector2 c1 = new(r.Right, r.Bottom), c2 = new(r.Left, r.Bottom);
            Vector2 d1 = new(r.Left, r.Bottom), d2 = new(r.Left, r.Top);

            return Intersects(a, b, a1, a2) || Intersects(a, b, b1, b2)
                || Intersects(a, b, c1, c2) || Intersects(a, b, d1, d2);
        }
    }
}
