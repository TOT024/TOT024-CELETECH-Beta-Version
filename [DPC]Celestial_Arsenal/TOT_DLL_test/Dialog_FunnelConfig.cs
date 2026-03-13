using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TOT_DLL_test
{
    public class Dialog_ConfigureFunnels : Window
    {
        private readonly CompFunnelHauler haulerComp;
        private readonly CompApparelReloadable reloadableComp;

        private Vector2 scrollPosition = Vector2.zero;

        private const float ROW_HEIGHT = 32f;
        private const float BUTTON_HEIGHT = 30f;
        private const float GAP = 6f;
        private static readonly int[] UiOrderToSlot = { 0, 1, 2, 5, 4, 3 };
        private List<ThingDef> workingPlan = new List<ThingDef>();
        public override Vector2 InitialSize
        {
            get { return new Vector2(620f, 500f); }
        }

        public Dialog_ConfigureFunnels(CompFunnelHauler hauler)
        {
            haulerComp = hauler;
            reloadableComp = hauler.parent.GetComp<CompApparelReloadable>();

            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;

            LoadFromComp();
        }

        private int SlotCount
        {
            get
            {
                int max = reloadableComp != null ? reloadableComp.MaxCharges : 0;
                // 你的系统最多 6
                return Mathf.Clamp(max, 0, 6);
            }
        }

        private int UsedSlots
        {
            get
            {
                int count = 0;
                for (int i = 0; i < workingPlan.Count; i++)
                {
                    if (workingPlan[i] != null) count++;
                }
                return count;
            }
        }

        private void EnsureWorkingPlanSize(int n)
        {
            if (workingPlan == null) workingPlan = new List<ThingDef>();
            while (workingPlan.Count < n) workingPlan.Add(null);
            if (workingPlan.Count > n) workingPlan.RemoveRange(n, workingPlan.Count - n);
        }

        private void LoadFromComp()
        {
            workingPlan = (haulerComp.currentPlan != null)
                ? new List<ThingDef>(haulerComp.currentPlan)
                : new List<ThingDef>();

            EnsureWorkingPlanSize(SlotCount);
        }

        private void SaveToComp()
        {
            haulerComp.currentPlan = new List<ThingDef>(workingPlan);

            // 关键：你删了 droneVisuals 后，材质缓存必须重建，否则新机型可能没有 Mat
            haulerComp.Notify_PlanChanged();
        }

        private bool IsUnlockedByUiOrder(int uiIndex)
        {
            // 没槽位直接 false
            int slotCount = SlotCount;
            int slot = UiOrderToSlot[uiIndex];
            if (slot < 0 || slot >= slotCount) return false;

            // 顺序解锁：前面的都必须已配置（非 null）
            for (int j = 0; j < uiIndex; j++)
            {
                int prevSlot = UiOrderToSlot[j];
                if (prevSlot < 0 || prevSlot >= slotCount) return false;
                if (prevSlot >= workingPlan.Count) return false;
                if (workingPlan[prevSlot] == null) return false;
            }
            return true;
        }

        private List<ThingDef> GetDronePool()
        {
            if (haulerComp.Props.availableDroneDefs == null) return new List<ThingDef>();

            // 去重 + 固定顺序（按 label 排；你想按原列表顺序也行）
            return haulerComp.Props.availableDroneDefs
                .Where(d => d != null)
                .Distinct()
                .OrderBy(d => d.label)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "CMC_SetFunnelConfig".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // 容量信息
            Rect infoRect = new Rect(inRect.x, titleRect.yMax + 5f, inRect.width, 40f);
            Listing_Standard infoListing = new Listing_Standard();
            infoListing.Begin(infoRect);

            infoListing.Label("TOT_FunnelLoadoutCapacity".Translate(UsedSlots, SlotCount));

            Rect progressRect = infoListing.GetRect(22f);
            if (SlotCount > 0)
            {
                float fillPercent = (float)UsedSlots / (float)SlotCount;
                Widgets.FillableBar(progressRect, fillPercent);

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(progressRect, UsedSlots + " / " + SlotCount);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            infoListing.End();

            // 主区：6 行槽位（按 UiOrder）
            Rect mainRect = new Rect(
                inRect.x,
                infoRect.yMax + 6f,
                inRect.width,
                inRect.height - (infoRect.height + 30f + BUTTON_HEIGHT + 18f)
            );

            float viewH = UiOrderToSlot.Length * (ROW_HEIGHT + GAP);
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, viewH);

            Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);

            float curY = 0f;
            int slotCountNow = SlotCount;
            EnsureWorkingPlanSize(slotCountNow);

            List<ThingDef> pool = GetDronePool();

            for (int uiIndex = 0; uiIndex < UiOrderToSlot.Length; uiIndex++)
            {
                int slot = UiOrderToSlot[uiIndex];

                Rect rowRect = new Rect(0f, curY, viewRect.width, ROW_HEIGHT);

                // 左侧槽位描述：TOT_FunnelSlotDesc_1..6
                Rect leftRect = new Rect(rowRect.x, rowRect.y, rowRect.width * 0.45f, rowRect.height);
                string descKey = "TOT_FunnelSlotDesc_" + (slot + 1);
                Widgets.Label(leftRect, descKey.Translate());

                // 右侧按钮
                Rect btnRect = new Rect(leftRect.xMax + 8f, rowRect.y, rowRect.width - (leftRect.width + 8f), rowRect.height);

                bool slotExists = (slot >= 0 && slot < slotCountNow);

                if (!slotExists)
                {
                    Widgets.ButtonText(btnRect, "TOT_FunnelSlot_NotAvailable".Translate(), active: false);
                }
                else
                {
                    bool unlocked = IsUnlockedByUiOrder(uiIndex);

                    if (!unlocked)
                    {
                        Widgets.ButtonText(btnRect, "TOT_FunnelSlot_Locked".Translate(), active: false);
                        TooltipHandler.TipRegion(btnRect, "TOT_FunnelSlot_LockedTip".Translate());
                    }
                    else
                    {
                        ThingDef cur = (slot < workingPlan.Count) ? workingPlan[slot] : null;
                        string label = (cur != null)
                            ? "TOT_FunnelSlot_Current".Translate(cur.LabelCap)
                            : "TOT_FunnelSlot_Select".Translate();

                        if (Widgets.ButtonText(btnRect, label))
                        {
                            List<FloatMenuOption> opts = new List<FloatMenuOption>();

                            for (int i = 0; i < pool.Count; i++)
                            {
                                ThingDef def = pool[i];
                                if (def == null) continue;

                                string optLabel = "TOT_FunnelSlot_ChooseOption".Translate(def.LabelCap);
                                ThingDef captured = def;

                                opts.Add(new FloatMenuOption(optLabel, delegate
                                {
                                    EnsureWorkingPlanSize(slotCountNow);
                                    workingPlan[slot] = captured;
                                }));
                            }

                            if (opts.Count == 0)
                            {
                                opts.Add(new FloatMenuOption("TOT_FunnelSlot_NoOptions".Translate(), null));
                            }

                            Find.WindowStack.Add(new FloatMenu(opts));
                        }
                    }
                }

                curY += ROW_HEIGHT + GAP;
            }

            Widgets.EndScrollView();

            // 底部按钮
            Rect bottomRect = new Rect(inRect.x, inRect.yMax - BUTTON_HEIGHT - 10f, inRect.width, BUTTON_HEIGHT);

            Rect resetRect = new Rect(bottomRect.x, bottomRect.y, bottomRect.width * 0.2f, bottomRect.height);
            if (Widgets.ButtonText(resetRect, "Reset".Translate()))
            {
                EnsureWorkingPlanSize(slotCountNow);
                for (int i = 0; i < workingPlan.Count; i++)
                    workingPlan[i] = null;
            }

            Rect cancelRect = new Rect(bottomRect.xMax - bottomRect.width * 0.38f, bottomRect.y, bottomRect.width * 0.18f, bottomRect.height);
            Rect confirmRect = new Rect(cancelRect.xMax + 5f, bottomRect.y, bottomRect.width * 0.18f, bottomRect.height);

            if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(confirmRect, "Confirm".Translate()))
            {
                SaveToComp();
                Messages.Message("TOT_FunnelConfigSaved".Translate(), MessageTypeDefOf.PositiveEvent);
                Close();
            }
        }
    }
}