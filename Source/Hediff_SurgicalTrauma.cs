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
    public class Hediff_SurgicalTrauma : HediffWithComps
    {
        public override float BleedRate
        {
            get
            {
                if (this.pawn.Dead || this.BleedingStoppedDueToAge || (this.Part.def.IsSolid(this.Part, this.pawn.health.hediffSet.hediffs) || this.IsTended()) || (this.IsPermanent() || this.pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(this.Part)))
                    return 0.0f;
                float num = this.Severity * this.def.injuryProps.bleedRate;
                if (this.Part != null)
                    num *= this.Part.def.bleedRate;
                return num;
            }
        }

        private int AgeTicksToStopBleeding => 90000 + Mathf.RoundToInt(Mathf.Lerp(0.0f, 90000f, Mathf.Clamp(Mathf.InverseLerp(1f, 30f, this.Severity), 0.0f, 1f)));

        private bool BleedingStoppedDueToAge => this.ageTicks >= this.AgeTicksToStopBleeding;

        public override void Tick()
        {
            int num1 = this.BleedingStoppedDueToAge ? 1 : 0;
            base.Tick();
            int num2 = this.BleedingStoppedDueToAge ? 1 : 0;
            if (num1 == num2)
                return;
            this.pawn.health.Notify_HediffChanged((Hediff)this);
        }
    }
}
