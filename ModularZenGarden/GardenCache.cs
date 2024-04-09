using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class GardenCache
	{
		static readonly Dictionary<Point, Garden> gardens = new();

		static readonly HashSet<Garden> to_update = new(new GardenComparer());

		static readonly HashSet<Point> catalogues = new();

		public static void clear()
		{
			gardens.Clear();
			catalogues.Clear();
		}

		public static bool remove_garden(Furniture furniture)
		{
			// Returns true if the furniture is a catalogue

			Garden? garden = get_garden(furniture) ?? throw new NullReferenceException(
					$"No registered garden matched at {Utils.get_pos(furniture)}."
				);
			
			gardens.Remove(garden.position);

			HashSet<Garden> neighbors = get_neighbors(garden);

			foreach (Garden garden_2 in neighbors)
			{
				Point delta = garden_2.position - garden.position;
				garden.type.set_contacts(garden_2, Point.Zero - delta, false);
			}

			to_update.UnionWith(neighbors);

			if (garden.type.name == "catalogue")
			{
				catalogues.Remove(garden.position);
				return true;
			}
			return false;
		}

		public static bool add_garden(Furniture furniture)
		{
			// Returns true if the furniture is a catalogue

			Garden garden = new(furniture);

			HashSet<Garden> neighbors = get_neighbors(garden);

			foreach (Garden garden_2 in neighbors)
			{
				Point delta = garden_2.position - garden.position;
				garden_2.type.set_contacts(garden, delta, true);
				garden.type.set_contacts(garden_2, Point.Zero - delta, true);
			}

			gardens[garden.position] = garden;
			
			to_update.UnionWith(neighbors);
			to_update.Add(garden);

			if (garden.type.name == "catalogue")
			{
				catalogues.Add(garden.position);
				return true;
			}
			return false;
		}

		public static void update_textures()
		{
			foreach (Garden garden in to_update)
			{
				garden.update_texture();
			}

			to_update.Clear();
		}

		public static Garden? get_garden(Furniture furniture)
		{
			Point base_pos = Utils.get_pos(furniture);
			GardenType type = GardenType.get_type(furniture);
			// Removing a garden from a tile that's not its top left tile will move it
			// To find it anyway, its necessary to search around the position
			for (int x = 0; x < type.size.X; x++)
			{
				for (int y = 0; y < type.size.Y; y++)
				{
					Point rel_pos = base_pos - new Point(x, y);
					if (gardens.ContainsKey(rel_pos))
					{
						if (gardens[rel_pos].type == type) {
							return gardens[rel_pos];
						}
					}
				}
			}
			
			return null;
		}

		private static HashSet<Garden> get_neighbors(Garden garden)
		{
			HashSet<Garden> neighbors = new( new GardenComparer() );

			foreach (Garden garden_2 in gardens.Values)
			{
				Point delta = garden_2.position - garden.position;
				if (delta.X < -garden_2.type.size.X || delta.X > garden.type.size.X) continue;
				if (delta.Y < -garden_2.type.size.Y || delta.Y > garden.type.size.Y) continue;
				neighbors.Add(garden_2);
			}

			return neighbors;
		}
	}

}