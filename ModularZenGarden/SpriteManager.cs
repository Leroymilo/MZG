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

		public static readonly Point tile_size = new(16, 32); // SHOULD NOT CHANGE

		private static Texture2D? base_3x3;
		private static Texture2D? base_3x3_winter;
		static readonly Dictionary<Point, Dictionary<string, Texture2D>> default_bases = new();

		static readonly Dictionary<string, List<Texture2D>> borders_NxN = new();
		static readonly Dictionary<string, List<Texture2D>> borders_1x1 = new();

		static readonly Dictionary<Point, Texture2D> default_features = new();

		public static void load_sprites(IModHelper helper)
		{
			base_3x3 = helper.ModContent.Load<Texture2D>("assets/default_base_3x3");
			base_3x3_winter = helper.ModContent.Load<Texture2D>("assets/default_base_3x3_winter");

			load_borders(helper);
		}

		private static Color[] get_data(Texture2D from, int w, int h, Point? origin = null)
		{
			if (origin == null)
				origin = Point.Zero;
			
			int x = origin.Value.X, y = origin.Value.Y;
			
			Color[] data = new Color[w*h];
			from.GetData(
				0,
				new Rectangle(x, y, w, h),
				data, 0, w*h
			);
			return data;

		}

		private static void set_data(Texture2D to, Point origin, int w, int h, Color[] data)
		{
			to.SetData(
				0,
				new Rectangle(
					origin.X, origin.Y,
					w, h
				),
				data, 0, w*h
			);
		}

		public static Texture2D get_default_base(Point size, string season)
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
				tile_size.X * size.X,
				tile_size.Y * size.Y
			);

			Point true_origin = new(0, size.Y * (tile_size.Y - tile_size.X) - 2);
			Point pixel_origin = true_origin;
			Point true_end = new(tile_size.X * size.X, tile_size.Y * size.Y - 2);
			Color[] data;

			// Case : new texture is wider than original texture
			// Repeating original texture in width
			int height = Math.Min(true_end.Y - true_origin.Y, from_.Height);

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
			int width = true_end.X - pixel_origin.X;
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
				height = true_end.Y - pixel_origin.Y;
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
				tile_size.X, tile_size.Y
			);

			int data_size = tile_size.X * tile_size.Y;

			Rectangle rect = new(
				tile_size.X * tile_x,
				tile_size.Y * tile_y,
				tile_size.X,
				tile_size.Y
			);

			Color[] data = new Color[data_size];
			from.GetData(
				0,
				rect,
				data, 0, data_size
			);

			texture.SetData(
				0,
				new Rectangle(
					0, 0,
					tile_size.X,
					tile_size.Y
				),
				data, 0, data_size
			);

			return texture;
		}

		private static void load_borders(IModHelper helper)
		{
			// NxN
			Texture2D spritesheet_NxN = helper.ModContent.Load<Texture2D>("assets/borders/NxN.png");
			Point nb_sprites = new(spritesheet_NxN.Width/tile_size.X, spritesheet_NxN.Height/tile_size.Y);

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

		public static Texture2D get_border(Point size, string type, int value)
		{
			if (size.X > 1 && size.Y > 1)
				return borders_NxN[type][value];
			
			if (size.X == 1 && size.Y == 1)
				return borders_1x1[type][value];
			
			throw new Exception("Unsupported Garden Size.");
		}

		public static Texture2D get_default_feature(Point size)
		{
			if (default_features.ContainsKey(size))
				return default_features[size];
			
			int width = tile_size.X * size.X, height = tile_size.Y * size.Y;

			Texture2D texture = new(
				GameRunner.instance.GraphicsDevice,
				width, height
			);

			Color[] data = Enumerable.Repeat(Color.Transparent, width*height).ToArray();
			set_data(texture, Point.Zero, width, height, data);

			default_features[size] = texture;
			return texture;
		}
	}
}