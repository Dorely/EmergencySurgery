using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EmergencySurgery
{
    class Recipe_Surgery_NoMedicine : RecipeWorker
    {
        private const float MaxSuccessChance = 0.98f;
        private const float CatastrophicFailChance = 0.5f;
        private const float RidiculousFailChanceFromCatastrophic = 0.1f;
        private const float InspiredSurgerySuccessChanceFactor = 2f;
        private static readonly SimpleCurve MedicineMedicalPotencyToSurgeryChanceFactor = new SimpleCurve()
        {
          { new CurvePoint(0.0f, 0.7f), true },
          { new CurvePoint(1f, 1f), true },
          { new CurvePoint(2f, 1.3f), true }
        };

        protected bool CheckSurgeryFail(Pawn surgeon, Pawn patient, BodyPartRecord part, Bill bill)
        {
            if ((double)bill.recipe.surgerySuccessChanceFactor >= 99999.0)
                return false;
            float num = 1f;
            if (!patient.RaceProps.IsMechanoid)
                num *= surgeon.GetStatValue(StatDefOf.MedicalSurgerySuccessChance);
            if (!this.recipe.surgeryIgnoreEnvironment && patient.InBed())
                num *= patient.CurrentBed().GetStatValue(StatDefOf.SurgerySuccessChanceFactor);
            float a = num * this.recipe.surgerySuccessChanceFactor;
            if (surgeon.InspirationDef == InspirationDefOf.Inspired_Surgery && !patient.RaceProps.IsMechanoid)
            {
                a *= 2f;
                surgeon.mindState.inspirationHandler.EndInspiration(InspirationDefOf.Inspired_Surgery);
            }
            if (Rand.Chance(Mathf.Min(a, 0.98f)))
                return false;
            if (Rand.Chance(this.recipe.deathOnFailedSurgeryChance))
            {
                HealthUtility.GiveInjuriesOperationFailureCatastrophic(patient, part);
                if (!patient.Dead)
                    patient.Kill(null);
                Find.LetterStack.ReceiveLetter("LetterLabelSurgeryFailed".Translate(patient.Named("PATIENT")), "MessageMedicalOperationFailureFatal".Translate((NamedArgument)surgeon.LabelShort, (NamedArgument)patient.LabelShort, (NamedArgument)this.recipe.LabelCap, surgeon.Named("SURGEON"), patient.Named("PATIENT")), LetterDefOf.NegativeEvent, (LookTargets)(Thing)patient);
            }
            else if (Rand.Chance(0.5f))
            {
                if (Rand.Chance(0.1f))
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelSurgeryFailed".Translate(patient.Named("PATIENT")), "MessageMedicalOperationFailureRidiculous".Translate((NamedArgument)surgeon.LabelShort, (NamedArgument)patient.LabelShort, surgeon.Named("SURGEON"), patient.Named("PATIENT"), this.recipe.Named("RECIPE")), LetterDefOf.NegativeEvent, (LookTargets)(Thing)patient);
                    HealthUtility.GiveInjuriesOperationFailureRidiculous(patient);
                }
                else
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelSurgeryFailed".Translate(patient.Named("PATIENT")), "MessageMedicalOperationFailureCatastrophic".Translate((NamedArgument)surgeon.LabelShort, (NamedArgument)patient.LabelShort, surgeon.Named("SURGEON"), patient.Named("PATIENT"), this.recipe.Named("RECIPE")), LetterDefOf.NegativeEvent, (LookTargets)(Thing)patient);
                    HealthUtility.GiveInjuriesOperationFailureCatastrophic(patient, part);
                }
            }
            else
            {
                Find.LetterStack.ReceiveLetter("LetterLabelSurgeryFailed".Translate(patient.Named("PATIENT")), "MessageMedicalOperationFailureMinor".Translate((NamedArgument)surgeon.LabelShort, (NamedArgument)patient.LabelShort, surgeon.Named("SURGEON"), patient.Named("PATIENT"), this.recipe.Named("RECIPE")), LetterDefOf.NegativeEvent, (LookTargets)(Thing)patient);
                HealthUtility.GiveInjuriesOperationFailureMinor(patient, part);
            }
            if (!patient.Dead)
                this.TryGainBotchedSurgeryThought(patient, surgeon);
            return true;
        }

        private void TryGainBotchedSurgeryThought(Pawn patient, Pawn surgeon)
        {
            if (!patient.RaceProps.Humanlike || patient.needs.mood == null)
                return;
            patient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.BotchedMySurgery, surgeon);
        }
    }
}