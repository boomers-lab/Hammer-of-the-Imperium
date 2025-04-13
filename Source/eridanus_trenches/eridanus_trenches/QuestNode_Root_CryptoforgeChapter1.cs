using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using System.Collections.Generic;

namespace eridanus_trenches
{
    public class QuestNode_Root_GuardSiege : QuestNode_Site
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
            WorldGrid worldGrid = Find.WorldGrid;
            Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
            int j = 1;
            for (int i = 0; i < tileNeighbors.Count-1; i++)
            {
                var path = Find.WorldPathFinder.FindPath(tileNeighbors[i], tileNeighbors[j], null);
                worldGrid.OverlayRoad(path.Peek(i), path.Peek(j), RoadDefOf.AncientAsphaltHighway);
                j++;
                if (j >= tileNeighbors.Count)
                {
                    j = 0;
                }
            }

            QuestPart_EndQuestOnScanSignals questPart_ScanSignalsCounter = new QuestPart_EndQuestOnScanSignals();
            questPart_ScanSignalsCounter.mapParent = site;
            questPart_ScanSignalsCounter.inSignalEnable = siteMapGeneratedSignal;
            questPart_ScanSignalsCounter.scanningBuilding = InternalDefOf.VQE_FrozenScanningRelay;
            questPart_ScanSignalsCounter.maxSignalsCount = 2;
            questPart_ScanSignalsCounter.inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.Scanned_" + InternalDefOf.VQE_FrozenScanningRelay.defName);
            quest.AddPart(questPart_ScanSignalsCounter);
        }

        public override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}