using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EmergencySurgery
{
    class Recipe_RemoveBodyPart_NoMedicine : Recipe_Surgery_NoMedicine
    {
        protected virtual bool SpawnPartsWhenRemoved => true;

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
            {
                BodyPartRecord part = notMissingPart;
                if (pawn.health.hediffSet.HasDirectlyAddedPartFor(part))
                    yield return part;
                else if (MedicalRecipesUtility.IsCleanAndDroppable(pawn, part))
                    yield return part;
                else if (part != pawn.RaceProps.body.corePart && part.def.canSuggestAmputation && pawn.health.hediffSet.hediffs.Any<Hediff>((Predicate<Hediff>)(d => !(d is Hediff_Injury) && d.def.isBad && d.Visible && d.Part == part)))
                    yield return part;
            }
        }

        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction) 
            => (pawn.Faction != billDoerFaction && pawn.Faction != null || pawn.IsQuestLodger()) && HealthUtility.PartRemovalIntent(pawn, part) == BodyPartRemovalIntent.Harvest;

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            AddMemories(pawn, billDoer);
            bool flag1 = MedicalRecipesUtility.IsClean(pawn, part);
            bool flag2 = this.IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            if (billDoer != null)
            {
                if (this.CheckSurgeryFail(billDoer, pawn, part, bill))
                    return;
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, (object)billDoer, (object)pawn);
                if (this.SpawnPartsWhenRemoved)
                {
                    MedicalRecipesUtility.SpawnNaturalPartIfClean(pawn, part, billDoer.Position, billDoer.Map);
                    MedicalRecipesUtility.SpawnThingsFromHediffs(pawn, part, billDoer.Position, billDoer.Map);
                }
            }
            this.DamagePart(pawn, part);
            if (flag1)
                this.ApplyThoughts(pawn, billDoer);
            if (!flag2)
                return;
            this.ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
        }

        public virtual void DamagePart(Pawn pawn, BodyPartRecord part)
        {
            var parentPart = part.parent;
            pawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, 99999f, 999f, hitPart: part, spawnFilth: true));
            pawn.health.AddHediff(EmergencySurgeryDefOf.EmergencySurgery_SurgicalTrauma, parentPart);
        }

        public virtual void AddMemories(Pawn patient, Pawn surgeon)
        {
            if (ModsConfig.IdeologyActive)
            {
                var ideo = patient.Ideo;
                if (ideo.HasMeme(MemeDefOfExtended.PainIsVirtue))
                {
                    patient.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_AwakeForOperationGood);
                    return;
                }
            }

            if (patient.story.traits.HasTrait(TraitDefOf.Masochist))
            {
                patient.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_AwakeForOperationGood);
                return;
            }

            patient.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_AwakeForOperation);
            
        }

        public virtual void ApplyThoughts(Pawn pawn, Pawn billDoer)
        {
            if (pawn.Dead)
            {
                ThoughtUtility.GiveThoughtsForPawnExecuted(pawn, billDoer, PawnExecutionKind.OrganHarvesting);
            }
            else
            {
                ThoughtUtility.GiveThoughtsForPawnOrganHarvested(pawn, billDoer);
            }
        }

        public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
        {
            if (pawn.RaceProps.IsMechanoid || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                return RecipeDefOf.RemoveBodyPart.label;
            switch (HealthUtility.PartRemovalIntent(pawn, part))
            {
                case BodyPartRemovalIntent.Harvest:
                    return (string)"HarvestOrgan".Translate() + " " + "NoMedicine".Translate();
                case BodyPartRemovalIntent.Amputate:
                    return part.depth == BodyPartDepth.Inside || part.def.socketed ? (string)"RemoveOrgan".Translate() + " " + "NoMedicine".Translate() : (string)"Amputate".Translate() + " " + "NoMedicine".Translate();
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
