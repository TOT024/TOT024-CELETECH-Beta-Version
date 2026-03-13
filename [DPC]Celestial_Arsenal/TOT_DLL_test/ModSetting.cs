using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class WeaponAndAmount
    {
        public WeaponAndAmount() { }
        public ThingDef projectile;
        public int amount = -1;
        public float armorPenetrationBase = -1;
        public float explosionRadius = -1;
        public float ScatterExplosionDamage = -1;
        public float ScatterExplosionArmorPenetration = -1;
        public float damAmount_CMC = -1;
    }
    public class WeaponAndCooldown
    {
        public WeaponAndCooldown() { }
        public ThingDef weaponDef;
        public float cooldownTime = -1;
        public float warmupTime = -1;
    }
    public class Settings_CMC : ModSettings
    {
        public bool Funnelbeltlayer = false; // 默认 false = Belt
        public bool Weapon_Patch1_CMC;       // 敌人可持有重型武器
        public bool Weapon_Patch2_CMC;
        public bool Weapon_Patch3_CMC;       // 敌人可持有常规武器
        public float Weapon_Patch4_CMC;
        public float Weapon_Patch5_CMC = 1;
        public float Weapon_Patch6_CMC = 1;
        public float Weapon_Patch7_CMC = 1;
        public float Weapon_Patch8_CMC = 1;
        public float Weapon_Patch9_CMC = 1;
        public float TradePointFactor = 1;
        public Dictionary<string, WeaponAndAmount> dictionary_Weapon_Damage_CMC = new Dictionary<string, WeaponAndAmount>();
        public Dictionary<string, WeaponAndCooldown> dictionary_Weapon_Cooldown_CMC = new Dictionary<string, WeaponAndCooldown>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Funnelbeltlayer, "Funnelbeltlayer", false);
            Scribe_Values.Look(ref Weapon_Patch1_CMC, "Weapon_Patch_CMC", false);
            Scribe_Values.Look(ref Weapon_Patch2_CMC, "Weapon_Patch2_CMC", false);
            Scribe_Values.Look(ref Weapon_Patch3_CMC, "Weapon_Patch3_CMC", true);
            Scribe_Values.Look(ref Weapon_Patch5_CMC, "Weapon_Patch5_CMC", 1f);
            Scribe_Values.Look(ref Weapon_Patch6_CMC, "Weapon_Patch6_CMC", 1f);
            Scribe_Values.Look(ref Weapon_Patch7_CMC, "Weapon_Patch7_CMC", 1f);
            Scribe_Values.Look(ref Weapon_Patch8_CMC, "Weapon_Patch8_CMC", 1f);
            Scribe_Values.Look(ref Weapon_Patch9_CMC, "Weapon_Patch9_CMC", 1f);
            Scribe_Values.Look(ref TradePointFactor, "TradePointFactor", 1f);
        }
        public void ResetToDefaults()
        {
            Funnelbeltlayer = false;   // false = Belt
            Weapon_Patch1_CMC = false; // 重型武器
            Weapon_Patch2_CMC = false;
            Weapon_Patch3_CMC = true;  // 常规武器
            Weapon_Patch5_CMC = 1f;
            Weapon_Patch6_CMC = 1f;
            Weapon_Patch7_CMC = 1f;
            Weapon_Patch8_CMC = 1f;
            Weapon_Patch9_CMC = 1f;
            TradePointFactor = 1f;
        }
    }
    [StaticConstructorOnStartup]
    public class Settings_CMC_Main : Mod
    {
        private int ticks = 0;
        public Settings_CMC settings_CMC;
        public static Settings_CMC Settings_CMC;
        public int Damage = -1;
        public static Settings_CMC_Main Instance { get; private set; }
        public Settings_CMC_Main(ModContentPack content) : base(content)
        {
            settings_CMC = GetSettings<Settings_CMC>();
            Settings_CMC = GetSettings<Settings_CMC>();
            Instance = this;
        }
        public override string SettingsCategory()
        {
            return "Setting_CMCName".Translate();
        }
        private static void BoldLabel(Listing_Standard ls, string key, GUIStyle style)
        {
            string text = key.Translate();
            Rect rect = ls.GetRect(Text.CalcHeight(text, ls.ColumnWidth));
            GUI.Label(rect, text, style);
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            ticks++;
            bool oldHeavy = settings_CMC.Weapon_Patch1_CMC;
            bool oldNormal = settings_CMC.Weapon_Patch3_CMC;
            bool oldFunnel = settings_CMC.Funnelbeltlayer;

            Listing_Standard ls = new Listing_Standard();
            ls.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height));
            GUIStyle boldStyle = new GUIStyle(Text.CurFontStyle)
            {
                fontStyle = FontStyle.Bold
            };

            ls.GapLine(20f);
            BoldLabel(ls, "Setting_CMC", boldStyle);

            ls.CheckboxLabeled("Setting1_CMC".Translate(), ref settings_CMC.Weapon_Patch1_CMC, "Setting_D1_CMC".Translate());
            // ls.CheckboxLabeled("Setting2_CMC".Translate(), ref settings_CMC.Weapon_Patch2_CMC, "Setting_D2_CMC");
            ls.CheckboxLabeled("Setting3_CMC".Translate(), ref settings_CMC.Weapon_Patch3_CMC, "Setting_D3_CMC".Translate());

            ls.GapLine(20f);
            BoldLabel(ls, "Setting_label1_CMC", boldStyle);

            settings_CMC.Weapon_Patch5_CMC = ls.SliderLabeled("Setting5_CMC".Translate() + ": " + ((int)(settings_CMC.Weapon_Patch5_CMC / 0.1) * 0.1f), ((int)(settings_CMC.Weapon_Patch5_CMC / 0.1) * 0.1f), 0.3f, 6f, 0.3f, "Setting5_1_CMC".Translate());
            settings_CMC.Weapon_Patch6_CMC = ls.SliderLabeled("Setting6_CMC".Translate() + ": " + ((int)(settings_CMC.Weapon_Patch6_CMC / 0.1) * 0.1f), ((int)(settings_CMC.Weapon_Patch6_CMC / 0.1) * 0.1f), 0.3f, 6f, 0.3f, "Setting6_1_CMC".Translate());
            settings_CMC.Weapon_Patch7_CMC = ls.SliderLabeled("Setting7_CMC".Translate() + ": " + ((int)(settings_CMC.Weapon_Patch7_CMC / 0.1) * 0.1f), ((int)(settings_CMC.Weapon_Patch7_CMC / 0.1) * 0.1f), 0.3f, 6f, 0.3f, "Setting7_1_CMC".Translate());
            settings_CMC.Weapon_Patch9_CMC = ls.SliderLabeled("Setting9_CMC".Translate() + ": " + ((int)(settings_CMC.Weapon_Patch9_CMC / 0.1) * 0.1f), ((int)(settings_CMC.Weapon_Patch9_CMC / 0.1) * 0.1f), 0.1f, 2f, 0.3f, "Setting9_1_CMC".Translate());
            settings_CMC.Weapon_Patch8_CMC = ls.SliderLabeled("Setting8_CMC".Translate() + ": " + ((int)(settings_CMC.Weapon_Patch8_CMC / 0.1) * 0.1f), ((int)(settings_CMC.Weapon_Patch8_CMC / 0.1) * 0.1f), 0.1f, 2f, 0.3f, "Setting8_1_CMC".Translate());

            ls.GapLine(20f);
            BoldLabel(ls, "Setting_label1_CMC2", boldStyle);
            settings_CMC.TradePointFactor = ls.SliderLabeled("CMC_TradePointFactorLabel".Translate() + ": " + ((int)(settings_CMC.TradePointFactor / 0.1) * 0.1f), ((int)(settings_CMC.TradePointFactor / 0.1) * 0.1f), 0.1f, 2f, 0.3f, "CMC_TradePointFactorDesc".Translate());

            ls.GapLine(20f);
            BoldLabel(ls, "Setting_label1_CMC3", boldStyle);
            ls.CheckboxLabeled("Setting_FunnelbeltLayer_CMC".Translate(), ref settings_CMC.Funnelbeltlayer, "Setting_D_FunnelbeltLayer_CMC".Translate());

            ls.End();
            Rect resetRect = new Rect(inRect.x, inRect.yMax - 35f, 180f, 35f);
            if (Widgets.ButtonText(resetRect, "Setting_ResetAll_CMC".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "Setting_ResetAll_Confirm_CMC".Translate(),
                    delegate
                    {
                        settings_CMC.ResetToDefaults();
                        WriteSettings();
                        SimpleClass.Ado.Doing();
                    }));
            }

            if (oldHeavy != settings_CMC.Weapon_Patch1_CMC || oldNormal != settings_CMC.Weapon_Patch3_CMC)
            {
                SimpleClass.Ado.ApplyWeaponTagRules();
            }

            if (oldFunnel != settings_CMC.Funnelbeltlayer)
            {
                SimpleClass.Ado.ApplyFunnelBeltLayer();
            }

            if (ticks > 100)
            {
                SimpleClass.Ado.ApplyDamageAndPen();
                SimpleClass.Ado.ApplyCooldownAndWarmup();
                ticks = 0;
            }
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
            SimpleClass.Ado.Doing(); 
        }
    }
    [StaticConstructorOnStartup]
    class SimpleClass
    {
        public class Ado
        {
            private static readonly string[] funnelApparels = new string[]
            {
            "CMC_FunnelBelt",
            "CMC_FunnelHauler_Mix",
            "CMC_FunnelHauler_Laser"
            };
            private static readonly Dictionary<string, List<string>> originalWeaponTags = new Dictionary<string, List<string>>();
            public static void DoingList()
            {
                if (Settings_CMC_Main.Instance?.settings_CMC == null) return;

                var s = Settings_CMC_Main.Instance.settings_CMC;
                s.dictionary_Weapon_Damage_CMC.Clear();
                s.dictionary_Weapon_Cooldown_CMC.Clear();
                originalWeaponTags.Clear();

                foreach (ThingDef _thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (_thing.weaponTags == null || !_thing.IsRangedWeapon) continue;
                    if (!_thing.weaponTags.Any(x => x.EndsWith("_CMCf"))) continue;

                    originalWeaponTags[_thing.defName] = new List<string>(_thing.weaponTags);

                    if (_thing.Verbs != null && _thing.Verbs.Count > 0)
                    {
                        float warmupTime = _thing.Verbs.First().warmupTime;
                        var cooldownStat = _thing.statBases?.Find(x => x.stat == StatDefOf.RangedWeapon_Cooldown);
                        float RangedWeapon_Cooldown = cooldownStat != null ? cooldownStat.value : 1f;

                        WeaponAndCooldown weaponAndCooldown = new WeaponAndCooldown
                        {
                            weaponDef = _thing,
                            cooldownTime = RangedWeapon_Cooldown,
                            warmupTime = warmupTime
                        };
                        s.dictionary_Weapon_Cooldown_CMC[_thing.defName] = weaponAndCooldown;

                        if (_thing.Verbs.Find(x => x.defaultProjectile != null) is VerbProperties verbProjectile)
                        {
                            ThingDef defaultProjectile = verbProjectile.defaultProjectile;
                            int amount = (int)typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(defaultProjectile.projectile);
                            float armorPenetrationBase = (float)typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(defaultProjectile.projectile);

                            WeaponAndAmount weaponAndAmount = new WeaponAndAmount
                            {
                                projectile = defaultProjectile,
                                amount = amount,
                                armorPenetrationBase = armorPenetrationBase,
                                explosionRadius = defaultProjectile.projectile.explosionRadius
                            };
                            s.dictionary_Weapon_Damage_CMC[verbProjectile.defaultProjectile.defName] = weaponAndAmount;
                        }

                        if (_thing.comps != null && _thing.comps.Find(x => x is CompProperties_SecondaryVerb_Rework) is CompProperties_SecondaryVerb_Rework compProp_Secondary)
                        {
                            ThingDef defaultProjectile = compProp_Secondary.verbProps.defaultProjectile;
                            int amount = (int)typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(defaultProjectile.projectile);
                            float armorPenetrationBase = (float)typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(defaultProjectile.projectile);

                            WeaponAndAmount weaponAndAmount = new WeaponAndAmount
                            {
                                projectile = defaultProjectile,
                                amount = amount,
                                armorPenetrationBase = armorPenetrationBase,
                                explosionRadius = defaultProjectile.projectile.explosionRadius
                            };
                            s.dictionary_Weapon_Damage_CMC[compProp_Secondary.verbProps.defaultProjectile.defName] = weaponAndAmount;
                        }

                        if (_thing.comps != null && _thing.comps.Find(x => x is CompProperties_LaserData_Sustain) is CompProperties_LaserData_Sustain compProp)
                        {
                            WeaponAndAmount weaponAndAmount = new WeaponAndAmount
                            {
                                projectile = _thing,
                                amount = compProp.DamageNum,
                                ScatterExplosionDamage = compProp.ScatterExplosionDamage,
                                ScatterExplosionArmorPenetration = compProp.ScatterExplosionArmorPenetration,
                                armorPenetrationBase = compProp.DamageArmorPenetration,
                                explosionRadius = compProp.ScatterExplosionRadius
                            };
                            s.dictionary_Weapon_Damage_CMC[_thing.defName] = weaponAndAmount;
                        }
                        else if (_thing.comps != null && _thing.comps.Find(x => x is CompProperties_LaserData_Instant) is CompProperties_LaserData_Instant _compProp)
                        {
                            WeaponAndAmount weaponAndAmount = new WeaponAndAmount
                            {
                                projectile = _thing,
                                amount = _compProp.DamageNum,
                                ScatterExplosionDamage = _compProp.ScatterExplosionDamage,
                                ScatterExplosionArmorPenetration = _compProp.ScatterExplosionArmorPenetration,
                                armorPenetrationBase = _compProp.DamageArmorPenetration,
                                explosionRadius = _compProp.ScatterExplosionRadius
                            };
                            s.dictionary_Weapon_Damage_CMC[_thing.defName] = weaponAndAmount;
                        }
                    }
                }
            }

            public static void Doing()
            {
                ApplyDamageAndPen();
                ApplyCooldownAndWarmup();
                ApplyWeaponTagRules();
                ApplyFunnelBeltLayer();
            }

            public static void ApplyDamageAndPen()
            {
                if (Settings_CMC_Main.Instance?.settings_CMC == null) return;
                var s = Settings_CMC_Main.Instance.settings_CMC;

                foreach (var WAA in s.dictionary_Weapon_Damage_CMC)
                {
                    ThingDef defaultProjectile = WAA.Value.projectile;
                    if (defaultProjectile == null) continue;

                    if (defaultProjectile.projectile != null)
                    {
                        typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.NonPublic | BindingFlags.Instance)
                            .SetValue(defaultProjectile.projectile, (int)(WAA.Value.amount * s.Weapon_Patch5_CMC));

                        typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.NonPublic | BindingFlags.Instance)
                            .SetValue(defaultProjectile.projectile, WAA.Value.armorPenetrationBase * s.Weapon_Patch6_CMC);

                        if (defaultProjectile.projectile.explosionRadius > 0)
                        {
                            defaultProjectile.projectile.explosionRadius = WAA.Value.explosionRadius * s.Weapon_Patch7_CMC;
                        }
                    }
                    else if (defaultProjectile.comps != null && defaultProjectile.comps.Find(x => x is CompProperties_LaserData_Sustain) is CompProperties_LaserData_Sustain compProp)
                    {
                        compProp.DamageNum = (int)(WAA.Value.amount * s.Weapon_Patch5_CMC);
                        compProp.DamageArmorPenetration = (int)(WAA.Value.armorPenetrationBase * s.Weapon_Patch6_CMC);

                        if (compProp.ScatterExplosionDef != null)
                        {
                            compProp.ScatterExplosionRadius = WAA.Value.explosionRadius * s.Weapon_Patch7_CMC;
                            compProp.ScatterExplosionDamage = (int)(WAA.Value.ScatterExplosionDamage * s.Weapon_Patch5_CMC);
                            if (compProp.ScatterExplosionDamage <= 0) compProp.ScatterExplosionDamage = 1;
                            compProp.ScatterExplosionArmorPenetration = (int)(WAA.Value.ScatterExplosionArmorPenetration * s.Weapon_Patch6_CMC);
                        }
                    }
                    else if (defaultProjectile.comps != null && defaultProjectile.comps.Find(x => x is CompProperties_LaserData_Instant) is CompProperties_LaserData_Instant _compProp)
                    {
                        _compProp.DamageNum = (int)(WAA.Value.amount * s.Weapon_Patch5_CMC);
                        _compProp.DamageArmorPenetration = (int)(WAA.Value.armorPenetrationBase * s.Weapon_Patch6_CMC);

                        if (_compProp.ScatterExplosionDef != null)
                        {
                            _compProp.ScatterExplosionRadius = WAA.Value.explosionRadius * s.Weapon_Patch7_CMC;
                            _compProp.ScatterExplosionDamage = (int)(WAA.Value.ScatterExplosionDamage * s.Weapon_Patch5_CMC);
                            if (_compProp.ScatterExplosionDamage <= 0) _compProp.ScatterExplosionDamage = 1;
                            _compProp.ScatterExplosionArmorPenetration = (int)(WAA.Value.ScatterExplosionArmorPenetration * s.Weapon_Patch6_CMC);
                        }
                    }
                }
            }

            public static void ApplyCooldownAndWarmup()
            {
                if (Settings_CMC_Main.Instance?.settings_CMC == null) return;
                var s = Settings_CMC_Main.Instance.settings_CMC;

                foreach (var WAC in s.dictionary_Weapon_Cooldown_CMC)
                {
                    ThingDef weaponDef = WAC.Value.weaponDef;
                    if (weaponDef == null) continue;

                    var cooldown = weaponDef.statBases?.Find(x => x.stat == StatDefOf.RangedWeapon_Cooldown);
                    if (cooldown != null)
                    {
                        cooldown.value = WAC.Value.cooldownTime * s.Weapon_Patch8_CMC;
                    }

                    if (weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
                    {
                        weaponDef.Verbs.First().warmupTime = WAC.Value.warmupTime * s.Weapon_Patch9_CMC;
                    }
                }
            }

            public static void ApplyWeaponTagRules()
            {
                if (Settings_CMC_Main.Instance?.settings_CMC == null) return;
                var s = Settings_CMC_Main.Instance.settings_CMC;

                bool allowHeavy = s.Weapon_Patch1_CMC;
                bool allowNormal = s.Weapon_Patch3_CMC;

                foreach (var kv in originalWeaponTags)
                {
                    ThingDef thing = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key);
                    if (thing == null) continue;

                    List<string> rebuilt = new List<string>();

                    foreach (string tag in kv.Value)
                    {
                        if (string.IsNullOrEmpty(tag)) continue;

                        if (tag.EndsWith("_CMCf"))
                        {
                            rebuilt.Add(tag);
                            continue;
                        }

                        if (tag == "GunSuper_CMC")
                        {
                            rebuilt.Add(tag);
                            continue;
                        }

                        if (tag == "GunHeavy" || tag == "GunHeavy_CMC_")
                        {
                            rebuilt.Add(allowHeavy ? "GunHeavy" : "GunHeavy_CMC_");
                            continue;
                        }

                        if (allowNormal)
                        {
                            // 开启常规武器：移除 _CMC_ 后缀
                            if (tag.EndsWith("_CMC_")) rebuilt.Add(tag.Substring(0, tag.Length - 5));
                            else rebuilt.Add(tag);
                        }
                        else
                        {
                            // 关闭常规武器：添加 _CMC_ 后缀
                            if (tag.EndsWith("_CMC_")) rebuilt.Add(tag);
                            else rebuilt.Add(tag + "_CMC_");
                        }
                    }

                    thing.weaponTags = rebuilt.Distinct().ToList();
                }
            }

            public static void ApplyFunnelBeltLayer()
            {
                if (Settings_CMC_Main.Instance?.settings_CMC == null) return;

                bool enabled = Settings_CMC_Main.Instance.settings_CMC.Funnelbeltlayer;
                string targetLayerName = enabled ? "TOT_FunnelContainer" : "Belt";

                ApparelLayerDef targetLayer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail(targetLayerName);
                if (targetLayer == null)
                {
                    Log.Warning("[TOT_DLL_test] Missing ApparelLayerDef: " + targetLayerName);
                    return;
                }

                foreach (string defName in funnelApparels)
                {
                    ThingDef td = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                    if (td?.apparel == null) continue;
                    td.apparel.layers = new List<ApparelLayerDef> { targetLayer };
                }
            }
        }

        static readonly long baseline;
        static SimpleClass()
        {
            Ado.DoingList();
            Ado.Doing();
            baseline = DateTime.Now.Ticks;
        }
    }
}

