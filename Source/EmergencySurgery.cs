using System;
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

    [HarmonyPatch(typeof(Bill_Medical), "Notify_DoBillStarted")]
    static class Bill_Medical_Patches
    {
        public static Pawn MedicalBillDoer;

        static void Prefix(Pawn billDoer)
        {
            //store the pawn doing the surgery so that I can read it inside the anasthetize step
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
            //if theres medicine, don't do patch's logic
            if (Bill_Medical_Patches.MedicalBillDoer.CurJob.placedThings != null)
            {
                return true;
            }

            if (!pawn.RaceProps.IsFlesh)
            {
                __result = false;
                return true;
            }

            pawn.health.forceIncap = true;
            pawn.health.forceIncap = false;

            __result = true;
            return false; //don't do the original method - this could cause incompatibilities
        }
    }


}
