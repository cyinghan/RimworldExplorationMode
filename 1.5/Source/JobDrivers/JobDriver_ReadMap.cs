using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RimworldExploration
{
    public class JobDriver_ReadMap : JobDriver
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            ReadMapComp compProp = job.targetA.Thing.TryGetComp<ReadMapComp>();
            if (compProp != null && job.targetA != null && job.targetA.Thing != null)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
                if(pawn.interactions != null)
                {
                    yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
                }
                Toil ReadMapToil = ToilMaker.MakeToil("ReadMap");
                ReadMapToil.WithProgressBarToilDelay(TargetIndex.A);
                ReadMapToil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
                ReadMapToil.socialMode = RandomSocialMode.Off;
                ReadMapToil.defaultCompleteMode = ToilCompleteMode.Instant;
                ReadMapToil.initAction = () => LearnLocation(compProp);
                yield return ReadMapToil;
            }
        }
        
        public void LearnLocation(ReadMapComp prop)
        {
            List<Settlement> unknownSettlements = Find.WorldObjects.Settlements.FindAll(s=> !VisibilityManager.IsFounded(s));
            if (unknownSettlements.Count > 0 && prop.parent!=null)
            {
                Thing mapItem = prop.parent;
                int parentIntegrity = (int) Math.Round(mapItem.HitPoints / (double)mapItem.MaxHitPoints);
                var random = new Random();
                IEnumerable<Settlement> selectedSettlements = unknownSettlements.OrderBy(x => random.Next()).Take(Math.Min(unknownSettlements.Count, prop.compProperties_ReadMap.locations));
                foreach (var stmt in selectedSettlements)
                {
                    VisibilityManager.RevealAt(stmt, prop.compProperties_ReadMap.size * parentIntegrity);
                    Message msg = new Message(Translator.Translate("RWE_RevealedLocation").Formatted(stmt.LabelCap), MessageTypeDefOf.PositiveEvent);
                    Messages.Message(msg);
                }
                VisibilityManager.UpdateGraphics();
            }
            if (prop.parent!=null) prop.parent.Destroy();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}