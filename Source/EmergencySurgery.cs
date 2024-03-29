﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimWorld
{
    [DefOf]
    public static class MemeDefOfExtended
    {
        [MayRequireIdeology]
        public static MemeDef PainIsVirtue;
    }

    [DefOf]
    public static class PreceptDefOfExtended
    {
        [MayRequireIdeology]
        public static PreceptDef Pain_Idealized;
    }

    [DefOf]
    public static class TraitDefOfExtended
    {
        public static TraitDef Masochist;
    }

    [DefOf]
    public static class BodyPartDefOfExtended
    {
        public static BodyPartDef Tongue;
    }
}

namespace EmergencySurgery
{
    //TODO cannot verify if infection is working properly, it doesnt seem to be, but the randomness could just mean I'm (un)lucky

    [DefOf]
    public static class EmergencySurgeryDefOf
    {
        public static HediffDef EmergencySurgery_SurgicalTrauma;
        public static ThoughtDef EmergencySurgery_AwakeForOperation;
        public static ThoughtDef EmergencySurgery_AwakeForOperationGood;
        public static ThoughtDef EmergencySurgery_PsychopathPerformedAwakeSurgery;
    }

    [StaticConstructorOnStartup]
    public class EmergencySurgery
    {
        static EmergencySurgery()
        {
            var harmony = new Harmony("com.dorely.emergencysurgery");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Bill_Medical), "Notify_BillWorkStarted")]
    static class Bill_Medical_Patches
    {
        public static Pawn MedicalBillDoer;

        static void Prefix(Pawn billDoer)
        {
            //store the pawn doing the surgery so that I can read it inside the anesthetize step
            MedicalBillDoer = billDoer;
        }

        static void Postfix()
        {
            MedicalBillDoer = null;
        }
    }


    [HarmonyPatch(typeof(HealthUtility), "TryAnesthetize")]
    static class HealthUtility_Patches
    {
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if(Bill_Medical_Patches.MedicalBillDoer == null)
            {
                return true;
            }

            //if theres medicine, don't do patch's logic
            if (Bill_Medical_Patches.MedicalBillDoer?.CurJob.placedThings != null)
            {
                return true;
            }

            if (!pawn.RaceProps.IsFlesh)
            {
                __result = false;
                return true;
            }


            //pawn.health.forceIncap = true;
            //pawn.health.forceIncap = false;
            pawn.health.forceDowned = true;
            pawn.health.forceDowned = false;

            __result = true;
            return false; //don't do the original method - this could cause incompatibilities
        }
    }


}
