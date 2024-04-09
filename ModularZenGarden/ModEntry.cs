using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Objects;

namespace ModularZenGarden
{

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
			Utils.monitor = Monitor;
			helper.Events.Content.AssetRequested += on_asset_requested;
			helper.Events.World.FurnitureListChanged += on_furniture_list_changed;
			helper.Events.Player.Warped += on_player_warped;

			patch_furniture_draw();

			load_assets(helper);
        }

		private void patch_furniture_draw()
		{
			var harmony = new Harmony(ModManifest.UniqueID);

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

		private void load_assets(IModHelper helper)
		{
			SpriteManager.load_sprites(helper);

			Dictionary<string, Dictionary<string, object>> types_data =
				helper.ModContent.Load<Dictionary<string, Dictionary<string, object>>>("assets/types.json");
			
			GardenType.make_blank_types(helper);
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


		/// <inheritdoc cref="IContentEvents.AssetRequested"/>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void on_asset_requested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
			{
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

			if (e.NameWithoutLocale.StartsWith("MZG"))
			{
				string type_name = e.NameWithoutLocale.ToString() ?? throw new Exception("should not happen");
				type_name = GardenType.get_type_name(type_name);
				GardenType type = GardenType.types[type_name];

				e.LoadFrom(type.get_base, AssetLoadPriority.Medium);
				// e.LoadFromModFile<Texture2D>($"assets/default_{type.dim}", AssetLoadPriority.Medium);
				e.Edit(asset =>
				{
					type.patch_image(asset.AsImage());
				});
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