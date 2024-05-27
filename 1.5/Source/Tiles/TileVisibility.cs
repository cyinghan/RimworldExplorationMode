using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimworldExploration.Layer;
using RimworldExplorationMode.Settings;
using RimworldExplorationMode.Integration;
using Outposts;
using Verse;
using HarmonyLib;

namespace RimworldExploration
{
    public enum TileUpdateType
    {
        None=0,
        Fog=1,
        Full=2,
        Planet=3
    }
    
    public class TileVisibility : IExposable
    {
        private bool explored;
        private bool visible;
        private List<int> beholders;
        private int tileID = -1;

        public bool Visible
        {
            get { return visible || VisibilityManager.revealAll; }
            set
            {
                if (VisibilityManager.TileExplored(tileID) && VisibilityManager.Precheck_TileID_Fog!=null) 
                    VisibilityManager.Precheck_TileID_Fog.Add(tileID);
                visible = value;
            }
        }
        
        public bool Explored
        {
            get { return explored || VisibilityManager.revealAll; }
            set
            {
                if (VisibilityManager.Precheck_TileID_Explored!=null) 
                    VisibilityManager.Precheck_TileID_Explored.Add(tileID);
                explored = value;
            }
        }
        
        public TileVisibility()
        {
        }
        public TileVisibility(int id)
        {
            tileID = id;
            DefaultInit();
        }

        private void DefaultInit()
        {
            SetAllVisionValues(false);
            beholders = new List<int>();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref tileID, "RWE_visTileID");
            Scribe_Values.Look(ref explored, "RWE_explored");
            Scribe_Values.Look(ref visible, "RWE_visible");
            Scribe_Collections.Look(ref beholders, "RWE_beholders", LookMode.Value);
        }

        public void AddVision(WorldObject obj)
        {
            if (!beholders.Contains(obj.ID))
            {
                if (!Visible) Visible = true;
                if (!Explored) Explored = true;
                beholders.Add(obj.ID);
            }
        }
        
        public void RemoveVision(WorldObject obj)
        {
            beholders.Remove(obj.ID);
            CheckVision();
        }
        
        public void SetAllVisionValues(bool val)
        {
            Visible = val;
            Explored = val;
        }
        
        public void CheckVision()
        {
            if (LoadedModManager.GetMod<RWEMod>().GetSettings<RWEMode_Settings>().DisableFogOfWar &&
                ProgramState.Entry != Current.ProgramState && Explored)
            {
                if (!Visible) Visible = true;
                return;
            }
            if (beholders.NullOrEmpty())
            {
                if (Visible) Visible = false;
            }
            else
            {
                if (!Visible) Visible = true;
            }
        }

