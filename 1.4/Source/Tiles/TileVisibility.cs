using System.Collections.Generic;
using RimWorld.Planet;
using RimworldExploration.Layer;
using Outposts;
using Verse;
using HarmonyLib;
using RimWorld;

namespace RimworldExploration
{
    enum TileUpdateType
    {
        None=0,
        Fog=1,
        Full=2,
        Planet=3
    }
    
    public class TileVisibility : IExposable
    {
        public bool explored;
        public bool visible;
        public List<int> beholders;

        public TileVisibility()
        {
            explored = false;
            visible = false;
            beholders = new List<int>();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref explored, "RWE_explored");
            Scribe_Values.Look(ref visible, "RWE_visible");
            Scribe_Collections.Look(ref beholders, "RWE_beholders", LookMode.Value);
        }

        public void AddVision(WorldObject obj)
        {
            if (!beholders.Contains(obj.ID))
            {
                visible = true;
                explored = true;
                beholders.Add(obj.ID);
            }
        }
        
        public void RemoveVision(WorldObject obj)
        {
            beholders.Remove(obj.ID);
            if (beholders.NullOrEmpty())
            {
                visible = false;
            }
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
            if (obj.Faction!=null && obj.Faction.IsPlayer)
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
        public static bool hasSatelite;
        public static Dictionary<int, TileVisibility> tileTracker;
        public static Dictionary<int, WorldObjectVisibility> objectTracker;
        public static List<WorldObject> updateTracker;

        private static TileUpdateType updateType;
            
        private static List<int> tmpTileIDs;
        private static List<TileVisibility> tmpTileVisibility;
        private static List<int> tmpWorldObjects;
        private static List<WorldObjectVisibility> tmpWorldObjectVisibility;
        
        public VisibilityManager(Game game)
        {
            tileTracker = new Dictionary<int, TileVisibility>();
            objectTracker = new Dictionary<int, WorldObjectVisibility>();
            updateTracker = new List<WorldObject>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref updateType, "RWE_updateType");
            Scribe_Values.Look(ref hasSatelite, "RWE_hasSatelite");
            Scribe_Collections.Look(ref tileTracker, "RWE_tileTracker",LookMode.Value, LookMode.Deep,
                ref tmpTileIDs, ref tmpTileVisibility);
            Scribe_Collections.Look(ref objectTracker, "RWE_objectTracker",LookMode.Value, LookMode.Deep,
                ref tmpWorldObjects, ref tmpWorldObjectVisibility);
            Scribe_Collections.Look(ref updateTracker, "RWE_updateTracker", LookMode.Deep);
        }
        
        public static void Initialize()
        {
            updateType = TileUpdateType.None;
            hasSatelite = false;
            tileTracker.Clear();
            objectTracker.Clear();
            updateTracker.Clear();
            if (tileTracker.Count < 1)
            {
                for (int tileID = Find.WorldGrid.tiles.Count; tileID >= 0; tileID--)
                {
                    AddTileToTracker(tileID);
                }
            }
        }

        public static void ResetExplore()
        {
            foreach (var tile in tileTracker)
            {
                tile.Value.explored = false;
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
            if (objectTracker.ContainsKey(obj.ID))
            {
                objectTracker[obj.ID].followed = val;
                objectTracker[obj.ID].founded = val;
            }
            else
            {
                AddObject(obj);
                objectTracker[obj.ID].followed = val;
                objectTracker[obj.ID].founded = val;
            }
        }

        public static void AddTileToTracker(int tileID)
        {
            tileTracker[tileID] = new TileVisibility();
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
                RefreshObject(obj);
                objectTracker.Remove(obj.ID);
            }
        }

        private static void SetUpdateType(TileUpdateType newType)
        {
            if (updateType < newType)
            {
                updateType = newType;
            }
        }

        public static void RefreshObject(WorldObject obj)
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

        public static bool IsVisibleTo(int target, int observer, float range)
        {
            Tile targetTile = Find.WorldGrid.tiles[target];
            Tile observerTile = Find.WorldGrid.tiles[observer];
            float elevationDiff = observerTile.hilliness - targetTile.hilliness;
            float pollutionSum = observerTile.pollution + targetTile.pollution;
            float swampinessSum = observerTile.swampiness + targetTile.swampiness;
            float requiredRange = elevationDiff + pollutionSum + swampinessSum;
            // Log.Message($"=== {elevationDiff}, {pollutionSum}, {swampinessSum}");
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
            if (obj != null)
            {
                
                if (IsFollowed(obj) ||
                    OnVisibleTile(obj) || FromRimNaut(obj)
                    || (!IsWarband(obj) && (IsFounded(obj) || hasSatelite)))
                {
                    
                    return true;
                }
            }
            return false;
        }
        
