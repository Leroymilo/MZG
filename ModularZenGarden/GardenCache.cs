using Microsoft.Xna.Framework;
using StardewValley.Buildings;
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

		public static void remove_garden(Furniture furniture)
		{
			Garden? garden = get_garden(furniture)
				?? throw new NullReferenceException(
					$"No registered garden matched at {Utils.get_pos(furniture)}."
				);
			
			remove_garden(garden);

			if (garden.type.name == "catalogue")
				catalogues.Remove(garden.position);
		}

		public static void remove_garden(Building building)
		{
			Garden? garden = get_garden(building)
				?? throw new NullReferenceException(
					$"No registered garden matched at {Utils.get_pos(building)}."
				);
			
			remove_garden(garden);
		}

		private static void remove_garden(Garden garden)
		{
			gardens.Remove(garden.position);

			HashSet<Garden> neighbors = get_neighbors(garden);

			foreach (Garden garden_2 in neighbors)
			{
				Point delta = garden_2.position - garden.position;
				garden.type.set_contacts(garden_2, Point.Zero - delta, false);
			}

			to_update.UnionWith(neighbors);
		}

		public static void add_garden(Furniture furniture)
		{
			Garden garden = new(furniture);

			add_garden(garden);

			if (garden.type.name == "catalogue")
				catalogues.Add(garden.position);
		}

		public static void add_garden(Building building)
		{
			Garden garden = new(building);

			add_garden(garden);
		}

		private static void add_garden(Garden garden)
		{
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
			GardenType type = GardenType.get_type(furniture);
			Point base_pos = Utils.get_pos(furniture);
			return get_garden(type, base_pos);
		}

		public static Garden? get_garden(Building building)
		{
			GardenType type = GardenType.get_type(building);
			Point base_pos = Utils.get_pos(building);
			return get_garden(type, base_pos);
		}

		private static Garden? get_garden(GardenType type, Point base_pos)
		{
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