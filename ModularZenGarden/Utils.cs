using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class Utils
	{
		public static readonly string[] seasons = {"spring", "summer", "fall", "winter"};
		public static IMonitor? monitor;
		public static readonly Dictionary<string, Texture2D> border_textures = new();
		// keys are file names without extension

		public static Vector2 get_pos(Furniture furniture)
		{
			return new Vector2(furniture.boundingBox.X/64, furniture.boundingBox.Y/64);
		}
	}

}