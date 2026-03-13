using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_LaserHeat : CompProperties
    {
        public float maxHeat = 100f;
        public float heatPerBurstShot = 4f;
        public float coolPerTick = 0.2f;      
        public int overheatBackswingTicks = 90; 

        public CompProperties_LaserHeat()
        {
            compClass = typeof(CompLaserHeat);
        }
    }

    public class CompLaserHeat : ThingComp
    {
        private float heat;

        public CompProperties_LaserHeat Props => (CompProperties_LaserHeat)props;
        public float Heat => heat;
        public float HeatPct => Props.maxHeat <= 0f ? 0f : heat / Props.maxHeat;
        public bool IsOverheated => heat >= Props.maxHeat - 0.0001f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref heat, "heat", 0f);
        }

        public void AddHeatPerShot()
        {
            heat += Props.heatPerBurstShot;
            if (heat > Props.maxHeat) heat = Props.maxHeat;
        }

        public void CoolPerTick()
        {
            if (heat <= 0f) return;
            heat -= Props.coolPerTick;
            if (heat < 0f) heat = 0f;
        }

        public override void CompTick()
        {
            base.CompTick();
            var eq = parent.GetComp<CompEquippable>();
            bool bursting = eq?.PrimaryVerb?.Bursting ?? false;
            if (!bursting)
            {
                CoolPerTick();
            }
        }
    }
}