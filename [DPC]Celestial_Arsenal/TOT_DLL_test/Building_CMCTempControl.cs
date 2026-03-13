using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Building_CMCTempControl : Building_TempControl
    {
        private CompBreakdownable breakdownComp;
        private bool isBlocked;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.breakdownComp = base.GetComp<CompBreakdownable>();
        }

        public override void TickRare()
        {
            if (!base.Spawned)
            {
                return;
            }
            if (!this.compPowerTrader.PowerOn)
            {
                return;
            }
            if (this.breakdownComp != null && this.breakdownComp.BrokenDown)
            {
                return;
            }

            IntVec3 intVec = base.Position + IntVec3.South.RotatedBy(base.Rotation);

            if (intVec.Impassable(base.Map))
            {
                this.isBlocked = true;
                this.compPowerTrader.PowerOutput = -this.compPowerTrader.Props.PowerConsumption;
                return;
            }

            this.isBlocked = false;
            Room room = intVec.GetRoom(base.Map);

            if (room == null || room.UsesOutdoorTemperature)
            {
                this.compPowerTrader.PowerOutput = -this.compPowerTrader.Props.PowerConsumption;
                return;
            }

            float targetTemperature = this.compTempControl.targetTemperature;
            room.Temperature = targetTemperature;

            float powerCost = this.compPowerTrader.Props.PowerConsumption + (room.CellCount * 0.5f);
            this.compPowerTrader.PowerOutput = -powerCost;

            this.compTempControl.operatingAtHighPower = true;
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (this.isBlocked)
            {
                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }
                text += "CMC.TempControlIsBlocked".Translate().Colorize(ColorLibrary.Red);
            }
            return text;
        }
    }
}