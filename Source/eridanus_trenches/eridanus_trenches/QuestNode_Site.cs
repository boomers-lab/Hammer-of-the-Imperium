using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace eridanus_trenches
{
    public abstract class QuestNode_MultiSite : QuestNode
    {
        public SitePartDef QuestSite { get; }
        protected bool TryFindSiteTile(out int tile, Predicate<int> extraValidator = null, List<BiomeDef> allowedBiomes = null)
        {
            if (allowedBiomes != null && Find.WorldGrid.tiles.Any(x => allowedBiomes.Contains(x.biome)) is false)
            {
                allowedBiomes = null;
            }
            var tiles = Find.World.tilesInRandomOrder.Tiles.Where((int x) => (extraValidator == null || extraValidator(x))
                && IsValidTile(x, allowedBiomes));
            if (tiles.TryRandomElement(out tile))
            {
                return true;
            }
            tile = -1;
            return false;
        }

        public static bool IsValidTile(int tile, List<BiomeDef> allowedBiomes = null)
        {
            Tile tile2 = Find.WorldGrid[tile];
            if (!tile2.biome.canBuildBase)
            {
                return false;
            }
            if (!tile2.biome.implemented)
            {
                return false;
            }
            if (tile2.hilliness == Hilliness.Impassable)
            {
                return false;
            }
            if (Find.WorldObjects.AnyMapParentAt(tile) || Current.Game.FindMap(tile) != null
            || Find.WorldObjects.AnyWorldObjectOfDefAt(WorldObjectDefOf.AbandonedSettlement, tile))
            {
                return false;
            }
            if (allowedBiomes != null && allowedBiomes.Count > 0 && !allowedBiomes.Contains(tile2.biome))
            {
                return false;
            }
            return true;
        }

        // Check to make sure this won't overwrite any existing settlements
        public static bool CheckTileNeighbors(int tile, int dist)
        {
            List<int> tileNeighborsMaster = new List<int>();
            WorldGrid worldGrid = Find.WorldGrid;

            if (Find.WorldObjects.AnySettlementBaseAt(tile))
            {
                return false;
            }

            tileNeighborsMaster.Add(tile); // add center tile so its not checked

            List<int> tileNeighbors = new List<int>();
            Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
            Queue<int> tilesLeft = new Queue<int>();
            foreach (int item in tileNeighbors) {
                tilesLeft.Enqueue(item);
            }

            while(tilesLeft.Count > 0)
            {
                int item = tilesLeft.Dequeue();
                if (!tileNeighborsMaster.Contains(item))
                {
                    if (Find.WorldObjects.AnySettlementBaseAt(item))
                    {
                        return false;
                    }
                    else
                    {
                        tileNeighborsMaster.Add(item);
                        Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
                        foreach (int item2 in tileNeighbors)
                        {
                            // doesn't check twice, and checks it within the distance
                            if (!tileNeighborsMaster.Contains(item) && Find.WorldGrid.TraversalDistanceBetween(tile, item2) < dist)
                            {
                                tilesLeft.Enqueue(item2);
                            }
                        }
                        tileNeighbors.Clear();
                    }
                }
            }
            return true;
        }

        public override bool TestRunInt(Slate slate)
        {
            return TryFindSiteTile(out _);
        }

        protected Site GenerateSite(Quest quest, Slate slate, float points,
            int tile, Faction parentFaction, out string siteMapGeneratedSignal, bool failWhenMapRemoved = true)
        {
            SitePartParams sitePartParams = new SitePartParams
            {
                points = points,
                threatPoints = points
            };

            Site site = QuestGen_Sites.GenerateSite(new List<SitePartDefWithParams>
            {
                new SitePartDefWithParams(QuestSite, sitePartParams)
            }, tile, parentFaction);

            site.doorsAlwaysOpenForPlayerPawns = false;
            if (parentFaction != null && site.Faction != parentFaction)
            {
                site.SetFaction(parentFaction);
            }
            slate.Set("site", site);
            quest.SpawnWorldObject(site);

            string siteMapRemovedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
            siteMapGeneratedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");

            if (failWhenMapRemoved)
            {
                quest.SignalPassActivable(delegate
                {
                    quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
                }, siteMapGeneratedSignal, siteMapRemovedSignal);
            }
            return site;
        }

        protected Faction CreateFaction(Quest quest, Slate slate, FactionDef factionDef)
        {
            FactionGeneratorParms parms = new FactionGeneratorParms(factionDef, default(IdeoGenerationParms), true);
            parms.ideoGenerationParms = new IdeoGenerationParms(parms.factionDef);
            Faction parentFaction = FactionGenerator.NewGeneratedFaction(parms);
            parentFaction.temporary = true;
            parentFaction.factionHostileOnHarmByPlayer = true;
            parentFaction.neverFlee = true;
            Find.FactionManager.Add(parentFaction);
            quest.ReserveFaction(parentFaction);
            slate.Set("parentFaction", parentFaction);
            slate.Set("siteFaction", parentFaction);
            return parentFaction;
        }

        protected bool PrepareQuest(out Quest quest, out Slate slate, out Map map, out float points,
        out int tile, Predicate<int> extraValidator = null, List<BiomeDef> allowedBiomes = null)
        {
            quest = QuestGen.quest;
            slate = QuestGen.slate;
            map = QuestGen_Get.GetMap();
            QuestGenUtility.RunAdjustPointsForDistantFight();
            points = slate.Get("points", 0f);
            slate.Set("playerFaction", Faction.OfPlayer);
            slate.Set("map", map);
            if (!TryFindSiteTile(out tile, extraValidator, allowedBiomes))
            {
                return false;
            }
            return true;
        }

        public static List<PawnKindDef> GeneratePawnKindList(Faction faction, float points, Site site)
        {
            PawnGroupMakerParms pawnGroupMakerParms = GetParms(faction, points, site);
            var minPoints = faction.def.MinPointsToGeneratePawnGroup(pawnGroupMakerParms.groupKind, pawnGroupMakerParms);
            points = minPoints < float.MaxValue ? Mathf.Max(points, minPoints) : points;
            pawnGroupMakerParms.points = points;
            List<PawnKindDef> generatedPawns = new List<PawnKindDef>();
            while (generatedPawns.Any() is false && points < 99999)
            {
                points += 50f;
                pawnGroupMakerParms.points = points;
                generatedPawns = GeneratePawnKinds(pawnGroupMakerParms, false).ToList();
            }
            return generatedPawns;
        }
        public static IEnumerable<PawnKindDef> GeneratePawnKinds(PawnGroupMakerParms parms, bool warnOnZeroResults = true)
        {
            if (parms.groupKind == null)
            {
                Log.Error("Tried to generate pawns with null pawn group kind def. parms=" + parms);
                yield break;
            }
            if (parms.faction == null)
            {
                Log.Error("Tried to generate pawn kinds with null faction. parms=" + parms);
                yield break;
            }
            if (parms.faction.def.pawnGroupMakers.NullOrEmpty())
            {
                Log.Error(string.Concat("Faction ", parms.faction, " of def ", parms.faction.def, " has no PawnGroupMakers."));
                yield break;
            }
            if (!PawnGroupMakerUtility.TryGetRandomPawnGroupMaker(parms, out var pawnGroupMaker))
            {
                yield break;
            }
            foreach (PawnKindDef item in pawnGroupMaker.GeneratePawnKindsExample(parms))
            {
                yield return item;
            }
        }
        private static PawnGroupMakerParms GetParms(Faction faction, float points, Site site)
        {
            return new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = site.Tile,
                faction = faction,
                points = points,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack
            };
        }

        protected string FormatPawnListToString(List<PawnKindDef> pawns)
        {
            if (pawns == null || !pawns.Any())
            {
                return "";
            }
            return pawns.GroupBy(p => p).Select(group => $"{group.Count()} {group.Key.label}").ToCommaList();
        }
    }
}