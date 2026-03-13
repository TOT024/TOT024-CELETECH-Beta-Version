using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class StatPart_AccessoryModifiers : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing) return;
            var holder = req.Thing.TryGetComp<CompAccessoryHolder>();
            if (holder == null) return;
            if (holder.TryGetStatOffset(this.parentStat, out float add, out float mult))
            {
                val += add;
                val *= mult;
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing) return null;

            var holder = req.Thing.TryGetComp<CompAccessoryHolder>();
            if (holder == null) return null;

            if (holder.TryGetStatOffset(this.parentStat, out float add, out float mult))
            {
                if (add == 0f && mult == 1f) return null;
                return "CMC_StatsReport_Accessories".Translate() + ": " +
                       this.parentStat.Worker.ValueToString(add, false, ToStringNumberSense.Offset);
            }
            return null;
        }
    }
}