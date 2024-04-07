using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ModularZenGarden {

	internal class FurniturePatches {

		static private SpriteBatch? sprite_batch;
		static private Vector2 position;
		static private Rectangle value;
		static private float alpha;
		static private SpriteEffects effects;
		static private float depth;

		// patches need to be static!
		internal static bool draw_prefix(
			Furniture __instance,
			SpriteBatch spriteBatch,
			int x, int y, float alpha = 1f
		)
		{
			if (Utils.monitor == null)
				throw new NullReferenceException("Monitor was not set.");

			try
			{
				// This only applies to Zen Gardens
				if (__instance.ItemId.StartsWith("MZG"))
				{
					sprite_batch = spriteBatch;
					FurniturePatches.alpha = alpha;
					draw_furniture(__instance, x, y);
					return false; // don't run original logic
				}
				return true; // run original logic
			}
			catch (Exception ex)
			{
				Utils.monitor.Log($"Failed in {nameof(draw_prefix)}:\n{ex}", LogLevel.Error);
				return true; // run original logic
			}
		}

		private static void draw_furniture(Furniture __instance, int x, int y)
		{
			if (Utils.monitor == null)
				throw new NullReferenceException("Monitor was not set.");
			
			// idk when this applies, but it's copied from Furniture.draw
			if (__instance.isTemporarilyInvisible)
			{
				return;
			}

			GardenType type = GardenType.get_type(__instance);

			// might not be placed yet
			Garden? garden = GardenCache.get_garden(__instance);

			// extracted and simplified from Furniture.draw :
			value = __instance.sourceRect.Value;
			
			Texture2D base_texture = type.bases[SDate.Now().SeasonKey];
			Texture2D feature_texture = type.features[SDate.Now().SeasonKey];

			effects = __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			depth = (__instance.boundingBox.Value.Bottom - 8) / 10000f;

			// The position is not computed the same way when placing or placed
			if (Furniture.isDrawingLocationFurniture)
			{
				// when the furniture is placed
				position = new(
					__instance.boundingBox.X,
					__instance.boundingBox.Y - (__instance.sourceRect.Height * 4 - __instance.boundingBox.Height)
				);
			}
			else
			{
				// when the furniture follows the cursor
				position = new(
					64*x,
					64*y - (value.Height * 4 - __instance.boundingBox.Height)
				);
			}

			if (__instance.shakeTimer > 0) {
				position += new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
			}
			position = Game1.GlobalToLocal(Game1.viewport, position);

			draw_texture(base_texture);
			depth += 0.01f / 10000f;
			if (garden == null || !Furniture.isDrawingLocationFurniture)
			{
				// draw default border when placing
				draw_texture(Utils.border_textures[$"border_default_3x3"]);
				depth += 0.01f / 10000f;
			}
			else
			{
				// draw computed borders when placed
				foreach (Vector2 tile_pos in garden.type.get_draw_order())
				{
					Texture2D texture = garden.border_textures[tile_pos];

					Rectangle new_value = new(
						0, 0,
						16, 32
					);
					Vector2 offset = new(
						tile_pos.X * 64,
						(tile_pos.Y + type.size.Y-1) * 64
					);
					draw_texture(texture, offset, new_value);
					depth += 0.01f / 10000f;
				}
			}
			draw_texture(feature_texture);
		}

		private static void draw_texture(
			Texture2D texture,
			Vector2? offset = null,
			Rectangle? val = null
		)
		{
			// Wrapper around SpriteBatch.Draw
			// because most of the arguments won't change in one call of Furniture.draw

			if (sprite_batch == null) throw new NullReferenceException("Sprite batch was not set.");

			Vector2 pos = position;
			if (offset != null) pos += (Vector2)offset;
			if (val == null) val = value;

			sprite_batch.Draw(
				texture, pos, val,
				Color.White * alpha,
				0f, Vector2.Zero, 4f,
				effects, depth
			);
		}
	}

}