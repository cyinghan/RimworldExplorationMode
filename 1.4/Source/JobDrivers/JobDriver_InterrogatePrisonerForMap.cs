using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;

namespace RimworldExploration
{
	public class JobDriver_InterrogatePrisonerForMap : JobDriver
	{
		protected Pawn Talkee => (Pawn)job.targetA.Thing;
		private Random rand = new Random();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOnMentalState(TargetIndex.A);
			this.FailOnNotAwake(TargetIndex.A);
			this.FailOn(() => !Talkee.IsPrisonerOfColony || !Talkee.guest.PrisonerIsSecure);
			yield return Toils_Interpersonal.GotoPrisoner(pawn, Talkee, Talkee.guest.interactionMode);
			yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
			yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
			if (pawn.relations.OpinionOf(Talkee) > 10 || pawn.story.traits.HasTrait(TraitDefOf.Kind))
			{
				for (int i = 0; i < 5; i++)
				{
					double chance = rand.NextDouble();
					if (chance < 0.1)
					{
						LearnFactionLocation(Talkee);
					}
					yield return Toils_Interpersonal.GotoPrisoner(pawn, Talkee, Talkee.guest.interactionMode);
					yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
				}
			}
			else
			{
				for (int i=0;i<5;i++) 
				{
					double chance = rand.NextDouble();
					if (chance < 0.1)
					{
						LearnFactionLocation(Talkee);
					}
					yield return Toils_Interpersonal.GotoPrisoner(pawn, Talkee, Talkee.guest.interactionMode);
					yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
					yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, delegate
					{
						if (pawn.meleeVerbs.TryMeleeAttack(Talkee, job.verbToUse))
						{
							base.Map.attackTargetsCache.UpdateTarget(pawn);
						}
					});
				}
			}
		}

		public void LearnFactionLocation(Pawn pawn)
		{
			List<Settlement> prisonerSettlements =
				Find.WorldObjects.Settlements.FindAll(s =>
					!VisibilityManager.IsFounded(s) && s.Faction == pawn.Faction);
			if (prisonerSettlements.Count > 0)
			{
				var random = new Random();
				Settlement stmt = prisonerSettlements[random.Next()];
				VisibilityManager.RevealAt(stmt, 4);
				Message msg = new Message(
					Translator.Translate("RWE_PrisonerRevealedLocation").Formatted(pawn.LabelCap, stmt.LabelCap),
					MessageTypeDefOf.PositiveEvent);
				Messages.Message(msg);
				VisibilityManager.UpdateGraphics();
			}
		}
	}
}