        public static bool OnVisibleTile(WorldObject obj)
        {
            if (tileTracker.ContainsKey(obj.Tile)) 
                return tileTracker[obj.Tile].visible;
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
            if (tileID >= 0)
                return tileTracker[tileID].explored;
            return false;
        }
        
        public static bool TileVisible(int tileID)
        {
            if (tileID >= 0)
                return tileTracker[tileID].visible;
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

        public static void RevealWithObject(WorldObject obj, int maxRange)
        {
            if (obj.Tile < 0 || !objectTracker.ContainsKey(obj.ID)) return;
            RefreshObject(obj);
            Queue<int> noSearch = new Queue<int>();
            Queue<int> searching = new Queue<int>();
            searching.Enqueue(obj.Tile);
            for (int i = maxRange; i > 0; i--)
            {
                Queue<int> nextSearches = new Queue<int>();
                while (!searching.EnumerableNullOrEmpty())
                {
                    int tile = searching.Dequeue();
                    
                    if (!tileTracker[tile].explored)
                        SetUpdateType(TileUpdateType.Full);
                    else if (!tileTracker[tile].visible)
                        SetUpdateType(TileUpdateType.Fog);
                    
                    LinkObjectToTile(obj, tile);
                    List<int> neighbors = new List<int>();
                    Find.WorldGrid.GetTileNeighbors(tile, neighbors);
                    if (IsVisibleTo(tile, obj.Tile, i + 3))
                    {
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

                    if (!tileTracker[tile].explored)
                        SetUpdateType(TileUpdateType.Full);
                    else if (!tileTracker[tile].visible)
                        SetUpdateType(TileUpdateType.Fog);
                    tileTracker[tile].explored = true;
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
        
        public static void RevealInit(int tileID, int maxRange)
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

                    if (!tileTracker[tile].explored)
                        SetUpdateType(TileUpdateType.Full);
                    else if (!tileTracker[tile].visible)
                        SetUpdateType(TileUpdateType.Fog);
                    tileTracker[tile].explored = true;
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
                tileStat.Value.explored = true;
            }
            SetUpdateType(TileUpdateType.Full);
            UpdateGraphics();
        }

        public static void Scan()
        {
            if (hasSatelite)
            {
                RevealWorld();
                foreach (var obj in objectTracker)
                {
                    obj.Value.founded = true;
                }

                foreach (WorldObject mp in Find.WorldObjects.MapParents)
                {
                    AddObject(mp);
                    Founded(mp);
                }
                List<WorldLayer> layers = Traverse.Create(Find.World.renderer).Field("layers").GetValue() as List<WorldLayer>;
                foreach (var layer in layers)
                {
                    if (layer.GetType() == typeof(WorldLayer_UngeneratedFog))
                    {
                        WorldLayer_UngeneratedFog ungenFogLayer = (WorldLayer_UngeneratedFog)layer;
                        ungenFogLayer.Restore();
                        SetUpdateType(TileUpdateType.Planet);
                        UpdateGraphics();
                        break;
                    }
                }
            }
        }

        public static bool Trackable(WorldObject obj)
        {
            // tracks worldobject without a map parent
            if (obj.Faction != null && obj.Faction.IsPlayer &&
                (obj.GetType() == typeof(Caravan) ||
                 obj.GetType().IsSubclassOf(typeof(Outpost))))
            {
                Follow(obj);
                return true;
            }
            
            // tracks ally
            if (Current.ProgramState==ProgramState.Playing && IsAlly(obj))
            {
                Follow(obj);
                return true;
            }

            // tracks with map parent
            if (obj.GetType().IsSubclassOf(typeof(MapParent)) || obj.GetType()==typeof(MapParent))
            {
                MapParent mp = (MapParent) obj;
                if (mp.Map!=null && mp.Map.mapPawns.FreeColonists.Count > 0)
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
            if (updateType >= TileUpdateType.Fog)
            {
                Find.World.renderer.SetDirty<WorldLayer_Fog>();
            }
            if (updateType >= TileUpdateType.Full)
            {
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