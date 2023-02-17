using System.Collections.Generic;
using RimWorld.Planet;
using RimworldExploration.Layer;
using Verse;

namespace RimworldExploration
{
    enum TileUpdateType
    {
        None=0,
        Fog=1,
        Full=2,
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
        public bool tracked;
        public HashSet<int> tileHolder;
        
        public WorldObjectVisibility()
        {
            founded = false;
            tracked = false;
            tileHolder = new HashSet<int>();
        }

        public WorldObjectVisibility(WorldObject obj)
        {
            founded = false;
            tracked = false;
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
            Scribe_Values.Look(ref tracked, "RWE_tracked");
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

        public static bool IsTracked(WorldObject obj)
        {
            if (objectTracker.ContainsKey(obj.ID))
                return objectTracker[obj.ID].tracked;
            return false;
        }
        
        public static void SetTracked(WorldObject obj, bool val = true)
        {
            if (objectTracker.ContainsKey(obj.ID))
                objectTracker[obj.ID].tracked = val;
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

        public static bool IsObjectVisible(WorldObject obj)
        {
            if (obj != null)
            {
                if ((IsTracked(obj)) ||
                    // obj.GetType()==typeof(CaravansBattlefield) ||
                    OnVisibleTile(obj) || hasSatelite ||
                    (!IsWarband(obj) && IsFounded(obj)))
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
        
        public static void RevealWorld()
        {
            foreach (var tileStat in tileTracker)
            {
                tileStat.Value.explored = true;
            }
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
            }
        }

        public static bool Trackable(WorldObject obj)
        {
            if (obj.Faction!=null && obj.Faction.IsPlayer && obj.GetType() == typeof(Caravan))
            {
                SetTracked(obj);
                return true;
            } 
            if (obj.GetType().IsSubclassOf(typeof(MapParent)) || obj.GetType()==typeof(MapParent))
            {
                MapParent mp = (MapParent) obj;
                if (mp.Map!=null && mp.Map.mapPawns.FreeColonists.Count > 0)
                {
                    SetTracked(obj);
                    return true;
                }
            }
            SetTracked(obj, false);
            return false;
        }

        public static void UpdateGraphics()
        {
            if (updateType >= TileUpdateType.Fog)
            {
                Find.World.renderer.SetDirty<WorldLayer_Fog>();
            }
            if (updateType == TileUpdateType.Full)
            {
                Find.World.renderer.SetDirty<WorldLayer_Exploration>();
            }
            updateType = TileUpdateType.None;
        }
        
    }
}