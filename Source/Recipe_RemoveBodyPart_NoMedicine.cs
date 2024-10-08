﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace EmergencySurgery
{
    class Recipe_RemoveBodyPart_NoMedicine : Recipe_Surgery
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
                else if (part.def == BodyPartDefOf.Leg || part.def == BodyPartDefOf.Arm || part.def == BodyPartDefOfExtended.Tongue || part.def == BodyPartDefOf.Eye) // add arms, legs, eyes, and tongues to be removed any time
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
                var ingredientsEmpty = new List<Thing>();
                if (this.CheckSurgeryFail(billDoer, pawn, ingredientsEmpty, part, bill))
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
                this.ApplyThoughts(pawn, billDoer, MedicalRecipesUtility.IsCleanAndDroppable(pawn, part));
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
            //Log.Message($"AddMemories called for patient: {patient.Name}, surgeon: {surgeon?.Name}");

            if (ModsConfig.IdeologyActive)
            {
                var ideo = patient.Ideo;
                //Log.Message($"Ideology active. Patient's ideology: {ideo.name}");

                if (ideo.HasMeme(MemeDefOfExtended.PainIsVirtue) || ideo.HasPrecept(PreceptDefOfExtended.Pain_Idealized))
                {
                    //Log.Message("Patient has PainIsVirtue meme or Pain_Idealized precept.");
                    patient.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_AwakeForOperationGood);
                    return;
                }
            }

            if (patient.story.traits.HasTrait(TraitDefOfExtended.Masochist))
            {
                //Log.Message("Patient has Masochist trait.");
                patient.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_AwakeForOperationGood);
                return;
            }

            //Log.Message("Patient does not have special traits or memes. Applying default memory.");
            patient.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_AwakeForOperation);

            if (surgeon?.story?.traits != null)
            {
                //Log.Message($"Surgeon traits: Psychopath: {surgeon.story.traits.HasTrait(TraitDefOf.Psychopath)}, Bloodlust: {surgeon.story.traits.HasTrait(TraitDefOf.Bloodlust)}");

                if (surgeon.story.traits.HasTrait(TraitDefOf.Psychopath) || surgeon.story.traits.HasTrait(TraitDefOf.Bloodlust))
                {
                    //Log.Message("Surgeon has Psychopath or Bloodlust trait.");
                    surgeon.needs.mood.thoughts.memories.TryGainMemory(EmergencySurgeryDefOf.EmergencySurgery_PsychopathPerformedAwakeSurgery);
                }
            }
        }

        public virtual void ApplyThoughts(Pawn pawn, Pawn billDoer, bool partWasDropped)
        {
            if (pawn.Dead)
            {
                ThoughtUtility.GiveThoughtsForPawnExecuted(pawn, billDoer, PawnExecutionKind.OrganHarvesting);
            }
            else if (partWasDropped)
            {
                ThoughtUtility.GiveThoughtsForPawnOrganHarvested(pawn, billDoer);
            }
        }

        public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
        {
            if (pawn.RaceProps.IsMechanoid || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
                return RecipeDefOf.RemoveBodyPart.label;

            if (part.depth == BodyPartDepth.Outside && part.def != BodyPartDefOf.Eye)
            {
                return (string)"RemoveNoMedicine".Translate();
            }
            else
            {
                return (string)"HarvestNoMedicine".Translate();
            }
        }
    }
}
