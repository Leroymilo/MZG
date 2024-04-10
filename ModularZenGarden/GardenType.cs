using System.ComponentModel;
using System.Data.Common;
using System.Net;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
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
		public readonly string? author = null;
		public readonly Point size;
		public readonly string dim;
		public readonly Dictionary<string, Texture2D> bases = new();
		public readonly Dictionary<string, Texture2D> features = new();

		public static readonly Dictionary<string, GardenType> types = new();
		public static readonly Dictionary<Point, List<BorderPart>> default_borders = new();
		public static HashSet<string> building_names = new() {
			"Earth Obelisk",
			"Water Obelisk",
			"Desert Obelisk"
		};	// Excludes the Island Obelisk for now

		public static bool is_garden(Furniture furniture)
		{
			return furniture.ItemId.StartsWith("MZG");
		}

		public static bool is_garden(Building building)
		{
			return building_names.Contains(building.buildingType.Value);
		}

		public static string get_type_name(string full_name)
		{
			return full_name.Remove(0, "MZG ".Length);
		}

		public static GardenType get_type(Furniture furniture)
		{
			return types[get_type_name(furniture.ItemId)];
		}

		public static GardenType get_type(Building building)
		{
			switch (building.buildingType.Value)
			{
				case "Earth Obelisk":
					return types["stones"];
				case "Water Obelisk":
					return types["tree"];
				case "Desert Obelisk":
					return types["lantern"];
				default:
					throw new InvalidDataException("This building type has no garden type associated.");
			}
		}

		public static void make_blank_types(IModHelper helper)
		{
			for (long w = 1; w < 4; w++)
			{
				for (long h = 1; h < 4; h++)
				{
					Dictionary<string, object> type_data = new() {
						{"width", w},
						{"height", h},
						{"use_default_base", true},
						{"use_default_feature", true}
					};
					new GardenType($"empty {w}x{h}", type_data, helper);
				}
			}
		}

		public GardenType(string type_name, Dictionary<string, object> type_data, IModHelper helper)
		{
			types[type_name] = this;

			name = type_name;

			if (type_data.ContainsKey("author"))
				author = (string)type_data["author"];
			
			size = new(
				(int)(long)type_data["width"],
				(int)(long)type_data["height"]
			);
			if (size.X < 1 || size.Y < 1)
				throw new ArgumentException($"Size of type {type_name} is invalid.");
			// if (size.X > 1 && size.Y == 1)
			// 	throw new ArgumentException("Garden of 1 tall are not supported. Consider using multiple 1x1 gardens.");
			dim = $"{size.X}x{size.Y}";

			// loading textures of bases and features
			string path = $"assets/gardens/{name}/";
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

			string display_name;
			if (name == "catalogue")
				display_name = "/Zen Garden Catalogue";
			else
			{
				display_name = $"/Zen Garden {dim}";
				if (author != null) display_name += $" by {author}";
			}

			string string_data = full_name;						// internal name
			string_data += "/other";							// type
			string_data += $"/{size.X} {2*size.Y}";				// texture size (probably unused)
			string_data += $"/{size.X} {size.Y}";				// collision size
			string_data += "/1";								// rotation
			string_data += "/2000";								// price for catalog
			string_data += "/2";								// placeable outdoors & indoors
			string_data += display_name;						// display name
			string_data += "/0";								// sprite index
			string_data += $"/{full_name}";						// texture path (to direct load overwrite)
			string_data += "/true";								// true to prevent showing in vanilla catalog
			string_data += "/MZG_furniture";					// context tag to appear in custom catalog

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

		public void patch_image(IAssetData asset)
		{
			// Patching the image used for the items

			IAssetDataForImage editor = asset.AsImage();

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
				source: features[SDate.Now().SeasonKey],
				patchMode: PatchMode.Overlay
			);
		}

		public void set_contacts(Garden garden, Point origin, bool value)
		{
			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
				{
					garden.set_contact(origin + new Point(x, y), value);
				}
			}
		}

		public void get_border(Func<Point, int> c_value_getter, List<BorderPart> border_parts)
		{
			border_parts.Clear();

			// 1x1
			if (size.X == 1 && size.Y == 1)
			{
				border_parts.Add(new(
					SpriteManager.get_border_part(
						size, "top", c_value_getter(new(0, -1))
					), Point.Zero
				));
				border_parts.Add(new(
					SpriteManager.get_border_part(
						size, "left", c_value_getter(new(-1, 0))
					), Point.Zero
				));
				border_parts.Add(new(
					SpriteManager.get_border_part(
						size, "right", c_value_getter(new(1, 0))
					), Point.Zero
				));
				border_parts.Add(new(
					SpriteManager.get_border_part(
						size, "bottom", c_value_getter(new(0, 1))
					), Point.Zero
				));
			}

			// 1xN
			else if (size.X == 1 && size.Y > 1)
			{
				Point top = Point.Zero;

				// top
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "top", c_value_getter(top + new Point(0, -1))
					), top
				));

				// top left
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "top_left", c_value_getter(top + new Point(-1, 0))
					), top
				));

				// top right
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "top_right", c_value_getter(top + new Point(1, 0))
					), top
				));

				// left & right
				for (int y = 1; y < size.Y-1; y++)
				{
					Point tile = new(0, y);

					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "left", c_value_getter(tile + new Point(-1, 0))
						), tile
					));
						
					tile.X = size.X-1;
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "right", c_value_getter(tile + new Point(1, 0))
						), tile
					));
				}

				Point bottom = new(0, size.Y-1);

				// bottom left
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "bottom_left", c_value_getter(bottom + new Point(-1, 0))
					), bottom
				));

				// bottom right
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "bottom_right", c_value_getter(bottom + new Point(1, 0))
					), bottom
				));

				// bottom
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "bottom", c_value_getter(bottom + new Point(0, 1))
					), bottom
				));
			}

			// Nx1
			else if (size.X > 1 && size.Y == 1)
			{
				Point left = Point.Zero;

				// left top
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "left_top", c_value_getter(left + new Point(0, -1))
					), left
				));

				// left
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "left", c_value_getter(left + new Point(-1, 0))
					), left
				));

				// left bottom
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "left_bottom", c_value_getter(left + new Point(0, 1))
					), left
				));

				// top
				for (int x = 1; x < size.X-1; x++)
				{
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "top", c_value_getter(new(x, -1))
						), new(x, 0)
					));
				}

				Point right = new(size.X-1, 0);

				// right top
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "right_top", c_value_getter(right + new Point(0, -1))
					), right
				));

				// right
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "right", c_value_getter(right + new Point(1, 0))
					), right
				));

				// right bottom
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "right_bottom", c_value_getter(right + new Point(0, 1))
					), right
				));

				// bottom
				for (int x = 1; x < size.X-1; x++)
				{
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "bottom", c_value_getter(new(x, 1))
						), new(x, 0)
					));
				}
			}

			// NxN
			else if (size.X > 1 && size.Y > 1)
			{
				// top left
				Point tile = Point.Zero;
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "top_left", c_value_getter(tile)
					), tile
				));

				// top
				for (tile.X = 1; tile.X < size.X-1; tile.X++)
				{
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "top", c_value_getter(tile)
						), tile
					));
				}

				// top right
				tile.X = size.X-1;
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "top_right", c_value_getter(tile)
					), tile
				));

				// left & right
				for (tile.Y = 1; tile.Y < size.Y-1; tile.Y++)
				{
					tile.X = 0;
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "left", c_value_getter(tile)
						), tile
					));
						
					tile.X = size.X-1;
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "right", c_value_getter(tile)
						), tile
					));
				}

				// bottom left
				tile.X = 0; tile.Y = size.Y-1;
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "bottom_left", c_value_getter(tile)
					), tile
				));

				// bottom
				for (tile.X = 1; tile.X < size.X-1; tile.X++)
				{
					border_parts.Add(new (
						SpriteManager.get_border_part(
							size, "bottom", c_value_getter(tile)
						), tile
					));
				}

				// bottom right
				tile.X = size.X-1;
				border_parts.Add(new (
					SpriteManager.get_border_part(
						size, "bottom_right", c_value_getter(tile)
					), tile
				));
			}

			else throw new Exception("Unsupported Garden Size.");
		}

		public List<BorderPart> get_default_border()
		{
			if (default_borders.ContainsKey(size))
				return default_borders[size];

			List<BorderPart> border_parts = new();

			get_border(tile => {return 0;}, border_parts);

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