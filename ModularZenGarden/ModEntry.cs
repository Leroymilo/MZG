using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Objects;
using StardewValley.GameData.Shops;
using StardewValley.Buildings;

namespace ModularZenGarden
{

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
			Utils.monitor = Monitor;
			helper.Events.Content.AssetRequested += on_asset_requested;
			helper.Events.World.FurnitureListChanged += on_furniture_list_changed;
			helper.Events.World.BuildingListChanged += on_building_list_changed;
			helper.Events.Player.Warped += on_player_warped;
			helper.Events.GameLoop.GameLaunched += on_game_launched;

			var harmony = new Harmony(ModManifest.UniqueID);
			patch_furniture_draw(harmony);
			patch_furniture_checkForAction(harmony);

			load_assets(helper);
        }

		private void patch_furniture_draw(Harmony harmony)
		{
			// fetching Furniture.draw MethodInfo
			MethodInfo? original_method = typeof(Furniture)
				.GetMethods()
				.Where(x => x.Name == "draw")
				.Where(x => x.DeclaringType != null
					&& x.DeclaringType.Name == "Furniture")
				.FirstOrDefault();

			harmony.Patch(
				original: original_method,
				prefix: new HarmonyMethod(
					typeof(FurniturePatches),
					nameof(FurniturePatches.draw_prefix)
				)
			);
		}

		private void patch_furniture_checkForAction(Harmony harmony)
		{
			// fetching Furniture.checkForAction MethodInfo
			MethodInfo? original_method = typeof(Furniture)
				.GetMethods()
				.Where(x => x.Name == "checkForAction")
				.Where(x => x.DeclaringType != null
					&& x.DeclaringType.Name == "Furniture")
				.FirstOrDefault();
			
			harmony.Patch(
				original: original_method,
				prefix: new HarmonyMethod(
					typeof(FurniturePatches),
					nameof(FurniturePatches.checkForAction_prefix)
				)
			);
		}

		private void load_assets(IModHelper helper)
		{
			SpriteManager.load_sprites(helper);
			GardenType.load_types(helper);
		}


		/// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void on_game_launched(object? sender, GameLaunchedEventArgs e)
		{
			string CP_id = "leroymilo.USJB";
			// check if a mod is loaded
			if (Helper.ModRegistry.IsLoaded(CP_id))
			{
				Utils.apply_to_obelisks = true;
			}
		}

		/// <inheritdoc cref="IContentEvents.AssetRequested"/>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void on_asset_requested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
			{
				// Adding all Zen Garden Furniture
				e.Edit(GardenType.patch_furniture_data);
			}

			if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
			{
				// Adding the Zen Garden catalogue
				e.Edit(GardenType.patch_shop_data);
			}

			if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
			{
				// Adding Sprites to obelisks
				e.Edit(GardenType.patch_building_data);
			}

			if (e.NameWithoutLocale.StartsWith("MZG_F"))
			{
				// Use cached textures to replace loading of Furniture textures
				string type_name = e.NameWithoutLocale.ToString() ?? throw new Exception("should not happen");
				GardenType type = GardenType.get_type(type_name);

				e.LoadFrom(type.get_base, AssetLoadPriority.Medium);
				e.Edit(type.patch_image);
			}
		}

        /// <inheritdoc cref="IWorldEvents.FurnitureListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_furniture_list_changed(object? sender, FurnitureListChangedEventArgs e)
		{
			if (!e.IsCurrentLocation) return;	// ignoring furniture placed outside current location

			foreach (Furniture furniture in e.Added)
			{
				if (!furniture.ItemId.StartsWith("MZG")) continue;
				GardenCache.add_garden(furniture);
			}
			
			foreach (Furniture furniture in e.Removed)
			{
				if (!furniture.ItemId.StartsWith("MZG")) continue;
				GardenCache.remove_garden(furniture);
			}

			GardenCache.update_textures();
		}

        /// <inheritdoc cref="IWorldEvents.BuildingListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_building_list_changed(object? sender, BuildingListChangedEventArgs e)
		{
			// This is probably useless since you edit buildings from shops,
			// but I leave it just in case for mods allowing to edit buildings like furniture

			if (!e.IsCurrentLocation) return;	// ignoring furniture placed outside current location

			foreach (Building building in e.Added)
			{
				if (!GardenType.is_garden(building)) continue;
				GardenCache.add_garden(building);
			}
			
			foreach (Building building in e.Removed)
			{
				if (!GardenType.is_garden(building)) continue;
				GardenCache.remove_garden(building);
			}

			GardenCache.update_textures();
		}

		/// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_player_warped(object? sender, WarpedEventArgs e)
		{
			if (!e.IsLocalPlayer) return;	// for forward compatibility

			GardenCache.clear();

			foreach (Furniture furniture in e.NewLocation.furniture) {
				if (!GardenType.is_garden(furniture)) continue;
				GardenCache.add_garden(furniture);
			}

			foreach (Building building in e.NewLocation.buildings) {
				if (!GardenType.is_garden(building)) continue;
				GardenCache.add_garden(building);
			}
			
			GardenCache.update_textures();
		}
    }
	
}