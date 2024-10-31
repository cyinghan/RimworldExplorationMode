using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace RimworldExploration
{
    public sealed class WorldFeatureManager : WorldComponent
    {
        public List<bool> learnedFeatures;
        

        public WorldFeatureManager(World world) : base(world)
        {
            
        }
        
        
        public override void FinalizeInit()
        {
            if (learnedFeatures==null)
                learnedFeatures = Enumerable.Repeat(false, world.features.features.Count).ToList();
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref learnedFeatures, "RWE_learnedFeatures", LookMode.Value);
        }
    }
}