using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;

namespace ModularZenGarden {

	struct BorderPart
	{
		public Texture2D texture;
		public Point tile;

		public BorderPart(Texture2D texture, Point tile)
		{
			this.texture = texture;
			this.tile = tile;
		}
	}
	
	class GardenType
	{
		public readonly string name;
		public readonly string author;
		public readonly Point size;
		public readonly string dim;
		public readonly Dictionary<string, Texture2D> bases = new();
		public readonly Dictionary<string, Texture2D> features = new();

		public static readonly Dictionary<string, GardenType> types = new();
		public static readonly Dictionary<Point, List<BorderPart>> default_borders = new();

		public static string get_type_name(string full_name)
		{
			return full_name.Remove(0, "MZG ".Length);
		}

		public static GardenType get_type(Furniture furniture)
		{
			return types[get_type_name(furniture.ItemId)];
		}

		public GardenType(string type_name, Dictionary<string, object> type_data, IModHelper helper)
		{
			types[type_name] = this;

			name = type_name;
			author = (string)type_data["author"];
			size = new(
				(int)(long)type_data["width"],
				(int)(long)type_data["height"]
			);
			if (size.X < 1 || size.Y < 1)
			{
				throw new ArgumentException($"Size of type {type_name} is invalid.");
			}
			dim = $"{size.X}x{size.Y}";

			string path = $"assets/gardens/{name}/";

			// loading textures of bases and features
			foreach (string season in Utils.seasons)
			{
				if ((bool)type_data.GetValueOrDefault("use_default_base", false))
					bases[season] = SpriteManager.get_default_base(size, season);
				
				else
				{
					try
					{
						// Searching for seasonal variant
						bases[season] = helper.ModContent.Load<Texture2D>(
							path + $"base_{season}.png"
						);
					}
					catch (Microsoft.Xna.Framework.Content.ContentLoadException)
					{
						try
						{
							// Searching for general sprite
							bases[season] = helper.ModContent.Load<Texture2D>(
								path + $"base.png"
							);
						}
						catch (Microsoft.Xna.Framework.Content.ContentLoadException)
						{
							Utils.log(
								$"Sprite for {season} base of {type_name} was not found, fallback to default.",
								LogLevel.Warn
							);
							// Loading default sprite
							bases[season] = SpriteManager.get_default_base(size, season);
						}
					}
				}

				if ((bool)type_data.GetValueOrDefault("use_default_feature", false))
					features[season] = SpriteManager.get_default_feature(size);

				else
				{
					try
					{
						// Searching for seasonal variant
						features[season] = helper.ModContent.Load<Texture2D>(
							path + $"feature_{season}.png"
						);
					}
					catch (Microsoft.Xna.Framework.Content.ContentLoadException)
					{
						try
						{
							// Searching for general sprite
							features[season] = helper.ModContent.Load<Texture2D>(
								path + $"feature.png"
							);
						}
						catch (Microsoft.Xna.Framework.Content.ContentLoadException)
						{
							Utils.log(
								$"Sprite for {season} feature of {type_name} was not found, fallback to default.",
								LogLevel.Warn
							);
							// Loading default sprite
							features[season] = SpriteManager.get_default_feature(size);
						}
					}
				}
				
			}
		}

		public string get_string_data(string full_name)
		{
			// Building the string to patch into Content/Data/Furniture.xnb

			string string_data = full_name;					// internal name
			string_data += "/other";						// type
			string_data += $"/{size.X} {2*size.Y}";			// texture size (probably unused)
			string_data += $"/{size.X} {size.Y}";			// collision size
			string_data += "/1";							// rotation
			string_data += "/20";							// magic number price for now
			string_data += "/2";							// placeable outdoors & indoors
			string_data += $"/Zen Garden by {author}";		// display name
			string_data += "/0";							// sprite index
			string_data += $"/{full_name}";					// texture path (to direct load overwrite)

			return string_data;
		}

		public Texture2D get_base()
		{
			Texture2D base_ = bases[SDate.Now().SeasonKey];

			Texture2D texture = new(
				GameRunner.instance.GraphicsDevice,
				base_.Width,
				base_.Height
			);

			texture.CopyFromTexture(base_);

			return texture;
		}

		public void patch_image(IAssetDataForImage editor)
		{
			// Patching the image used for the items

			string season = SDate.Now().SeasonKey;

			// editor.PatchImage(
			// 	source: bases[season],
			// 	patchMode: PatchMode.Replace
			// );

			foreach (BorderPart border_part in get_default_border())
			{
				Rectangle target_area = new(
					border_part.tile.X * SpriteManager.tile_size.X,
					(border_part.tile.Y + size.Y - 1) * SpriteManager.tile_size.X,
					SpriteManager.tile_size.X, SpriteManager.tile_size.Y
				);

				editor.PatchImage(
					source: border_part.texture,
					targetArea: target_area,
					patchMode: PatchMode.Overlay
				);
			}
			editor.PatchImage(
				source: features[season],
				patchMode: PatchMode.Overlay
			);
		}

		public void add_contacts(Garden garden, Point origin)
		{
			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
				{
					garden.add_contact(origin + new Point(x, y));
				}
			}
		}

		public void remove_contacts(Garden garden, Point origin)
		{
			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
				{
					garden.remove_contact(origin + new Point(x, y));
				}
			}
		}

		public List<BorderPart> get_default_border()
		{
			if (default_borders.ContainsKey(size))
				return default_borders[size];

			List<BorderPart> border_parts = new();

			if (size.X > 1 && size.Y > 1)
			{
				border_parts.Add(new(SpriteManager.get_border(size, "top_left", 0), Point.Zero));

				for (int x = 1; x < size.X-1; x++)
					border_parts.Add(new(SpriteManager.get_border(size, "top", 0), new(x, 0)));

				border_parts.Add(new(SpriteManager.get_border(size, "top_right", 0), new(size.X-1, 0)));

				for (int y = 1; y < size.Y-1; y++)
				{
					border_parts.Add(new(SpriteManager.get_border(size, "left", 0), new(0, y)));
					border_parts.Add(new(SpriteManager.get_border(size, "right", 0), new(size.X-1, y)));
				}
				
				border_parts.Add(new(SpriteManager.get_border(size, "bottom_left", 0), new(0, size.Y-1)));

				for (int x = 1; x < size.X-1; x++)
					border_parts.Add(new(SpriteManager.get_border(size, "bottom", 0), new(x, size.Y-1)));


				border_parts.Add(new(SpriteManager.get_border(size, "bottom_right", 0), new(size.X-1, size.Y-1)));
			}

			else if (size.X == 1 && size.Y == 1)
			{
				border_parts.Add(new(SpriteManager.get_border(size, "top", 0), Point.Zero));
				border_parts.Add(new(SpriteManager.get_border(size, "left", 0), Point.Zero));
				border_parts.Add(new(SpriteManager.get_border(size, "right", 0), Point.Zero));
				border_parts.Add(new(SpriteManager.get_border(size, "bottom", 0), Point.Zero));
			}

			else throw new Exception("Unsupported Garden Size.");

			default_borders[size] = border_parts;
			return border_parts;
		}

		// override object.ToString
		public override string ToString()
		{
			return name;
		}

		// override object.Equals
		public override bool Equals(object? obj)
		{	
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			return name == obj.ToString();
		}
		
		// override object.GetHashCode
		public override int GetHashCode()
		{
			return name.GetHashCode();
		}
	}

}