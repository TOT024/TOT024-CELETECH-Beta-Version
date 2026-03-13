using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;
using static HarmonyLib.Code;

namespace TOT_DLL_test
{
    public class CompAbilityEffect_AoEFist : CompAbilityEffect
    {
        private new CompProperties_AoEFist Props
        {
            get
            {
                return (CompProperties_AoEFist)this.props;
            }
        }
        private Pawn Pawn
        {
            get
            {
                return this.parent.pawn;
            }
        }
        private bool Canusecell(IntVec3 c)
        {
            return c.InBounds(this.Pawn.Map) && !(c == this.Pawn.Position) && c.InHorDistOf(this.Pawn.Position, this.Props.range) && this.parent.verb.TryFindShootLineFromTo(this.parent.pawn.Position, c, out ShootLine shootLine, false);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 position = Pawn.Position;
            float num = (target.CenterVector3.ToIntVec3() - Pawn.Position).AngleFlat;
            bool flag4 = this.Props.SpawnFleck != null;
            if (flag4)
            {
                for (int j = 0; j < this.Props.Fleck_Num; j++)
                {
                    float scale = 15f + Rand.Range(-5f, 5f);
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(Pawn.DrawPos, Pawn.Map, this.Props.SpawnFleck, scale);
                    float num2 = num + Rand.Range(-30f, 30f);
                    bool flag9 = num2 > 180f;
                    if (flag9)
                    {
                        num2 = num2 - 180f + -180f;
                    }
                    bool flag10 = num2 < -180f;
                    if (flag10)
                    {
                        num2 = num2 + 180f + 180f;
                    }
                    dataStatic2.rotation = 0f;
                    dataStatic2.rotation = num2;
                    dataStatic2.velocityAngle = num2;
                    dataStatic2.velocitySpeed = Rand.Range(70f, 75f);
                    Pawn.Map.flecks.CreateFleck(dataStatic2);
                }
            }
            DamageDef named = DefDatabase<DamageDef>.GetNamed("Bomb", true);
            GenExplosion.DoExplosion(position, this.parent.pawn.MapHeld, this.Props.range, named, Pawn, 8, -1f, null, null, null, null, null, 1f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, false, 1.6f, 0f, false, null, 1f, null, this.AffectedCells(target));
            base.Apply(target, dest);
        }
        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            this.tmpCells.Clear();
            Vector3 b = this.Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(this.Pawn.Map);
            if (this.Pawn.Position == intVec)
            {
                return this.tmpCells;
            }
            float lengthHorizontal = (intVec - this.Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - this.Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - this.Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)this.Pawn.Position.x + num * this.Props.range);
            intVec.z = Mathf.RoundToInt((float)this.Pawn.Position.z + num2 * this.Props.range);
            float target2 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - b, Vector3.right, Vector3.up);
            float num3 = this.Props.lineWidthEnd / 2f;
            float num4 = Mathf.Sqrt(Mathf.Pow((intVec - this.Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
            float num5 = 57.29578f * Mathf.Asin(num3 / num4);
            int num6 = GenRadial.NumCellsInRadius(this.Props.range);
            for (int i = 0; i < num6; i++)
            {
                IntVec3 intVec2 = this.Pawn.Position + GenRadial.RadialPattern[i];
                if (this.Canusecell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - b, Vector3.right, Vector3.up), target2)) <= num5)
                {
                    this.tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(this.Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!this.tmpCells.Contains(intVec3) && this.Canusecell(intVec3))
                {
                    this.tmpCells.Add(intVec3);
                }
            }
            return this.tmpCells;
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                {
                    EffecterDef effecter = DefDatabase<EffecterDef>.GetNamed("CMC_PulseWave", true);
                    this.parent.AddEffecterToMaintain(effecter.Spawn(this.parent.pawn.Position, a.Cell, this.parent.pawn.Map, 1f), this.Pawn.Position, a.Cell, 17, this.Pawn.MapHeld);
                },
                ticksAwayFromCast = 8
            };
            yield break;
        }
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(this.AffectedCells(target));
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.Pawn.Faction != null)
            {
                foreach (IntVec3 c in this.AffectedCells(target))
                {
                    List<Thing> thingList = c.GetThingList(this.Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == this.Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        private List<IntVec3> tmpCells = new List<IntVec3>();
        public static EffecterDef pulse;
    }
}