        public void SetExploredValue(bool isExplored)
        {
            Explored = isExplored;
            if (VisibilityManager.TileExplored(tileID) && VisibilityManager.Precheck_TileID_Fog!=null) 
                VisibilityManager.Precheck_TileID_Fog.Add(tileID);
        }
    }

    public class WorldObjectVisibility : IExposable
    {   
        public int worldObjectID;
        public bool founded;
        public bool followed;
        public HashSet<int> tileHolder;
        
        public WorldObjectVisibility()
        {
            founded = false;
            followed = false;
            tileHolder = new HashSet<int>();
        }

        public WorldObjectVisibility(WorldObject obj)
        {
            founded = false;
            followed = false;
            tileHolder = new HashSet<int>();
            worldObjectID = obj.ID;
            
            if (obj.Faction!=null && (obj.Faction.IsPlayer || VisibilityManager.OnVisibleTile(obj)))
            {
                founded = true;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref worldObjectID, "RWE_worldObjectID");
            Scribe_Values.Look(ref founded, "RWE_founded");
            Scribe_Values.Look(ref followed, "RWE_tracked");
            Scribe_Collections.Look(ref tileHolder, "RWE_tileHolder", LookMode.Value);
            
        }
    }
    

    public class VisibilityManager : GameComponent
    {
        
        public static bool revealAll;
        public static Dictionary<int, TileVisibility> tileTracker;
        public static Dictionary<int, WorldObjectVisibility> objectTracker;
        public static List<WorldObject> updateTracker;
        public static List<int> Precheck_TileID_Fog;
        public static List<int> Precheck_TileID_Explored;
        public static bool fogInitialized;
        public static bool exploreInitialized;
        public static List<string> ForceTrackWorldObjectClasses = new List<string>(){"Volcano"};
        private static TileUpdateType updateType;
        
        public VisibilityManager(Game game)
        {
            tileTracker = new Dictionary<int, TileVisibility>();
            objectTracker = new Dictionary<int, WorldObjectVisibility>();
            updateTracker = new List<WorldObject>();
            Precheck_TileID_Fog = new List<int>();
            Precheck_TileID_Explored = new List<int>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref updateType, "RWE_updateType");
            Scribe_Values.Look(ref revealAll, "RWE_hasSatelite");
            Scribe_Collections.Look(ref tileTracker, "RWE_tileTracker",LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref objectTracker, "RWE_objectTracker",LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref updateTracker, "RWE_updateTracker", LookMode.Deep);
        }

        public override void StartedNewGame()
        {
            UpdateAllTiles();
            SetUpdateType(TileUpdateType.Full);
            UpdateGraphics();
        }

        public override void LoadedGame()
        {
            UpdateAllTiles();
            SetUpdateType(TileUpdateType.Full);
            UpdateGraphics();
        }

        public static void Initialize()
        {
            updateType = TileUpdateType.None;
            revealAll = false;
            tileTracker.Clear();
            objectTracker.Clear();
            updateTracker.Clear();
            fogInitialized = false;
            exploreInitialized = false;
            TrackAllTiles();
        }

        public static void TrackAllTiles()
        {
            if (tileTracker.NullOrEmpty())
            {
                int maxTiles = Find.WorldGrid.tiles.Count;
                for (int tileID = 0; tileID < maxTiles; tileID++)
                {
                    AddTileToTracker(tileID);
                }
            }
        }

        public static void UpdateAllTiles()
        {
            Precheck_TileID_Explored.Clear();
            Precheck_TileID_Fog.Clear();
            int maxTiles = Find.WorldGrid.tiles.Count;
            for (int tileID = 0; tileID < maxTiles; tileID++)
            {
                Precheck_TileID_Explored.Add(tileID);
                Precheck_TileID_Fog.Add(tileID);
            }
            MassCheckTile();
        }
        
        public static void ResetTile(int tileID)
        {
            tileTracker[tileID].SetAllVisionValues(false);
        }
        
        public static void MassCheckTile()
        {
            foreach (var tile in tileTracker)
            {
                tile.Value.CheckVision();
            }
        }

        public static bool IsFollowed(WorldObject obj)
        {
            if (objectTracker.ContainsKey(obj.ID))
                return objectTracker[obj.ID].followed;
            return false;
        }
        
        public static void Follow(WorldObject obj, bool val = true)
        {
            AddObject(obj);
            objectTracker[obj.ID].followed = val;
            if (val)
            {
                objectTracker[obj.ID].founded = val;
            }
        }

        public static void AddTileToTracker(int tileID)
        {
            tileTracker[tileID] = new TileVisibility(tileID);
        }

        public static void AddObject(WorldObject obj)
        {
            if (!objectTracker.ContainsKey(obj.ID))
            {
                objectTracker[obj.ID] = new WorldObjectVisibility(obj);
            }
        }
        
        public static void RemoveObject(WorldObject obj)
        {
            if (objectTracker.ContainsKey(obj.ID))
            {
                ResetObject(obj);
                objectTracker.Remove(obj.ID);
            }
        }

        public static void SetUpdateType(TileUpdateType newType)
        {
            if (updateType < newType)
            {
                updateType = newType;
            }
        }

        public static void ResetObject(WorldObject obj)
        {
            foreach (int tileID in objectTracker[obj.ID].tileHolder)
            {
                tileTracker[tileID].RemoveVision(obj);
            }
            objectTracker[obj.ID].tileHolder.Clear();
            SetUpdateType(TileUpdateType.Fog);
        }

        public static void LinkObjectToTile(WorldObject obj, int tileID)
        {
            tileTracker[tileID].AddVision(obj);
            objectTracker[obj.ID].tileHolder.Add(tileID);
        }

        public static bool isVisibleBeyond(int target, int observer, float range)
        {
            Tile targetTile = Find.WorldGrid.tiles[target];
            Tile observerTile = Find.WorldGrid.tiles[observer];
            float elevationDiff = (int)targetTile.hilliness - (int)observerTile.hilliness;
            if (elevationDiff > 0) return false;
            float pollutionSum = observerTile.pollution + targetTile.pollution;
            float swampinessSum = observerTile.swampiness + targetTile.swampiness;
            float requiredRange = elevationDiff + pollutionSum + swampinessSum;
            return range - requiredRange > 0;
        }
        
        public static bool FromRimNaut(WorldObject obj)
        {
            if (obj.def!=null)
                return obj.def.defName == "RimNauts2_ObjectHolder";
            return false;
        }

        public static bool IsObjectVisible(WorldObject obj)
        {
            if (IsFollowed(obj) || OnVisibleTile(obj) || FromRimNaut(obj)
                || (!IsWarband(obj) && IsFounded(obj)) || revealAll)
            {
                return true;
            }
            return false;
        }
        
        public static bool OnVisibleTile(WorldObject obj)
        {
            
            if (tileTracker.ContainsKey(obj.Tile))
                return tileTracker[obj.Tile].Visible;
            return false;
        }
        
        public static bool IsFounded(WorldObject obj)
        {
            if (objectTracker.ContainsKey(obj.ID))
                return objectTracker[obj.ID].founded;
            return false;
        }
        
        public static void Founded(WorldObject obj)
        {
            AddObject(obj);
            objectTracker[obj.ID].founded = true;
        }
        
        public static bool TileExplored(int tileID)
        {
            if (LoadedModManager.GetMod<RWEMod>().GetSettings<RWEMode_Settings>().RevealInitialMap && ProgramState.Entry==Current.ProgramState)
                return true;
            if (tileTracker.ContainsKey(tileID) && tileID >= 0)
                return tileTracker[tileID].Explored;
            return false;
        }
        
        public static bool TileVisible(int tileID)
        {
            if (tileID >= 0)
                return tileTracker[tileID].Visible;
            return false;
        }

        public static bool IsWarband(WorldObject obj)
        {
            return obj.GetType().GetMethod("ImmediateAction") != null;
        }

        public static bool IsAlly(WorldObject obj)
        {
            if (obj.Faction != null && !obj.Faction.IsPlayer &&
                obj.Faction.PlayerRelationKind == FactionRelationKind.Ally)
                return true;
            return false;
        }

        public static void UpdateStaticWorldObjects()
        {
            bool newBasesFounded = false;
            foreach (WorldObject mp in Find.WorldObjects.MapParents)
            {
                if (!IsFounded(mp) && IsObjectVisible(mp))
                {
                    Founded(mp);
                    newBasesFounded = true;
                }
            }
            var otherWOs = Find.WorldObjects.AllWorldObjects.Except(Find.WorldObjects.MapParents);
            foreach (WorldObject mp in otherWOs)
            {
                if (ForceTrackWorldObjectClasses.Contains(mp.GetType().Name) && !IsFounded(mp) && IsObjectVisible(mp))
                {
                    Founded(mp);
                    newBasesFounded = true;
                }
            }
            if (newBasesFounded) 
                Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
        }
        

        public static void RevealWithObject(WorldObject obj, int maxRange, bool ignoreTerrain=false)
        {
            if (obj.Tile < 0 || !objectTracker.ContainsKey(obj.ID)) return;
            ResetObject(obj);
            Queue<int> noSearch = new Queue<int>();
            Queue<int> searching = new Queue<int>();
            searching.Enqueue(obj.Tile);
            Tile observerTile = Find.WorldGrid.tiles[obj.Tile];
            int searchRange = maxRange + Math.Max(0, (int)observerTile.hilliness - 1);
            for (int i = searchRange; i > 0; i--)
            {
                Queue<int> nextSearches = new Queue<int>();
                while (!searching.EnumerableNullOrEmpty())
                {
                    int tile = searching.Dequeue();
                    if (!tileTracker[tile].Explored)
                        SetUpdateType(TileUpdateType.Full);
                    else if (!tileTracker[tile].Visible)
                        SetUpdateType(TileUpdateType.Fog);
                    LinkObjectToTile(obj, tile);
                    if (isVisibleBeyond(tile, obj.Tile, i) || ignoreTerrain)
                    {
                        List<int> neighbors = new List<int>();
                        Find.WorldGrid.GetTileNeighbors(tile, neighbors);
                        foreach (int neighbor in neighbors)
                        {
                            if (!noSearch.Contains(neighbor))
                            {
                                nextSearches.Enqueue(neighbor);
                            }
                            noSearch.Enqueue(neighbor);
                        }
                    }
                }
                while (!nextSearches.EnumerableNullOrEmpty())
                {
                    searching.Enqueue(nextSearches.Dequeue());
                }
            }
        }
        
        public static void RevealAt(WorldObject obj, int maxRange)
        {
            if (obj.Tile < 0) return;
            Queue<int> noSearch = new Queue<int>();
            Queue<int> searching = new Queue<int>();
            searching.Enqueue(obj.Tile);
            Founded(obj);
            for (int i = maxRange; i > 0; i--)
            {
                Queue<int> nextSearches = new Queue<int>();
                while (!searching.EnumerableNullOrEmpty())
                {
                    int tile = searching.Dequeue();
                    if (!tileTracker[tile].Explored)
                        SetUpdateType(TileUpdateType.Full);
                    else if (!tileTracker[tile].Visible)
                        SetUpdateType(TileUpdateType.Fog);
                    tileTracker[tile].SetExploredValue(true);
                    List<int> neighbors = new List<int>();
                    Find.WorldGrid.GetTileNeighbors(tile, neighbors);
                    foreach (int neighbor in neighbors)
                    {
                        if (!noSearch.Contains(neighbor))
                        {
                            nextSearches.Enqueue(neighbor);
                        }
                        noSearch.Enqueue(neighbor);
                    }
                }
                while (!nextSearches.EnumerableNullOrEmpty())
                {
                    searching.Enqueue(nextSearches.Dequeue());
                }
            }
        }
        
        public static void RevealInit(int tileID, int maxRange, ref List<int> trackedTiles)
        {
            if (tileID < 0) return;
            Queue<int> noSearch = new Queue<int>();
            Queue<int> searching = new Queue<int>();
            searching.Enqueue(tileID);
            for (int i = maxRange; i > 0; i--)
            {
                Queue<int> nextSearches = new Queue<int>();
                while (!searching.EnumerableNullOrEmpty())
                {
                    int tile = searching.Dequeue();
                    if (!tileTracker[tile].Explored)
                        SetUpdateType(TileUpdateType.Full);
                    else if (!tileTracker[tile].Visible)
                        SetUpdateType(TileUpdateType.Fog);
                    tileTracker[tile].SetAllVisionValues(true);
                    trackedTiles.Add(tile);
                    List<int> neighbors = new List<int>();
                    Find.WorldGrid.GetTileNeighbors(tile, neighbors);
                    foreach (int neighbor in neighbors)
                    {
                        if (!noSearch.Contains(neighbor))
                        {
                            nextSearches.Enqueue(neighbor);
                        }
                        noSearch.Enqueue(neighbor);
                    }
                }
                while (!nextSearches.EnumerableNullOrEmpty())
                {
                    searching.Enqueue(nextSearches.Dequeue());
                }
            }
        }
        
        public static void RevealWorld()
        {
            foreach (var tileStat in tileTracker)
            {
                tileStat.Value.SetAllVisionValues(true);
            }
            foreach (var obj in objectTracker)
            {
                obj.Value.founded = true;
            }

            foreach (WorldObject mp in Find.WorldObjects.MapParents)
            {
                AddObject(mp);
                Founded(mp);
            }
            
            List<WorldLayer> layers =
                Traverse.Create(Find.World.renderer).Field("layers").GetValue() as List<WorldLayer>;
            foreach (var layer in layers)
            {
                if (layer.GetType() == typeof(WorldLayer_UngeneratedFog))
                {
                    WorldLayer_UngeneratedFog ungenFogLayer = (WorldLayer_UngeneratedFog)layer;
                    ungenFogLayer.Restore();
                    SetUpdateType(TileUpdateType.Planet);
                    break;
                }
            }
            SetUpdateType(TileUpdateType.Full);
        }
        

        public static void ScanAllWithObject(WorldObject worldObject)
        {
            RevealWorld();
            foreach (var tile in tileTracker)
            {
                LinkObjectToTile(worldObject, tile.Key);
            }
            UpdateGraphics();
        }
        
        
        public static List<int> GetNeighborWithin(int tileID, int maxRange)
        {
            List<int> returnSearched = new List<int>();
            Queue<int> searching = new Queue<int>();
            searching.Enqueue(tileID);
            for (int i = maxRange; i > 0; i--)
            {
                Queue<int> nextSearches = new Queue<int>();
                while (!searching.EnumerableNullOrEmpty())
                {
                    int tile = searching.Dequeue();
                    tileTracker[tile].SetExploredValue(true);
                    List<int> neighbors = new List<int>();
                    Find.WorldGrid.GetTileNeighbors(tile, neighbors);
                    foreach (int neighbor in neighbors)
                    {
                        if (!returnSearched.Contains(neighbor))
                        {
                            nextSearches.Enqueue(neighbor);
                        }
                        returnSearched.Add(neighbor);
                    }
                }
                while (!nextSearches.EnumerableNullOrEmpty())
                {
                    searching.Enqueue(nextSearches.Dequeue());
                }
            }
            return returnSearched;
        }

        public static bool Trackable(WorldObject obj)
        {
            // Tracks worldobject without a map parent
            if (obj.Faction != null && obj.Faction.IsPlayer) {
                if (obj.GetType() == typeof(Caravan) || obj.GetType().IsSubclassOf(typeof(Caravan)))
                {
                    // Caravan caravan = (Caravan) obj;
                    // if (caravan.PawnsListForReading.Count < 1) return false;
                    Follow(obj);
                    return true;
                }

                if (obj.GetType().IsSubclassOf(typeof(Outpost)) || obj.GetType().ToString() == "Vehicles.AerialVehicleInFlight")
                {
                    Follow(obj);
                    return true;
                }
            }
            
            // Tracks ally
            if (Current.ProgramState==ProgramState.Playing && IsAlly(obj))
            {
                Follow(obj);
                return true;
            }

            // Tracks with map parent
            if (obj.GetType().IsSubclassOf(typeof(MapParent)) || obj.GetType()==typeof(MapParent))
            {
                MapParent mp = (MapParent) obj;
                if (mp.Map!=null && mp.Map.mapPawns.FreeColonists.Count > 0)
                {
                    Follow(obj);
                    return true;
                }
                if (mp.def!=null && mp.def.label!=null && mp.LabelCap == "Destroyed settlement")
                {
                    Follow(obj);
                    return true;
                }
            }
            Follow(obj, false);
            return false;
        }

        public static void UpdateGraphics()
        {
            if (Find.World != null)
            {
                if (updateType >= TileUpdateType.Fog)
                {
                    Find.World.renderer.SetDirty<WorldLayer_Fog>();
                }

                if (updateType >= TileUpdateType.Full)
                {
                    if (Current.ProgramState == ProgramState.Playing)
                    {

                        int index = 0;
                        foreach (WorldFeature feat in Find.World.features.features)
                        {
                            WorldFeatureManager manager = Find.World.GetComponent<WorldFeatureManager>();
                            if (!manager.learnedFeatures[index])
                            {
                                List<int> allTiles = feat.Tiles.ToList();
                                List<int> exploredTiles = allTiles.FindAll(t =>
                                    Find.WorldGrid.tiles[t].feature == feat && TileExplored(t));
                                if (exploredTiles.Count / (float)allTiles.Count > 0.25f)
                                {
                                    manager.learnedFeatures[index] = true;
                                }
                            }

                            index++;
                        }
                    }

                    Find.World.renderer.SetDirty<WorldLayer_Exploration>();
                }

                if (updateType >= TileUpdateType.Planet)
                {
                    Find.World.renderer.SetDirty<WorldLayer_UngeneratedFog>();
                }

                updateType = TileUpdateType.None;
            }
        }
    }
}