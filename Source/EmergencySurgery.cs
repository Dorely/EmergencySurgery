using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace EmergencySurgery
{

    [DefOf]
    public static class EmergencySurgeryDefOf
    {
        public static HediffDef EmergencySurgery_SurgicalTrauma;
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
            return false; //don't do the original method - could cause incompatibilities
        }
    }


}
