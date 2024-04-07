using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace ModularZenGarden {
	
	class GardenCache
	{
		static readonly Dictionary<Vector2, Garden> gardens = new();

		static readonly HashSet<Garden> to_update = new(new GardenComparer());

		public static void clear()
		{
			gardens.Clear();
		}

		public static void remove_garden(Furniture furniture)
		{
			Garden? garden = get_garden(furniture) ?? throw new NullReferenceException(
					$"No registered garden matched at {Utils.get_pos(furniture)}."
				);
			
			gardens.Remove(garden.position);

			HashSet<Garden> neighbors = get_neighbors(garden);

			foreach (Garden garden_2 in neighbors)
			{
				Vector2 delta = garden_2.position - garden.position;
				garden.type.remove_contacts(garden_2, -delta);
			}

			to_update.UnionWith(neighbors);
		}

		public static void add_garden(Furniture furniture)
		{
			Garden garden = new(furniture);

			HashSet<Garden> neighbors = get_neighbors(garden);

			foreach (Garden garden_2 in neighbors)
			{
				Vector2 delta = garden_2.position - garden.position;
				garden_2.type.add_contacts(garden, delta);
				garden.type.add_contacts(garden_2, -delta);
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
			Vector2 base_pos = Utils.get_pos(furniture);
			GardenType type = GardenType.get_type(furniture);
			// Removing a garden from a tile that's not its top left tile will move it
			// To find it anyway, its necessary to search around the position
			for (int x = 0; x < type.size.X; x++)
			{
				for (int y = 0; y < type.size.Y; y++)
				{
					Vector2 rel_pos = base_pos - new Vector2(x, y);
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
				Vector2 delta = garden_2.position - garden.position;
				if (delta.X < -garden_2.type.size.X || delta.X > garden.type.size.X) continue;
				if (delta.Y < -garden_2.type.size.Y || delta.Y > garden.type.size.Y) continue;
				neighbors.Add(garden_2);
			}

			return neighbors;
		}
	}

}