using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class Garden
	{
		public Vector2 position { get; private set; }
		public GardenType type { get; private set; }

		private Furniture furniture;

		private Dictionary<Vector2, bool> contacts = new();
		public Dictionary<Vector2, Texture2D> border_textures { get; private set; } = new();

		public Garden(Furniture furniture)
		{
			position = Utils.get_pos(furniture);
			type = GardenType.get_type(furniture);
			this.furniture = furniture;

			// building the dictionary of contacts around the Garden
			for (int x = -1; x <= type.size.X; x++)
			{
				contacts[new Vector2(x, -1)] = false;
				contacts[new Vector2(x, type.size.Y)] = false;
			}
			for (int y = 0; y < type.size.Y; y++)
			{
				contacts[new Vector2(-1, y)] = false;
				contacts[new Vector2(type.size.X, y)] = false;
			}
		}

		public void add_contact(Vector2 tile)
		{
			if (contacts.ContainsKey(tile))
				contacts[tile] = true;
		}

		public void remove_contact(Vector2 tile)
		{
			if (contacts.ContainsKey(tile))
				contacts[tile] = false;
		}
	
		public void update_texture()
		{
			// top left
			Vector2 tile = new(0, 0);
			border_textures[tile] = SpriteManager.get_border(
				type.size, "top_left",
				get_contact_value(tile)
			);

			// top
			for (tile.X = 1; tile.X < type.size.X-1; tile.X++)
			{
				border_textures[tile] = SpriteManager.get_border(
					type.size, "top",
					get_contact_value(tile)
				);
			}

			// top right
			tile.X = type.size.X-1;
			border_textures[tile] = SpriteManager.get_border(
				type.size, "top_right",
				get_contact_value(tile)
			);

			// left & right
			for (tile.Y = 1; tile.Y < type.size.Y-1; tile.Y++)
			{
				tile.X = 0;
				border_textures[tile] = SpriteManager.get_border(
					type.size, "left",
					get_contact_value(tile)
				);
					
				tile.X = type.size.X-1;
				border_textures[tile] = SpriteManager.get_border(
					type.size, "right",
					get_contact_value(tile)
				);
			}

			// bottom left
			tile.X = 0; tile.Y = type.size.Y-1;
			border_textures[tile] = SpriteManager.get_border(
				type.size, "bottom_left",
				get_contact_value(tile)
			);

			// bottom
			for (tile.X = 1; tile.X < type.size.X-1; tile.X++)
			{
				border_textures[tile] = SpriteManager.get_border(
					type.size, "bottom",
					get_contact_value(tile)
				);
			}

			// bottom right
			tile.X = type.size.X-1;
			border_textures[tile] = SpriteManager.get_border(
				type.size, "bottom_right",
				get_contact_value(tile)
			);
		}

		private int get_contact_value(Vector2 tile)
		{
			int value = 0;
			int p = 0;
			for (int y = -1; y <= 1; y++)
			{
				for (int x = -1; x <= 1; x++)
				{
					Vector2 contact_tile = tile + new Vector2(x, y);
					if (contacts.ContainsKey(contact_tile))
					{
						if (contacts[contact_tile])
							value += 1 << p;
						p ++;
					}
				}
			}
			return value;
		}
	}

	class GardenComparer : IEqualityComparer<Garden>
	{
		public bool Equals(Garden? g_1, Garden? g_2)
		{
			if (g_1 != null && g_2 != null) 
			{
				return g_1.position == g_2.position
					& g_1.type.Equals(g_2.type);
			}
			return false;	
		}

		public int GetHashCode(Garden garden)
		{
			return garden.position.GetHashCode()
				+ garden.type.GetHashCode();
		} 
	}
}