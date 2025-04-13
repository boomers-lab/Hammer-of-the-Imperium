// Assembly-CSharp, Version=1.5.9214.33606, Culture=neutral, PublicKeyToken=null
// RimWorld.RoadDefGenStep_Place
using RimWorld;
using Verse;

namespace eridanus_trenches
{
	public class RoadDefGenStep_PlaceTrench : RoadDefGenStep_Bulldoze
	{
		public BuildableDef place;

		public int proximitySpacing;

		public bool onlyIfOriginAllows;

		public string suppressOnTerrainTag;

		public override void Place(Map map, IntVec3 position, TerrainDef rockDef, IntVec3 origin, GenStep_Roads.DistanceElement[,] distance)
		{
			return;
			if (onlyIfOriginAllows)
			{
				bool flag = false;
				for (int i = 0; i < 4; i++)
				{
					IntVec3 c = position + GenAdj.CardinalDirections[i];
					if (c.InBounds(map) && chancePerPositionCurve.Evaluate(distance[c.x, c.z].fromRoad) > 0f && (GenConstruct.CanBuildOnTerrain(place, c, map, Rot4.North) || c.GetTerrain(map) == place) && (GenConstruct.CanBuildOnTerrain(place, distance[c.x, c.z].origin, map, Rot4.North) || distance[c.x, c.z].origin.GetTerrain(map) == place))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return;
				}
			}
			if (!suppressOnTerrainTag.NullOrEmpty() && map.terrainGrid.TerrainAt(position).HasTag(suppressOnTerrainTag))
			{
				return;
			}
			//base.Place(map, position, rockDef, origin, distance);
			if (place is ThingDef)
			{
				if (GenConstruct.CanBuildOnTerrain(place, position, map, Rot4.North) && (proximitySpacing <= 0 || GenClosest.ClosestThing_Global(position, map.listerThings.ThingsOfDef((ThingDef)place), proximitySpacing) == null))
				{
					while (position.GetThingList(map).Count > 0)
					{
						position.GetThingList(map)[0].Destroy();
					}
					RoadDefGenStep_DryWithFallback.PlaceWorker(map, position, TerrainDefOf.Gravel);
					GenSpawn.Spawn(ThingMaker.MakeThing((ThingDef)place), position, map);
				}
			}
			else
			{
				Log.ErrorOnce($"Can't figure out how to place object {place} while building road", 10785584);
			}
		}
	}
}