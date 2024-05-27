using System.Collections.Generic;
using Verse;

namespace RimworldExploration
{
    // public class PawnMapKnowledgeManager : GameComponent
    // {
    //     private static List<string> PawnSharedKnowledge = new List<string>();
    //     
    //     public PawnMapKnowledgeManager(Game game)
    //     {
    //         PawnSharedKnowledge = new List<string>();
    //     }
    //     
    //     public override void ExposeData()
    //     {
    //         base.ExposeData();
    //         Scribe_Collections.Look(ref PawnSharedKnowledge, "RWE_PawnSharedKnowledge", LookMode.Value);
    //     }
    //
    //     public static void Add(Pawn pawn)
    //     {
    //         if (pawn!=null && pawn.Name!=null && PawnSharedKnowledge.Contains(pawn.Name.ToStringFull)) 
    //             PawnSharedKnowledge.Add(pawn.Name.ToStringFull);
    //     }
    //     
    //     public static void Remove(Pawn pawn)
    //     {
    //         if (pawn!=null && pawn.Name!=null) 
    //             PawnSharedKnowledge.Remove(pawn.Name.ToStringFull);
    //     }
    //     
    //     public static bool Contains(Pawn pawn)
    //     {
    //         if (pawn!=null && pawn.Name!=null)
    //             return PawnSharedKnowledge.Contains(pawn.Name.ToStringFull);
    //         return false;
    //     }
    // }
}