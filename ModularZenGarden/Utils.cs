using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class Utils
	{
		public static readonly string[] seasons = {"spring", "summer", "fall", "winter"};
		public static IMonitor? monitor;

		public static Point get_pos(Furniture furniture)
		{
			return new Point(furniture.boundingBox.X/64, furniture.boundingBox.Y/64);
		}

		public static void log(string message, LogLevel log_level = LogLevel.Trace)
		{
			if (monitor == null)
				throw new NullReferenceException("Monitor was not set.");
			
			monitor.Log(message, log_level);
		}
	}

}