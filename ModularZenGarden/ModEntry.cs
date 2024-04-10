using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Objects;
using StardewValley.GameData.Shops;
using ContentPatcher;

namespace ModularZenGarden
{
	using Dict = Dictionary<string, object>;

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {

		private ShopData catalogue_shop_data = new();
		private ShopItemData catalogue_item_data = new();


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
			Utils.monitor = Monitor;
			helper.Events.Content.AssetRequested += on_asset_requested;
			helper.Events.World.FurnitureListChanged += on_furniture_list_changed;
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
			
			catalogue_item_data = helper.ModContent.Load<ShopItemData>("assets/catalogue_item.json");
			catalogue_shop_data = helper.ModContent.Load<ShopData>("assets/catalogue_shop.json");

			// Hardcoded Garden Catalogue
			new GardenType("catalogue", new Dict() {
				{"width", 1L},
				{"height", 1L},
				{"use_default_base", true}
			}, helper
			);
			
			// Creating every WxH empty gardens (1 <= W, H <= 3)
			GardenType.make_blank_types(helper);

			// Loading gardens from assets/types.json
			Dictionary<string, Dict> types_data =
				helper.ModContent.Load<Dictionary<string, Dict>>("assets/types.json");
			foreach ((string type_name, var type_data) in types_data)
			{
				try
				{
					new GardenType(
						type_name, type_data, helper
					);
				}
				catch (Exception ex)
				{
					Monitor.Log($"Could not load Garden type {type_name} : {ex}", LogLevel.Warn);
				}
			}
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
				Monitor.Log("Found USJB loaded", LogLevel.Debug);
				
				// get info for a mod
				IModInfo mod = Helper.ModRegistry.Get(CP_id)
					?? throw new NullReferenceException("can't get mod info");
				Monitor.Log("Found USJB info", LogLevel.Debug);
				Monitor.Log($"is content pack? {mod.IsContentPack}", LogLevel.Debug);
				Monitor.Log("Manifest :", LogLevel.Debug);
				IManifest manifest = mod.Manifest; // name, description, version, etc
				Monitor.Log($"\tdescription :{manifest.Description}", LogLevel.Debug);
				Monitor.Log($"\tversion :{manifest.Version}", LogLevel.Debug);

				// TODO : overwrite obelisks
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
				e.Edit(asset =>
				{
					var editor = asset.AsDictionary<string, string>();
					foreach ((string type_name, GardenType type) in GardenType.types)
					{
						string full_name = "MZG " + type_name;
						string data = type.get_string_data(full_name);
						editor.Data[full_name] = data;
					}
				});
			}

			if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
			{
				// Adding the Zen Garden catalogue
				e.Edit(asset =>
				{
					var data = asset.AsDictionary<string, ShopData>().Data;
					// Adding its shop data
					data["MZG_catalogue"] = catalogue_shop_data;
					// Adding the furniture to Robin's shop
					ShopData shop = data["Carpenter"];
					shop.Items.Insert(50, catalogue_item_data);
					// 50 is right after the Furniture Catalogue, it looks nice
				});
			}

			if (e.NameWithoutLocale.StartsWith("MZG"))
			{
				// Use cached textures to replace loading
				string type_name = e.NameWithoutLocale.ToString() ?? throw new Exception("should not happen");
				type_name = GardenType.get_type_name(type_name);
				GardenType type = GardenType.types[type_name];

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

		/// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
		private void on_player_warped(object? sender, WarpedEventArgs e)
		{
			if (!e.IsLocalPlayer) return;	// for forward compatibility

			GardenCache.clear();

			foreach (Furniture furniture in e.NewLocation.furniture) {
				if (!furniture.ItemId.StartsWith("MZG")) continue;
				GardenCache.add_garden(furniture);
			}
			
			GardenCache.update_textures();
		}
    }
	
}