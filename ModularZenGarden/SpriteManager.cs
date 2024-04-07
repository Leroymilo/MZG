using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace ModularZenGarden {

	class SpriteManager
	{
		public static readonly string[] border_names_NxN = {
			"top_left",		"top",		"top_right",
			"left",						"right",
			"bottom_left",	"bottom",	"bottom_right"
		};
		public static readonly string[] border_names_1x1 = {
					"top",
			"left",			"right",
					"bottom"
		};
		public static readonly Vector2 tile_size = new(16, 32); 

		static readonly Dictionary<Vector2, Texture2D> default_bases = new();

		static readonly Dictionary<string, List<Texture2D>> borders_NxN = new();
		static readonly Dictionary<string, List<Texture2D>> borders_1x1 = new();
		static readonly Dictionary<Vector2, Texture2D> default_borders = new();

		static readonly Dictionary<Vector2, Texture2D> default_features = new();

		private static Texture2D extract_tile(Texture2D from, int tile_x, int tile_y)
		{
			Texture2D texture = new(GameRunner.instance.GraphicsDevice, 16, 32);

			int data_size = (int)tile_size.X * (int)tile_size.Y;

			Color[] data = new Color[data_size];
			from.GetData(
				0,
				new Rectangle(
					(int)tile_size.X * tile_x,
					(int)tile_size.Y * tile_y,
					(int)tile_size.X,
					(int)tile_size.Y
				),
				data, 0, data_size
			);

			texture.SetData(
				0,
				new Rectangle(
					0, 0,
					(int)tile_size.X,
					(int)tile_size.Y
				),
				data, 0, data_size
			);

			return texture;
		}

		public static void load_borders(IModHelper helper)
		{
			// NxN
			Texture2D spritesheet_NxN = helper.ModContent.Load<Texture2D>("assets/borders/NxN.png");
			Vector2 nb_sprites = new(spritesheet_NxN.Width/16, spritesheet_NxN.Height/32);

			for (int y = 0; y < nb_sprites.Y; y++)
			{
				string name = border_names_NxN[y];
				borders_NxN[name] = new();

				for (int x = 0; x < nb_sprites.X; x++)
				{
					borders_NxN[name].Add(
						extract_tile(spritesheet_NxN, x, y)
					);
				}
			}

			// 1x1
			Texture2D spritesheet_1x1 = helper.ModContent.Load<Texture2D>("assets/borders/1x1.png");
			nb_sprites = new(spritesheet_1x1.Width/16, spritesheet_1x1.Height/32);

			for (int y = 0; y < nb_sprites.Y; y++)
			{
				string name = border_names_1x1[y];
				borders_1x1[name] = new();

				for (int x = 0; x < nb_sprites.X; x++)
				{
					borders_1x1[name].Add(
						extract_tile(spritesheet_1x1, x, y)
					);
				}
			}
		}

		public static Texture2D get_border(Vector2 size, string type, int value)
		{
			if (size.X > 1 && size.Y > 1)
				return borders_NxN[type][value];
			
			if (size.X == 1 && size.Y == 1)
				return borders_1x1[type][value];
			
			throw new Exception("Unsupported Garden Size.");
		}
	}
}