using System.Net;
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

		public static readonly Vector2 tile_size = new(16, 32); // SHOULD NOT CHANGE

		private static Texture2D? base_3x3;
		private static Texture2D? base_3x3_winter;
		static readonly Dictionary<Vector2, Dictionary<string, Texture2D>> default_bases = new();

		static readonly Dictionary<string, List<Texture2D>> borders_NxN = new();
		static readonly Dictionary<string, List<Texture2D>> borders_1x1 = new();
		static readonly Dictionary<Vector2, List<Texture2D>> default_borders = new();

		static readonly Dictionary<Vector2, Texture2D> default_features = new();

		public static void load_sprites(IModHelper helper)
		{
			base_3x3 = helper.ModContent.Load<Texture2D>("assets/default_base_3x3");
			base_3x3_winter = helper.ModContent.Load<Texture2D>("assets/default_base_3x3_winter");

			load_borders(helper);
		}

		private static Color[] get_data(Texture2D from, int w, int h, Vector2? origin = null)
		{
			if (origin == null)
				origin = new(0, 0);
			
			int x = (int)origin.Value.X, y = (int)origin.Value.Y;
			
			Color[] data = new Color[w*h];
			from.GetData(
				0,
				new Rectangle(x, y, w, h),
				data, 0, w*h
			);
			return data;

		}

		private static void set_data(Texture2D to, Vector2 origin, int w, int h, Color[] data)
		{
			to.SetData(
				0,
				new Rectangle(
					(int)origin.X, (int)origin.Y,
					w, h
				),
				data, 0, w*h
			);
		}

		public static Texture2D get_default_base(Vector2 size, string season)
		{
			if (season != "winter")
				season = "other";

			if (default_bases.ContainsKey(size))
			{
				if (default_bases[size].ContainsKey(season))
					return default_bases[size][season];
			}
			else
				default_bases[size] = new();

			Texture2D? from_;
			if (season == "winter")
				from_ = base_3x3_winter;
			else
			{
				from_ = base_3x3;
				season = "other";
			}
			if (from_ == null) throw new NullReferenceException("Textures not loaded");
				
			Texture2D texture = new(
				GameRunner.instance.GraphicsDevice,
				(int)(tile_size.X * size.X),
				(int)(tile_size.Y * size.Y)
			);

			Vector2 true_origin = new(0, size.Y * (tile_size.Y - tile_size.X) - 2);
			Vector2 pixel_origin = true_origin;
			Vector2 true_end = new(tile_size.X * size.X, tile_size.Y * size.Y - 2);
			Color[] data;

			// Case : new texture is wider than original texture
			// Repeating original texture in width
			int height = (int)Math.Min(true_end.Y - true_origin.Y, from_.Height);

			if (true_end.X - pixel_origin.X > from_.Width)
			{	
				// Getting data from original texture
				data = get_data(from_, from_.Width, height);

				while (true_end.X - pixel_origin.X > from_.Width)
				{
					// Copying data to new texture until width is filled
					set_data(texture, pixel_origin, from_.Width, height, data);
					pixel_origin.X += from_.Width;
				}
			}

			// Completing width
			int width = (int)true_end.X - (int)pixel_origin.X;
			// Getting data from original texture
			data = get_data(from_, width, height);

			// Copying data to new texture until width is filled
			set_data(texture, pixel_origin, width, height, data);
			pixel_origin.X = 0;
			pixel_origin.Y += height;

			// Case : new texture is taller than original texture
			if (true_end.Y - pixel_origin.Y >= height)
			{
				// Getting data from previous step (width filled)
				data = get_data(texture, texture.Width, height, true_origin);

				while (true_end.Y - pixel_origin.Y >= height)
				{
					// Copying data to new texture until height is filled
					set_data(texture, pixel_origin, texture.Width, height, data);
					pixel_origin.Y += height;
				}
			}

			// Completing height
			if (pixel_origin.Y < true_end.Y)
			{
				height = (int)true_end.Y - (int)pixel_origin.Y;
				// Getting data from original texture
				data = get_data(texture, texture.Width, height, true_origin);

				// Copying data to new texture until width is filled
				set_data(texture, pixel_origin, texture.Width, height, data);
			}

			default_bases[size][season] = texture;
			return texture;
		}

		private static Texture2D extract_tile(Texture2D from, int tile_x, int tile_y)
		{
			Texture2D texture = new(
				GameRunner.instance.GraphicsDevice,
				(int)tile_size.X, (int)tile_size.Y
			);

			int data_size = (int)(tile_size.X * tile_size.Y);

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

		private static void load_borders(IModHelper helper)
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
	
		public static Dictionary<Vector2, Texture2D> get_default_border(Vector2 size)
		{
			Dictionary<Vector2, Texture2D> border_parts = new();

			if (size.X > 1 && size.Y > 1)
			{
				border_parts[new(0, 		0		)] = get_border(size, "top_left", 		0);
				border_parts[new(size.X-1, 	0		)] = get_border(size, "top_right", 		0);
				border_parts[new(0, 		size.Y-1)] = get_border(size, "bottom_left", 	0);
				border_parts[new(size.X-1, 	size.Y-1)] = get_border(size, "bottom_right", 	0);

				for (int x = 1; x < size.X-1; x++)
				{
					border_parts[new(x, 0		)] = get_border(size, "top", 	0);
					border_parts[new(x, size.Y-1)] = get_border(size, "bottom", 0);
				}

				for (int y = 1; y < size.Y-1; y++)
				{
					border_parts[new(0, 		y)] = get_border(size, "left", 	0);
					border_parts[new(size.X-1, 	y)] = get_border(size, "right", 0);
				}
			}

			// if (size == Vector2.One)
			// {
			// 	border_parts[new(0, 0)] = 
			// }

			return border_parts;
		}

		public static Texture2D get_default_feature(Vector2 size)
		{
			if (default_features.ContainsKey(size))
				return default_features[size];
			
			int width = (int)(tile_size.X * size.X), height = (int)(tile_size.Y * size.Y);

			Texture2D texture = new(
				GameRunner.instance.GraphicsDevice,
				width, height
			);

			Color[] data = Enumerable.Repeat(Color.Transparent, width*height).ToArray();
			set_data(texture, new(0, 0), width, height, data);

			default_features[size] = texture;
			return texture;
		}
	}
}