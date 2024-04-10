using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class Utils
	{
		public static readonly string[] seasons = {"spring", "summer", "fall", "winter"};
		public static IMonitor? monitor;
		public static bool apply_to_obelisks = false;

		public static Point get_pos(Furniture furniture)
		{
			return new Point(furniture.boundingBox.X/64, furniture.boundingBox.Y/64);
		}

		public static Point get_pos(Building building)
		{
			return building.GetBoundingBox().Location;
		}

		public static void log(string message, LogLevel log_level = LogLevel.Info)
		{
			if (monitor == null)
				throw new NullReferenceException("Monitor was not set.");
			
			monitor.Log(message, log_level);
		}
	}

}