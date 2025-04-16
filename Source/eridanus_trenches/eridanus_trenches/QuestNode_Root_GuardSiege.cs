using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using System.Collections.Generic;

namespace eridanus_trenches
{
    public class QuestNode_Root_GuardSiege : QuestNode_MultiSite
    {
        public override void RunInt()
        {
            if (!PrepareQuest(out Quest quest, out Slate slate, out Map map, out float points, out int tile, delegate (int x)
            {
                return Find.WorldGrid[x].hilliness == Hilliness.Flat;
            }))
            {
                Log.Error("Failed to find a suitable site tile for the siege quest.");
                return;
            }
            var hostileFaction = Faction.OfMechanoids ?? Find.FactionManager.RandomEnemyFaction(allowNonHumanlike: false);
            var site = GenerateSite(quest, slate, points, tile, hostileFaction,
            out string siteMapGeneratedSignal, failWhenMapRemoved: false);
            
            List<int> tileNeighbors = new List<int>();
            tileNeighbors.Add(tile);
            addTrenchLineAround(tileNeighbors);

            // Sort them by distance relative to each other

        }

        // tiles is the tiles within the trench line, whether it be another trench line or a city within
        public void addTrenchLineAround(List<int> tiles)
        {
            List<int> trenchTiles = new List<int>();
            foreach(int tile in tiles)
            {
                List<int> tileNeighbors = new List<int>();
                Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
                foreach(int tile2 in tileNeighbors)
                {
                    if(!tiles.Contains(tile2) && !trenchTiles.Contains(tile2))
                    {
                        trenchTiles.Add(tile2);
                    }
                }
            }

            WorldGrid worldGrid = Find.WorldGrid;
            int j = 1;
            for (int i = 0; i < trenchTiles.Count - 1; i++)
            {
                worldGrid.OverlayRoad(trenchTiles[i], trenchTiles[j], RoadDefOf.AncientAsphaltHighway);
                j++;
                if (j >= trenchTiles.Count)
                {
                    j = 0;
                }
            }
        }

        public override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}