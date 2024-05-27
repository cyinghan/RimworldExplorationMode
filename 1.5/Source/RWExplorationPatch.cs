using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace RimworldExploration
{
    public class RimworldExploration
    {
        [StaticConstructorOnStartup]
        public class Main
        {
            static Main()
            {
                var harmony = new Harmony("Harmony_RimworldExploration");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Tile), MethodType.Setter)]
        public class WorldObject_SetTile_RWE
        {
            static void Postfix(WorldObject __instance)
            {
                if (__instance!=null && VisibilityManager.IsFollowed(__instance))
                {
                    if (!VisibilityManager.updateTracker.Contains(__instance))
                        VisibilityManager.updateTracker.Add(__instance);
                }
            }
        }
        
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Tick))]
        public class WorldObject_DynamicAerialVehicleCheck_RWE
        {
            private static Dictionary<int, int> AirUnitUpdateTracker = new Dictionary<int, int>();
            private static int defaultAirUpdateInterval = 100;
            static void Postfix(WorldObject __instance)
            {
                if (__instance!=null && __instance.GetType().ToString() == "Vehicles.AerialVehicleInFlight")
                {
                    if (!AirUnitUpdateTracker.ContainsKey(__instance.ID))
                    {
                        AirUnitUpdateTracker[__instance.ID] = 0;
                    }

                    if (AirUnitUpdateTracker[__instance.ID] > 0)
                    {
                        AirUnitUpdateTracker[__instance.ID]--;
                        return;
                    }

                    float minDist = 99999;
                    int closestTileID = __instance.Tile;
                    List<int> closeTiles = VisibilityManager.GetNeighborWithin(__instance.Tile, 5);
                    foreach (int tileID in closeTiles)
                    {
                        Vector3 tilePos = Find.WorldGrid.GetTileCenter(tileID);
                        float distance = Vector3.Distance(__instance.DrawPos, tilePos);
                        if (minDist > distance)
                        {
                            minDist = distance;
                            closestTileID = tileID;
                        }
                    }

                    int DefaultAirSightRange = 10;
                    if (__instance.Tile != closestTileID)
                    {
                        __instance.Tile = closestTileID;
                        VisibilityManager.RevealWithObject(__instance, DefaultAirSightRange, true);
                        VisibilityManager.UpdateStaticWorldObjects();
                        VisibilityManager.UpdateGraphics();
                        AirUnitUpdateTracker[__instance.ID] = defaultAirUpdateInterval;
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Current), nameof(Current.ProgramState), MethodType.Setter)]
        public class Current_ProgramStateInit_RWE
        {
            static void Prefix(ref ProgramState value)
            {
                if (value!=ProgramState.Entry)
                {
                    VisibilityManager.MassCheckTile();
                    VisibilityManager.SetUpdateType(TileUpdateType.Full);
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
        public class TickManager_DoSingleTick_RWE
        {
            private static float updateInterval = 2500;
            
            static void Postfix(TickManager __instance)
            {
                if (__instance.TicksGame % updateInterval == 0 && VisibilityManager.updateTracker.Count > 0)
                {
                    foreach (WorldObject obj in VisibilityManager.updateTracker)
                    {
                        int defaultRange = 7;
                        if (obj.GetType().ToString() == "Vehicle.AerialVehicleInFlight")
                        {
                            defaultRange = 9;
                        }
                        VisibilityManager.RevealWithObject(obj, defaultRange);
                    }
                    VisibilityManager.updateTracker.Clear();
                    VisibilityManager.UpdateStaticWorldObjects();
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.PostAdd))]
        public class WorldObject_PostAdd_RWE
        {
            static void Postfix(WorldObject __instance)
            {
                if (__instance != null)
                {
                    if (!__instance.HasName) return;
                    VisibilityManager.AddObject(__instance);
                    if (__instance.Faction != null)
                    {
                        if (VisibilityManager.Trackable(__instance) || 
                            ( __instance.Faction.IsPlayer && __instance.GetType()==typeof(Settlement)))
                        {
                            VisibilityManager.RevealWithObject(__instance, 7);
                        }
                        if (VisibilityManager.IsObjectVisible(__instance) || Find.WorldObjects.AnyDestroyedSettlementAt(__instance.Tile))
                        {
                            VisibilityManager.Founded(__instance);
                        }
                    }
                    VisibilityManager.UpdateGraphics();
                    if (__instance.LabelCap == "Satellite")
                    {
                        VisibilityManager.ScanAllWithObject(__instance);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.PostRemove))]
        public class WorldObject_PostRemove_RWE
        {
            static void Postfix(WorldObject __instance)
            {
                if (__instance!=null)
                {
                    VisibilityManager.RemoveObject(__instance);
                }
                VisibilityManager.UpdateGraphics();
            }
        }
        
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Destroy))]
        public class WorldObject_Destroy_RWE
        {
            static void Postfix(WorldObject __instance)
            {
                if (__instance!=null)
                {
                    VisibilityManager.RemoveObject(__instance);
                }
                VisibilityManager.UpdateGraphics();
            }
        }
        
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.SetFaction))]
        public class WorldObject_SetFaction_RWE
        {
            static void Postfix(WorldObject __instance)
            {
                VisibilityManager.AddObject(__instance);
                if (__instance != null && VisibilityManager.objectTracker!=null)
                {
                    if (VisibilityManager.Trackable(__instance))
                    {
                        VisibilityManager.AddObject(__instance);
                        VisibilityManager.RevealWithObject(__instance, 7);
                    }
                    else
                    {
                        VisibilityManager.ResetObject(__instance);
                    }
                }
                VisibilityManager.UpdateGraphics();
            }
        }
            
        [HarmonyPatch(typeof(GenWorldUI), nameof(GenWorldUI.WorldObjectsUnderMouse))]
        public class GenWorldUI_WorldObjectsUnderMouse_RWE
        {
            static void Postfix(ref List<WorldObject> __result)
            {
                if (__result.Count > 0 && (!VisibilityManager.IsObjectVisible(__result[0])))
                {
                    __result.Clear();
                }
            }
        }
        
        // Moving world objects
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Draw))]
        public class WorldObject_Draw_RWE
        {
            static bool Prefix(WorldObject __instance)
            {
                if (__instance!=null && __instance.GetType().ToString() == "Vehicles.AerialVehicleInFlight")
                {
                    float minDist = 99999;
                    int closestTileID = __instance.Tile;
                    List<int> closeTiles = VisibilityManager.GetNeighborWithin(__instance.Tile, 9);
                    foreach (int tileID in closeTiles)
                    {
                        Vector3 tilePos = Find.WorldGrid.GetTileCenter(tileID);
                        float distance = Vector3.Distance(__instance.DrawPos, tilePos);
                        if (minDist > distance)
                        {
                            minDist = distance;
                            closestTileID = tileID;
                        }
                    }
                    __instance.Tile = closestTileID;
                }
                if (__instance!=null && !VisibilityManager.IsObjectVisible(__instance))
                {
                    return false;
                }
                return true;
            }
        }
        
        // Stationary world objects
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Print))]
        public class WorldObject_Print_RWE
        {
            static bool Prefix(WorldObject __instance)
            {
                if (__instance!=null && !VisibilityManager.IsObjectVisible(__instance))
                {
                    if (__instance.questTags.NullOrEmpty())
                    {
                        return false;
                    }
                    VisibilityManager.RevealAt(__instance, 1);
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WorldObjectSelectionUtility), nameof(WorldObjectSelectionUtility.HiddenBehindTerrainNow))]
        public class WorldObjectSelectionUtility_HiddenBehindTerrainNow_RWE
        {
            static bool Prefix(WorldObject o, ref bool __result)
            {
                if (o!=null && !VisibilityManager.IsObjectVisible(o))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WorldSelector), nameof(WorldSelector.Select))]
        public class WorldSelector_Select_RWE
        {
            static bool Prefix(WorldObject obj)
            {
                if (!VisibilityManager.IsObjectVisible(obj))
                {
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WorldInspectPane), nameof(WorldInspectPane.CurTabs), MethodType.Getter)]
        public class WorldInspectPane_CurTabs_RWE
        {
            static bool Prefix()
            {
                if (!(Find.WorldSelector.SelectedObjects.Count > 0) && 
                    !VisibilityManager.TileExplored(Find.WorldSelector.selectedTile))
                {
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WorldInspectPane), nameof(WorldInspectPane.GetLabel))]
        public class WorldInspectPane_GetLabel_RWE
        {
            static bool Prefix(ref string __result)
            {
                if (!(Find.WorldSelector.SelectedObjects.Count > 0) && 
                    !VisibilityManager.TileExplored(Find.WorldSelector.selectedTile))
                {
                    __result = Translator.Translate("RWE_Unexplored");
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WorldInspectPane), nameof(WorldInspectPane.DoPaneContents))]
        public class WorldInspectPane_DoPaneContentsFor_RWE
        {
            static bool Prefix()
            {
                if (!(Find.WorldSelector.SelectedObjects.Count > 0) && 
                    !VisibilityManager.TileExplored(Find.WorldSelector.selectedTile))
                {
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(WorldSelector), "SelectUnderMouse")]
        public class WorldSelector_SelectUnderMouse_RWE
        {
            private static List<int> trackedTileIDs;
            static void Postfix(WorldSelector __instance, bool canSelectTile)
            {
                if (Current.ProgramState==ProgramState.Entry && canSelectTile)
                {
                    if (trackedTileIDs==null) trackedTileIDs = new List<int>();
                    foreach (int tileID in trackedTileIDs)
                    {
                        VisibilityManager.ResetTile(tileID);
                    }
                    trackedTileIDs.Clear();
                    WorldObject obj = __instance.SingleSelectedObject;
                    if (obj == null)
                    {
                        VisibilityManager.RevealInit(__instance.selectedTile, 7, ref trackedTileIDs);
                    }
                    else
                    {
                        VisibilityManager.RevealInit(obj.Tile, 7, ref trackedTileIDs);
                    }
                    VisibilityManager.UpdateGraphics();
                }
            }
        }

        [HarmonyPatch(typeof(WorldPathGrid), nameof(WorldPathGrid.CalculatedMovementDifficultyAt))]
        public class WorldPathGrid_CalculatedMovementDifficultyAt_RWE
        {
            static void Postfix(int tile, ref float __result)
            {
                Tile tileObj = Find.WorldGrid[tile];
                if (tileObj.Rivers != null && tileObj.Roads == null)
                {
                    float riverDifficulty = 0f;
                    foreach (var riverLink in tileObj.Rivers)
                    {
                        riverDifficulty += riverLink.river.widthOnWorld * 2f;
                    }
                    __result += riverDifficulty;
                }
            }
        }
        
        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.RegisterPawn))]
        public class MapPawns_RegisterPawn_RWE
        {
            static void Postfix(MapPawns __instance, Pawn p)
            {
                Map map = Traverse.Create(__instance).Field("map").GetValue() as Map;
                if (p.IsFreeColonist && map!=null && VisibilityManager.Trackable(map.Parent))
                {
                    VisibilityManager.Trackable(map.Parent);
                    VisibilityManager.RevealWithObject(map.Parent, 7);
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.DeRegisterPawn))]
        public class MapPawns_DeRegisterPawn_RWE
        {
            static void Postfix(MapPawns __instance, Pawn p)
            {
                Map map = Traverse.Create(__instance).Field("map").GetValue() as Map;
                if (p.IsFreeColonist && map!=null && !VisibilityManager.Trackable(map.Parent))
                {
                    VisibilityManager.ResetObject(map.Parent);
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(Caravan), nameof(Caravan.RemovePawn))]
        public class Caravan_RemovePawn_RWE
        {
            static void Postfix(Caravan __instance, Pawn p)
            {
                if (p.IsFreeColonist && !VisibilityManager.Trackable(__instance))
                {
                    VisibilityManager.ResetObject(__instance);
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(Caravan), nameof(Caravan.AddPawn))]
        public class Caravan_AddPawn_RWE
        {
            static void Postfix(Caravan __instance, Pawn p)
            {
                if (p.IsFreeColonist && VisibilityManager.Trackable(__instance))
                {
                    VisibilityManager.Trackable(__instance);
                    VisibilityManager.RevealWithObject(__instance, 7);
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(WorldGenStep_Terrain), "GenerateGridIntoWorld")]
        public class WorldComponent_FinalizeInit_RWE
        {
            static void Postfix()
            {
                VisibilityManager.Initialize();
            }
        }
        
        [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
        public class ResearchManager_FinishProject_RWE
        {
            static void Postfix(ResearchProjectDef proj)
            {
                if (proj == ResearchProjectDefOf.RWE_SateliteHacking)
                {
                    VisibilityManager.revealAll = true;
                    VisibilityManager.RevealWorld();
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(PawnInventoryGenerator), nameof(PawnInventoryGenerator.GenerateInventoryFor), new Type[]{typeof(Pawn), typeof(PawnGenerationRequest)})]
        public class PawnInventoryGenerator_GenerateInventoryFor_RWE
        {
            private static Random rand = new Random();
            static void Postfix(Pawn p, PawnGenerationRequest request)
            {
                if (p.Faction != null && p.RaceProps != null && rand.NextDouble() < 0.02f && p.RaceProps.Humanlike && 
                    p.ageTracker.AgeBiologicalYears > 9 && !p.Faction.IsPlayer)
                {
                    ThingDef MapDef = DefDatabase<ThingDef>.GetNamed("RWE_Map");
                    Thing mapThing = ThingMaker.MakeThing(MapDef);
                    p.inventory.innerContainer.TryAdd(mapThing);
                }
            }
        }
        
        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_RelationKindChanged))]
        public class Faction_Notify_RelationKindChanged_RWE
        {
            static void Postfix(Faction __instance, Faction other, FactionRelationKind previousKind)
            {
                if (Current.ProgramState==ProgramState.Playing && other.IsPlayer && !(previousKind < FactionRelationKind.Ally && __instance.PlayerRelationKind < FactionRelationKind.Ally))
                {
                    if (__instance.PlayerRelationKind == FactionRelationKind.Ally)
                    {
                        foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
                        {
                            if (obj.Faction == __instance)
                            {
                                VisibilityManager.Follow(obj);
                                VisibilityManager.RevealWithObject(obj, 7);
                            }
                        }
                    }
                    else
                    {
                        foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
                        {
                            if (obj.Faction == __instance)
                            {
                                VisibilityManager.Follow(obj, false);
                                VisibilityManager.ResetObject(obj);
                            }
                        }
                    }
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted))]
        public class InteractionWorker_DoRecruit_RWE
        {
            static void Prefix(Pawn initiator, Pawn recipient)
            {
                
                if (recipient.Faction!=null && recipient.Faction != initiator.Faction && recipient.guest.resistance<=0)
                {
                    // PawnMapKnowledgeManager.Add(recipient);
                    List<Settlement> unknownSettlements = Find.WorldObjects.Settlements.FindAll(s=> !VisibilityManager.IsFounded(s));
                    var random = new Random();
                    IEnumerable<Settlement> selectedSettlements = unknownSettlements.OrderBy(x => random.Next()).Take(1);
                    foreach (var stmt in selectedSettlements)
                    {
                        VisibilityManager.RevealAt(stmt, 3);
                        Message msg = new Message(Translator.Translate("RWE_RevealedLocationByRecruitment").Formatted(stmt.LabelCap, recipient.LabelCap), MessageTypeDefOf.PositiveEvent);
                        Messages.Message(msg);
                    }
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        // [HarmonyPatch(typeof(Pawn), nameof(Pawn.Discard))]
        // public class Pawn_Discard_RWE
        // {
        //     static void Postfix(Pawn __instance)
        //     {
        //         PawnMapKnowledgeManager.Remove(__instance);
        //     }
        // }
        
        
        
        // [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        // public class FloatMenuMakerMap_AddInterrogation_RWE
        // {
        //     static TargetingParameters tp = new TargetingParameters 
        //     {
        //         canTargetPawns = true,
        //         canTargetBuildings = false,
        //         canTargetMechs = false,
        //         onlyTargetPrisonersOfColony = true,
        //         validator = delegate(TargetInfo targ)
        //         {
        //             if (!(targ.Thing is Pawn pawn2))
        //             {
        //                 return false;
        //             }
        //             return pawn2.IsPrisonerOfColony && pawn2.guest.PrisonerIsSecure;
        //         }
        //     };
        //     
        //     static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        //     {
        //         
        //         if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        //         {
        //             
        //             foreach (LocalTargetInfo prisoner in GenUI.TargetsAt(clickPos, tp, thingsOnly: true))
        //             {
        //                 Pawn pTarg2 = (Pawn)prisoner.Thing;
        //                 Action action = delegate
        //                 {
        //                     JobDef jobdef = new JobDef();
        //                     jobdef.driverClass = typeof(JobDriver_InterrogatePrisonerForMap);
        //                     Job job = JobMaker.MakeJob(jobdef, pTarg2);
        //                     job.count = 1;
        //                     pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        //                 };
        //                 opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
        //                     new FloatMenuOption("RWE_Interrogate".Translate(prisoner.Thing.LabelCap), action,
        //                         MenuOptionPriority.High, null, prisoner.Thing), pawn, pTarg2));
        //             }
        //         }
        //     }
        // }
        
        [HarmonyPatch(typeof(WorldFeatures), "UpdateAlpha")]
        public class WorldFeatures_ShouldShowFeature_RWE
        {
            static bool Prefix(WorldFeatures __instance, WorldFeature feature)
            {
                WorldFeatureManager manager = Find.World.GetComponent<WorldFeatureManager>();
                int index = __instance.features.FindIndex(a => a==feature);
                if (!manager.learnedFeatures[index])
                {
                    feature.alpha = 0;
                    return false;
                }
                return true;
            }
        }
        
        
        // [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
        // public class Pawn_InteractionsTracker_TryInteractWith_RWE
        // {
        //     private static Random random = new Random();
        //     static void Postfix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef, ref bool __result)
        //     {
        //         Pawn actor = Traverse.Create(__instance).Field("pawn").GetValue() as Pawn;
        //         if (__result && actor.Faction!=null && recipient.Faction!=null && 
        //             actor.Faction.IsPlayer && !recipient.Faction.IsPlayer)
        //         {
        //             double chance = random.NextDouble();
        //             WorldFeatureManager manager = Find.World.GetComponent<WorldFeatureManager>();
        //             if (chance < 0.08f * manager.GetRevealChance(recipient))
        //             {
        //                 int index = random.Next(manager.learnedFeatures.Count);
        //                 manager.IncrementReveal(recipient);
        //                 if (!manager.learnedFeatures[index])
        //                 {
        //                     manager.learnedFeatures[index] = true;
        //                     Message msg = new Message(
        //                         Translator.Translate("RWE_InteractionRevealedWorldFeature").Formatted(actor.LabelCap,
        //                             recipient.LabelCap, Find.World.features.features[index].name),
        //                         MessageTypeDefOf.PositiveEvent);
        //                     Messages.Message(msg);
        //                 }
        //             }
        //         }
        //     }
        // }
        
        // [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.Notify_PawnRecruited))]
        // public class Pawn_InteractionsTracker_Notify_PawnRecruited_RWE
        // {
        //     private static Random random = new Random();
        //     static void Postfix(Pawn_GuestTracker __instance)
        //     {
        //         Pawn recruitee = Traverse.Create(__instance).Field("pawn").GetValue() as Pawn;
        //         WorldFeatureManager manager = Find.World.GetComponent<WorldFeatureManager>();
        //         for (int i = 0; i < 5; i++)
        //         {
        //             double chance = random.NextDouble();
        //             if (chance < 0.08f * manager.GetRevealChance(recruitee))
        //             {
        //                 int index = random.Next(manager.learnedFeatures.Count);
        //                 manager.IncrementReveal(recruitee);
        //                 if (!manager.learnedFeatures[index])
        //                 {
        //                     manager.learnedFeatures[index] = true;
        //                     Message msg = new Message(
        //                         Translator.Translate("RWE_RecruiteeRevealedWorldFeature").Formatted(
        //                             recruitee.LabelCap, Find.World.features.features[index].name),
        //                         MessageTypeDefOf.PositiveEvent);
        //                     Messages.Message(msg);
        //                 }
        //             }
        //         }
        //     }
        // }
    }
}