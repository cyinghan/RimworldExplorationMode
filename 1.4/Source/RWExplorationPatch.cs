﻿using System;
using Verse;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
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
        
        [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
        public class TickManager_DoSingleTick_RWE
        {
            private static float updateInterval = 1250;
            
            static void Postfix(TickManager __instance)
            {
                if (__instance.TicksGame % updateInterval == 0 && VisibilityManager.updateTracker.Count > 0)
                {
                    foreach (WorldObject obj in VisibilityManager.updateTracker)
                    {
                        VisibilityManager.RevealWithObject(obj, 7);
                    }
                    VisibilityManager.updateTracker.Clear();
                    foreach (WorldObject mp in Find.WorldObjects.MapParents)
                    {
                        if (VisibilityManager.IsObjectVisible(mp))
                        {
                            VisibilityManager.AddObject(mp);
                            VisibilityManager.Founded(mp);
                        }
                    }
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
                    if (__instance.LabelCap == "Colony 2") return;
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
                        VisibilityManager.UpdateGraphics();
                    }
                    if (__instance.LabelCap == "Satellite")
                    {
                        VisibilityManager.hasSatelite = true;
                        VisibilityManager.Scan();
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
                    if (__instance.LabelCap == "Satellite")
                    {
                        VisibilityManager.hasSatelite = false;
                        foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
                        {
                            // check remaining satellites
                            
                            if (obj.LabelCap == "Satellite" && obj != __instance)
                            {
                                VisibilityManager.hasSatelite = true;
                                break;
                            }
                        }
                    }
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
                        VisibilityManager.RefreshObject(__instance);
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
        
        // Touches moving world objects
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Draw))]
        public class WorldObject_Draw_RWE
        {
            static bool Prefix(WorldObject __instance)
            {
                if (__instance!=null && !VisibilityManager.IsObjectVisible(__instance))
                {
                    return false;
                }
                return true;
            }
        }
        
        // Touches stationary world objects
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Print))]
        public class WorldObject_Print_RWE
        {
            static bool Prefix(WorldObject __instance)
            {
                if (__instance!=null && !VisibilityManager.IsObjectVisible(__instance))
                {
                    return false;
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
            static void Postfix(WorldSelector __instance)
            {
                if (Current.ProgramState==ProgramState.Entry)
                {
                    VisibilityManager.ResetExplore();
                    VisibilityManager.RevealInit(__instance.selectedTile, 7);
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
                    VisibilityManager.RefreshObject(map.Parent);
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.StandardizeTileData))]
        public class WorldGrid_StandardizeTileData_RWE
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
                    VisibilityManager.hasSatelite = true;
                    VisibilityManager.Scan();
                }
            }
        }
        
        [HarmonyPatch(typeof(PawnInventoryGenerator), nameof(PawnInventoryGenerator.GenerateInventoryFor), new Type[]{typeof(Pawn), typeof(PawnGenerationRequest)})]
        public class PawnInventoryGenerator_GenerateInventoryFor_RWE
        {
            private static Random rand = new Random();
            static void Postfix(Pawn p, PawnGenerationRequest request)
            {
                if (rand.NextDouble() < 0.02f && p.RaceProps.Humanlike && p.ageTracker.AgeBiologicalYears > 7)
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
                                VisibilityManager.RefreshObject(obj);
                            }
                        }
                    }
                    VisibilityManager.UpdateGraphics();
                }
            }
        }
        
        // [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ChoicesAtFor))]
        // public class FloatMenuMakerMap_ChoicesAtFor_RWE
        // {
        //     static void Postfix(Pawn p, ref List<FloatMenuOption> __result)
        //     {
        //         FloatMenuOption option = new FloatMenuOption("Interrogate", () => GiveInterrogationJob(p));
        //         __result.Add();
        //     }
        //
        //     static void GiveInterrogationJob()
        //     {
        //         
        //     }
        // }
    }
}