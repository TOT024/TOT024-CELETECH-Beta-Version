using HarmonyMod;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Comp_TraderShuttle : ThingComp
    {
        public CompProperties_TraderShuttle Props
        {
            get
            {
                return this.props as CompProperties_TraderShuttle;
            }
        }
        public override void PostExposeData()
        {
            Scribe_Deep.Look<Landed_CMCTS>(ref this.tradeShip, "ship", Array.Empty<object>());
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                bool flag = this.mustCrash;
                if (!flag)
                {
                    bool flag2 = this.Props.soundThud != null;
                    if (flag2)
                    {
                        this.Props.soundThud.PlayOneShot(this.parent);
                    }
                    string label = "TOT_TraderShuttleArrivalLabel".Translate();
                    string text = "TOT_TraderShuttleArrivalText".Translate(this.tradeShip.name);

                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, this.parent, null, null);
                }
            }
        }
        public override void CompTick()
        {
            bool departed = this.tradeShip.Departed;
            if (departed)
            {
                bool spawned = this.parent.Spawned;
                if (spawned)
                {
                    this.SendAway();
                }
            }
            else
            {
                this.tradeShip.PassingShipTick();
            }
        }
        public override string CompInspectStringExtra()
        {
            string text = this.tradeShip.def.LabelCap + "\n" + "UTSLeavingIn".Translate(this.tradeShip.ticksUntilDeparture.ToStringTicksToPeriod(true, false, true, true, false));
            return text;
        }
        public override string TransformLabel(string label)
        {
            return this.tradeShip.name;
        }
        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            bool flag = false;
            bool flag2 = totalDamageDealt > 2000f;
            if (flag2)
            {
                flag = true;
            }
            else
            {
                bool flag3 = (double)this.parent.HitPoints <= (double)this.parent.MaxHitPoints * 0.5;
                if (flag3)
                {
                    flag = true;
                }
            }
            bool flag4 = flag;
            if (flag4)
            {
                this.SendAway();
            }
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn negotiator)
        {
            string label = "TradeWith".Translate(this.tradeShip.GetCallLabel());
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate ()
            {
                Job job = JobMaker.MakeJob(CMC_Def.CMCTS_TradeWithShip, this.parent);
                negotiator.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }, MenuOptionPriority.InitiateSocial, null, null, 0f, null, null, true, 0), negotiator, this.parent, "ReservedBy", null);
            yield break;
        }
        private Faction GetFaction(TraderKindDef trader)
        {
            return null;
        }
        public void GenerateInternalTradeShip(Map map, TraderKindDef traderKindDef = null)
        {
            bool flag = traderKindDef == null;
            if (flag)
            {
                traderKindDef = DefDatabase<TraderKindDef>.AllDefs.RandomElementByWeightWithFallback((TraderKindDef x) => x.CalculatedCommonality, null);
            }
            this.tradeShip = new Landed_CMCTS(map, traderKindDef, this.GetFaction(traderKindDef));
            this.tradeShip.passingShipManager = map.passingShipManager;
            this.tradeShip.name = "CMC_TradeShipName".Translate();
            this.tradeShip.GenerateThings();
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "CMC_TraderShipsSendAway".Translate(),
                defaultDesc = "CMC_TraderShipsSendAwayDesc".Translate(),
                action = new Action(this.SendAway),
                icon = Comp_TraderShuttle.SendAwayTexture
            };
            yield break;
        }
        private void SendAway()
        {
            if (!this.parent.Spawned)
            {
                string str = "Tried to send ";
                ThingWithComps parent = this.parent;
                Log.Error(str + ((parent != null) ? parent.ToString() : null) + " away, but it's unspawned.");
            }
            else
            {
                float initialSilver = 100000f;
                float currentSilver = this.tradeShip.Silver;
                float silverDelta = currentSilver - initialSilver;
                float basePoints = 0f;
                if (silverDelta > 0)
                {
                    basePoints = silverDelta * 0.45f;
                }
                else
                {
                    basePoints = Mathf.Abs(silverDelta) * 0.35f;
                }
                if (basePoints > 0)
                {
                    float randomMultiplier = UnityEngine.Random.Range(0.89f, 1.15f);
                    float finalPoints = basePoints * randomMultiplier * TOT_DLL_test.Settings_CMC_Main.Settings_CMC.TradePointFactor;
                    GameComponent_CeleTech.Instance.CurrentPoint += (int)finalPoints;
                    Messages.Message("TOT_PointsAdded".Translate(finalPoints.ToString("F0"), GameComponent_CeleTech.Instance.CurrentPoint.ToString("F0")), MessageTypeDefOf.PositiveEvent);
                }

                Map map = this.parent.Map;
                IntVec3 position = this.parent.Position;
                this.parent.DeSpawn(DestroyMode.Vanish);
                Skyfaller skyfaller = ThingMaker.MakeThing(this.Props.takeoffAnimation, null) as Skyfaller;
                bool flag2 = !skyfaller.innerContainer.TryAdd(this.parent, true);
                if (flag2)
                {
                    Log.Error("Could not add " + this.parent.ToStringSafe<Thing>() + " to a skyfaller.");
                    this.parent.Destroy(DestroyMode.QuestLogic);
                }
                GenSpawn.Spawn(skyfaller, position, map, WipeMode.Vanish);
            }
        }
        public override void PostDraw()
        {
            base.PostDraw();
            Matrix4x4 matrix = default(Matrix4x4);
            Vector3 pos = this.parent.DrawPos + Altitudes.AltIncVect + this.parent.def.graphicData.drawOffset;
            pos.y = AltitudeLayer.Building.AltitudeFor();
            matrix.SetTRS(pos, Quaternion.identity, vec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, LightTexture, 0);
        }
        public Landed_CMCTS tradeShip;
        public bool mustCrash = false;
        private static readonly Texture2D SendAwayTexture = ContentFinder<Texture2D>.Get("UI/SendAway", true);
        private static Material LightTexture = MaterialPool.MatFrom("Things/Skyfaller/TradeShuttle_Light", ShaderDatabase.MoteGlow);
        private static Vector3 vec = new Vector3(5f, 0f, 5f);
    }
}
