using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class GardenType
	{
		public readonly string name;
		public readonly string author;
		public readonly Vector2 size;
		public readonly string dim;
		public readonly Dictionary<string, Texture2D> bases = new();
		public readonly Dictionary<string, Texture2D> features = new();

		public static readonly Dictionary<string, GardenType> types = new();

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

			if (Utils.monitor == null) throw new NullReferenceException("Monitor was not set.");

			name = type_name;
			author = (string)type_data["author"];
			size = new( (long)type_data["width"], (long)type_data["height"] );
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
							Utils.monitor.Log(
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
							Utils.monitor.Log(
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

		public void patch_image(IAssetDataForImage editor)
		{
			// Patching the image used for the items

			string season = SDate.Now().SeasonKey;

			editor.PatchImage(
				source: bases[season],
				patchMode: PatchMode.Replace
			);

			Dictionary<Vector2, Texture2D> border_parts = SpriteManager.get_default_border(size);

			foreach (Vector2 tile in get_draw_order())
			{
				editor.PatchImage(
					source: border_parts[tile],
					targetArea: new Rectangle(
						(int)(tile.X * SpriteManager.tile_size.X),
						(int)((tile.Y + size.Y-1) * SpriteManager.tile_size.X),
						(int)SpriteManager.tile_size.X,
						(int)SpriteManager.tile_size.Y
					),
					patchMode: PatchMode.Overlay
				);
			}
			editor.PatchImage(
				source: features[season],
				patchMode: PatchMode.Overlay
			);
		}

		public void add_contacts(Garden garden, Vector2 origin)
		{
			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
				{
					garden.add_contact(origin + new Vector2(x, y));
				}
			}
		}

		public void remove_contacts(Garden garden, Vector2 origin)
		{
			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
				{
					garden.remove_contact(origin + new Vector2(x, y));
				}
			}
		}

		public List<Vector2> get_draw_order()
		{
			List<Vector2> draw_order = new();

			for (int x = 0; x < size.X; x++)
			{
				draw_order.Add(new(x, 0));
			}

			for (int y = 1; y < size.Y-1; y++)
			{
				draw_order.Add(new(0, 		 y));
				draw_order.Add(new(size.X-1, y));
			}

			for (int x = 0; x < size.X; x++)
			{
				draw_order.Add(new(x, size.Y-1));
			}

			return draw_order;
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