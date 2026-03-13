using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class Window_NearbyWeaponSelector : Window
    {
        private Building_WeaponModificationBench bench;
        private List<Thing> availableWeapons;
        private Vector2 scrollPosition;
        private const float RowHeight = 48f;
        private const float IconSize = 40f;

        public Window_NearbyWeaponSelector(Building_WeaponModificationBench bench)
        {
            this.bench = bench;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;

            FindWeapons();
        }

        public override Vector2 InitialSize => new Vector2(400f, 600f);

        private void FindWeapons()
        {
            availableWeapons = new List<Thing>();
            IEnumerable<Thing> nearbyThings = GenRadial.RadialDistinctThingsAround(bench.Position, bench.Map, 13f, true);

            foreach (Thing t in nearbyThings)
            {
                if (t == bench || !t.Spawned || t.Destroyed) continue;
                if (!t.def.IsWeapon) continue;

                bool hasAccessoryComp = t.TryGetComp<CompAccessoryHolder>() != null;
                bool hasRenderComp = t.TryGetComp<Comp_WeaponRenderStatic>() != null;

                if (hasAccessoryComp || hasRenderComp)
                {
                    availableWeapons.Add(t);
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            // [修改] 使用 Translate()
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), "CMC_SelectWeaponToModify".Translate());
            Text.Font = GameFont.Small;

            Rect listRect = new Rect(0, 40f, inRect.width, inRect.height - 40f);
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, availableWeapons.Count * RowHeight);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (Thing weapon in availableWeapons)
            {
                if (weapon == null || weapon.Destroyed || !weapon.Spawned) continue;

                Rect rowRect = new Rect(0, y, viewRect.width, RowHeight);

                if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);
                else if (y % (RowHeight * 2) == 0) Widgets.DrawAltRect(rowRect);

                Rect iconRect = new Rect(rowRect.x + 5f, rowRect.y + 4f, IconSize, IconSize);
                Widgets.ThingIcon(iconRect, weapon);

                Rect labelRect = new Rect(iconRect.xMax + 10f, rowRect.y, rowRect.width - iconRect.xMax - 10f, RowHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, weapon.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(rowRect))
                {
                    bench.ForceMountWeapon(weapon);
                    this.Close();
                }
                y += RowHeight;
            }

            if (availableWeapons.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(viewRect, "CMC_NoModifiableWeaponsFound".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Widgets.EndScrollView();
        }
    }
}