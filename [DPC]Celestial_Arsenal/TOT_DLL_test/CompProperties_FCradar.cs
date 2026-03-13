using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_FCradar : CompProperties
    {
        public CompProperties_FCradar()
        {
            this.compClass = typeof(Comp_FCradar);
        }
        public float rotatorSpeed = 0.2f;
    }
}
