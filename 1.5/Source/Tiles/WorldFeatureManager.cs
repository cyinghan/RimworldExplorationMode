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
        // public Dictionary<Pawn, int> pawnRevealCount;
        
        // private static List<Pawn> tmpPawns;
        // private static List<int> tmpCount;

        public WorldFeatureManager(World world) : base(world)
        {
            
        }
        
        public override void FinalizeInit()
        {
            if (learnedFeatures==null)
                learnedFeatures = Enumerable.Repeat(false, world.features.features.Count).ToList();
            // if (pawnRevealCount == null)
            //     pawnRevealCount = new Dictionary<Pawn, int>();
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref learnedFeatures, "RWE_learnedFeatures", LookMode.Value);
            // Scribe_Collections.Look(ref pawnRevealCount, "RWE_pawnRevealCount",LookMode.Deep, LookMode.Value,
                // ref tmpPawns, ref tmpCount);
        }

        // public void IncrementReveal(Pawn pawn)
        // {
        //     GetCount(pawn);
        //     // pawnRevealCount[pawn] += 1;
        // }

        // public int GetCount(Pawn pawn)
        // {
        //     if (!pawnRevealCount.ContainsKey(pawn))
        //     {
        //         pawnRevealCount[pawn] = 0;
        //     }
        //     return pawnRevealCount[pawn];
        // }
        //
        // public double GetRevealChance(Pawn pawn)
        // {
        //     return Math.Exp(-GetCount(pawn) * 5f / pawn.skills.GetSkill(SkillDefOf.Intellectual).Level);
        // }
    }
}