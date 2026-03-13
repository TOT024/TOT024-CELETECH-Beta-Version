using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public abstract class CompAccessoryEffect : ThingComp
    {
        public virtual void Notify_WeaponFired(Pawn user, Thing weapon) { }
        public virtual void Notify_WeaponKilled(Pawn user, Thing weapon) { }
    }

    public class CompProperties_AccessoryEffect : CompProperties
    {
        public CompProperties_AccessoryEffect()
        {
            compClass = typeof(CompAccessoryEffect);
        }
    }
    public class CompProperties_AccessoryEffect_GainFocus : CompProperties_AccessoryEffect
    {
        public float triggerChanceFire = 0.01f;
        public float triggerChanceKill = 1.0f;
        public CompProperties_AccessoryEffect_GainFocus()
        {
            compClass = typeof(CompAccessoryEffect_GainFocus);
        }
    }
    public class CompAccessoryEffect_GainFocus : CompAccessoryEffect
    {
        public CompProperties_AccessoryEffect_GainFocus Props => (CompProperties_AccessoryEffect_GainFocus)props;

        public override void Notify_WeaponFired(Pawn user, Thing weapon)
        {
            if (user == null || user.Map == null) return;
            if (Rand.Value <= Props.triggerChanceFire)
            {
                Pawn_PsychicEntropyTracker psychicEntropy = user.psychicEntropy;
                if (psychicEntropy == null)
                {
                    return;
                }
                psychicEntropy.RemoveAllEntropy();
            }
        }
        public override void Notify_WeaponKilled(Pawn user, Thing weapon)
        {
            if (user == null || user.Map == null) return;
            Pawn_PsychicEntropyTracker psychicEntropy = user.psychicEntropy;
            if (psychicEntropy == null)
            {
                return;
            }
            psychicEntropy.OffsetPsyfocusDirectly(Mathf.Max(0.06f, 0.01f * user.GetPsylinkLevel()));
        }
    }
}