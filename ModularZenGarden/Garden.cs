using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class Garden
	{
		public Point position { get; private set; }
		public GardenType type { get; private set; }

		private readonly Dictionary<Point, bool> contacts = new();
		public readonly List<BorderPart> border_parts = new();

		public Garden(Furniture furniture)
		{
			position = Utils.get_pos(furniture);
			type = GardenType.get_type(furniture);

			// building the dictionary of contacts around the Garden
			for (int x = -1; x <= type.size.X; x++)
			{
				contacts[new Point(x, -1)] = false;
				contacts[new Point(x, type.size.Y)] = false;
			}
			for (int y = 0; y < type.size.Y; y++)
			{
				contacts[new Point(-1, y)] = false;
				contacts[new Point(type.size.X, y)] = false;
			}
		}

		public void add_contact(Point tile)
		{
			if (contacts.ContainsKey(tile))
				contacts[tile] = true;
		}

		public void remove_contact(Point tile)
		{
			if (contacts.ContainsKey(tile))
				contacts[tile] = false;
		}
	
		public void update_texture()
		{
			border_parts.Clear();

			if (type.size.X > 1 && type.size.Y > 1)
			{
				// top left
				Point tile = Point.Zero;
				border_parts.Add(new (
					SpriteManager.get_border(
						type.size, "top_left", get_contact_value(tile)
					), tile
				));

				// top
				for (tile.X = 1; tile.X < type.size.X-1; tile.X++)
				{
					border_parts.Add(new (
						SpriteManager.get_border(
							type.size, "top", get_contact_value(tile)
						), tile
					));
				}

				// top right
				tile.X = type.size.X-1;
				border_parts.Add(new (
					SpriteManager.get_border(
						type.size, "top_right", get_contact_value(tile)
					), tile
				));

				// left & right
				for (tile.Y = 1; tile.Y < type.size.Y-1; tile.Y++)
				{
					tile.X = 0;
					border_parts.Add(new (
						SpriteManager.get_border(
							type.size, "left", get_contact_value(tile)
						), tile
					));
						
					tile.X = type.size.X-1;
					border_parts.Add(new (
						SpriteManager.get_border(
							type.size, "right", get_contact_value(tile)
						), tile
					));
				}

				// bottom left
				tile.X = 0; tile.Y = type.size.Y-1;
				border_parts.Add(new (
					SpriteManager.get_border(
						type.size, "bottom_left", get_contact_value(tile)
					), tile
				));

				// bottom
				for (tile.X = 1; tile.X < type.size.X-1; tile.X++)
				{
					border_parts.Add(new (
						SpriteManager.get_border(
							type.size, "bottom", get_contact_value(tile)
						), tile
					));
				}

				// bottom right
				tile.X = type.size.X-1;
				border_parts.Add(new (
					SpriteManager.get_border(
						type.size, "bottom_right", get_contact_value(tile)
					), tile
				));
			}

			else if (type.size.X == 1 && type.size.Y == 1)
			{
				border_parts.Add(new(
					SpriteManager.get_border(
						type.size, "top", get_contact_value(new(0, -1))
					), Point.Zero
				));
				border_parts.Add(new(
					SpriteManager.get_border(
						type.size, "left", get_contact_value(new(-1, 0))
					), Point.Zero
				));
				border_parts.Add(new(
					SpriteManager.get_border(
						type.size, "right", get_contact_value(new(1, 0))
					), Point.Zero
				));
				border_parts.Add(new(
					SpriteManager.get_border(
						type.size, "bottom", get_contact_value(new(0, 1))
					), Point.Zero
				));
			}

			else throw new Exception("Unsupported Garden Size.");
		}

		private int get_contact_value(Point tile)
		{
			int value = 0;
			int p = 0;
			for (int y = -1; y <= 1; y++)
			{
				for (int x = -1; x <= 1; x++)
				{
					Point contact_tile = tile + new Point(x, y);
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