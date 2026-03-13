using RimWorld;
using Verse;
namespace TOT_DLL_test
{
    public class CompProperties_BackpackAmmo : CompProperties_ApparelReloadable
    {
        public CompProperties_BackpackAmmo()
        {
            this.compClass = typeof(CompBackpackAmmo);
        }
    }
    public class CompBackpackAmmo : CompApparelReloadable
    {
        public int Charges
        {
            get
            {
                return this.remainingCharges;
            }
        }
        public int ExtractAmmo(int requestedAmount)
        {
            Log.Message("ExtractAmmo");
            int amountToGive = UnityEngine.Mathf.Min(requestedAmount, this.remainingCharges);
            this.remainingCharges -= amountToGive;
            return amountToGive;
        }
    }
}