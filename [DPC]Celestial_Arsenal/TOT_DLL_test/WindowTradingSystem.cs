using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.Steam;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public static class CMCTextures
    {
        public static readonly Texture2D WindowBg = ContentFinder<Texture2D>.Get("UI/CMC/WindowBg");
        public static readonly Texture2D ButtonBg = ContentFinder<Texture2D>.Get("UI/CMC/ButtonBg");
        public static readonly Texture2D ButtonBg_Hover = ContentFinder<Texture2D>.Get("UI/CMC/ButtonBg_Hover");
        public static readonly Texture2D ButtonWideBg = ContentFinder<Texture2D>.Get("UI/CMC/ButtonWideBg");
        public static readonly Texture2D ButtonWideBg_Hover = ContentFinder<Texture2D>.Get("UI/CMC/ButtonWideBg_Hover");
    }

    public static class TradeWindowLayout
    {
        public const float WindowWidth = 1200f;
        public const float WindowHeight = 600f;
        public const float RightPanelWidth = 360f;
        public const float PanelSpacing = 10f;
        public const float LeftPanelWidth = WindowWidth - RightPanelWidth - PanelSpacing;
    }

    public static class TradeWindowDrawer
    {
        private static string currentDialogueKey;
        private static bool lastFrameWasAttackMode = false;
        private static readonly List<string> TradeDialogueKeys = new List<string>
        {
            "CMC_TradeGreeting_1", "CMC_TradeGreeting_2", "CMC_TradeGreeting_3", "CMC_TradeGreeting_4",
            "CMC_TradeGreeting_5", "CMC_TradeGreeting_6", "CMC_TradeGreeting_7", "CMC_TradeGreeting_8"
        };
        private static readonly List<string> AttackDialogueKeys = new List<string>
        {
            "CMC_AttackGreeting_1", "CMC_AttackGreeting_2", "CMC_AttackGreeting_3", "CMC_AttackGreeting_4",
            "CMC_AttackGreeting_5", "CMC_AttackGreeting_6","CMC_AttackGreeting_7","CMC_AttackGreeting_8",
            "CMC_AttackGreeting_9", "CMC_AttackGreeting_10"
        };
        private static List<Texture2D> tradeFrames;
        private static List<Texture2D> attackFrames;

        private const float SecondsPerAnimFrame = 0.1f;
        private const float DialogueHeight = 50f;
        private const float Spacing = 2f;

        static TradeWindowDrawer()
        {
            tradeFrames = LoadFrames("UI/CharacterPortraits_Anim");
            attackFrames = LoadFrames("UI/CharacterPortraits_Anim_atk");
            Reset(false);
        }

        private static List<Texture2D> LoadFrames(string folderPath)
        {
            var loadedTextures = ContentFinder<Texture2D>.GetAllInFolder(folderPath);
            if (loadedTextures.Count() == 0)
            {
                return new List<Texture2D> { BaseContent.BadTex };
            }
            try
            {
                return loadedTextures.OrderBy(t => int.Parse(t.name)).ToList();
            }
            catch
            {
                return loadedTextures.OrderBy(t => t.name).ToList();
            }
        }
        public static void Reset(bool isAttackMode)
        {
            List<string> targetKeys = isAttackMode ? AttackDialogueKeys : TradeDialogueKeys;
            currentDialogueKey = targetKeys.RandomElement();
            lastFrameWasAttackMode = isAttackMode;
        }
        private static void PickNewRandomDialogue(bool isAttackMode)
        {
            List<string> targetKeys = isAttackMode ? AttackDialogueKeys : TradeDialogueKeys;
            if (targetKeys.Count > 1)
            {
                string oldKey = currentDialogueKey;
                do
                {
                    currentDialogueKey = targetKeys.RandomElement();
                } while (currentDialogueKey == oldKey);
            }
            else
            {
                currentDialogueKey = targetKeys.RandomElement();
            }
        }
        public static void DrawRightPanel(Rect panelRect, bool isAttackMode = false)
        {
            if (currentDialogueKey == null || isAttackMode != lastFrameWasAttackMode)
            {
                Reset(isAttackMode);
            }

            float margin = 10f;
            Rect innerRect = panelRect.ContractedBy(margin);

            Rect portraitRect = new Rect(innerRect.x, innerRect.y, innerRect.width, innerRect.height - DialogueHeight - Spacing);
            Rect dialogueRect = new Rect(innerRect.x, portraitRect.yMax + Spacing, innerRect.width, DialogueHeight);

            List<Texture2D> currentFrames = isAttackMode ? attackFrames : tradeFrames;
            float timeElapsed = Time.realtimeSinceStartup;
            int frameIndex = (int)(timeElapsed / SecondsPerAnimFrame);
            int listIndex = frameIndex % currentFrames.Count;
            Texture2D textureToDraw = currentFrames[listIndex];

            GUI.DrawTexture(portraitRect, textureToDraw ?? BaseContent.BadTex, ScaleMode.ScaleToFit);

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(dialogueRect.ContractedBy(5f), currentDialogueKey.Translate(SteamUtility.SteamPersonaName));

            bool clickedDialogue = Widgets.ButtonInvisible(dialogueRect);
            bool clickedPortrait = Widgets.ButtonInvisible(portraitRect);
            if (clickedDialogue || clickedPortrait)
            {
                PickNewRandomDialogue(isAttackMode);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            lastFrameWasAttackMode = isAttackMode;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        public static bool DrawCustomButton(Rect rect, string label, bool active = true, Texture2D normalTex = null, Texture2D hoverTex = null)
        {
            Color originalColor = GUI.color;
            if (!active)
            {
                GUI.color = Color.gray;
            }
            else if (Mouse.IsOver(rect))
            {
                GUI.color = new Color(1f, 1f, 1f, 1f);
            }
            Texture2D finalNormal = normalTex ?? CMCTextures.ButtonBg ?? BaseContent.WhiteTex;
            Texture2D finalHover = hoverTex ?? CMCTextures.ButtonBg_Hover;

            Texture2D bgTex = finalNormal;
            if (active && Mouse.IsOver(rect) && finalHover != null)
            {
                bgTex = finalHover;
            }

            GUI.DrawTexture(rect, bgTex);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.color = originalColor;

            if (!active) return false;

            bool clicked = Widgets.ButtonInvisible(rect);
            if (clicked)
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            return clicked;
        }
    }
    [StaticConstructorOnStartup]
    public class Window_TradeInfo : Window
    {
        private static Texture2D cachedMainLogo;
        private static Texture2D cachedCombinedSmallLogo;

        private const float InfoBoxHeight = 30f;
        private const float BigLogoHeight = 290f;
        private const float SmallLogoHeight = 210f;
        private const float ButtonHeight = 40f;
        private const float SectionSpacing = 5f;
        private const float MyWindowPadding = 18f;

        public static Texture2D MainLogo => cachedMainLogo ?? (cachedMainLogo = ContentFinder<Texture2D>.Get("UI/MainLogo", true)) ?? BaseContent.BadTex;
        public static Texture2D CombinedSmallLogo => cachedCombinedSmallLogo ?? (cachedCombinedSmallLogo = ContentFinder<Texture2D>.Get("UI/UI_CMC_SmallCombined", true)) ?? BaseContent.BadTex;
        public override Vector2 InitialSize => new Vector2(TradeWindowLayout.WindowWidth, TradeWindowLayout.WindowHeight);

        public Window_TradeInfo()
        {
            this.forcePause = false;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.doWindowBackground = false;
            this.drawShadow = false;
        }
        public override void DoWindowContents(Rect inRect)
        {
            if (CMCTextures.WindowBg != null)
            {
                GUI.DrawTexture(inRect, CMCTextures.WindowBg);
            }

            Rect contentArea = inRect.ContractedBy(MyWindowPadding);

            float totalLayoutWidth = TradeWindowLayout.LeftPanelWidth + TradeWindowLayout.RightPanelWidth + TradeWindowLayout.PanelSpacing;
            float leftPanelWidth = (TradeWindowLayout.LeftPanelWidth / totalLayoutWidth) * contentArea.width;
            float rightPanelWidth = (TradeWindowLayout.RightPanelWidth / totalLayoutWidth) * contentArea.width;
            float panelSpacing = (TradeWindowLayout.PanelSpacing / totalLayoutWidth) * contentArea.width;

            Rect leftPanelRect = new Rect(contentArea.x, contentArea.y, leftPanelWidth, contentArea.height);
            Rect rightPanelRect = new Rect(leftPanelRect.xMax + panelSpacing, contentArea.y, rightPanelWidth, contentArea.height);

            DrawLeftPanel(leftPanelRect);
            TradeWindowDrawer.DrawRightPanel(rightPanelRect, false);
        }
        private void DrawLeftPanel(Rect leftRect)
        {
            float halfWidth = leftRect.width / 2f;
            Rect bigLogoRect = new Rect(leftRect.x, leftRect.y, leftRect.width, BigLogoHeight);
            GUI.DrawTexture(bigLogoRect, MainLogo, ScaleMode.ScaleToFit);
            Rect smallLogoRowRect = new Rect(leftRect.x, bigLogoRect.yMax + SectionSpacing, leftRect.width, SmallLogoHeight);
            GUI.DrawTexture(smallLogoRowRect, CombinedSmallLogo, ScaleMode.ScaleToFit);
            Rect infoRect = new Rect(leftRect.x, leftRect.yMax - InfoBoxHeight, leftRect.width, InfoBoxHeight);

            int currentPoints = 0;
            if (GameComponent_CeleTech.Instance != null)
            {
                currentPoints = GameComponent_CeleTech.Instance.CurrentPoint;
            }
            int colonySilver = (Find.CurrentMap != null) ? Find.CurrentMap.resourceCounter.GetCount(ThingDefOf.Silver) : 0;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            string combinedInfo = "CMC_CurrentPoints".Translate(currentPoints) + "   " + "CMC_ColonySilver".Translate(colonySilver);
            Widgets.Label(infoRect, combinedInfo);
            Text.Anchor = TextAnchor.UpperLeft;
            Rect buttonRow2Y = new Rect(leftRect.x, infoRect.y - ButtonHeight - 2f, leftRect.width, ButtonHeight);
            Rect buttonRow1Y = new Rect(leftRect.x, buttonRow2Y.y - ButtonHeight - SectionSpacing, leftRect.width, ButtonHeight);
            Rect supportButtonRect = new Rect(buttonRow1Y.x, buttonRow1Y.y, halfWidth, ButtonHeight);
            Rect shopButtonRect = new Rect(buttonRow1Y.x + halfWidth, buttonRow1Y.y, halfWidth, ButtonHeight);
            Rect spareButton1Rect = new Rect(buttonRow2Y.x, buttonRow2Y.y, halfWidth, ButtonHeight);
            Rect spareButton2Rect = new Rect(buttonRow2Y.x + halfWidth, buttonRow2Y.y, halfWidth, ButtonHeight);
            bool unlocked = CMCResearchProjectDefOf.CMC_CAS_Init?.IsFinished ?? false;
            if (TradeWindowDrawer.DrawCustomButton(supportButtonRect, "CMC_NavOrbitalLogistics".Translate(),unlocked,CMCTextures.ButtonWideBg,CMCTextures.ButtonWideBg_Hover)
    && unlocked)
            {
                Find.WindowStack.Add(new Window_OrbitalLogistics());
                this.Close();
            }
            if (TradeWindowDrawer.DrawCustomButton(shopButtonRect, "CMC_EnterPointsShop".Translate(), true, CMCTextures.ButtonWideBg, CMCTextures.ButtonWideBg_Hover))
            {
                Find.WindowStack.Add(new Window_PointsShop());
                this.Close();
            }
            if (TradeWindowDrawer.DrawCustomButton(spareButton1Rect, "CMC_SpareButton1".Translate(), true, CMCTextures.ButtonWideBg, CMCTextures.ButtonWideBg_Hover))
            {
                Find.WindowStack.Add(new Window_TradeShuttle());
                this.Close();
            }
            if (TradeWindowDrawer.DrawCustomButton(spareButton2Rect, "CMC_SpareButton2".Translate(), true, CMCTextures.ButtonWideBg, CMCTextures.ButtonWideBg_Hover))
            {
                Find.WindowStack.Add(new Window_SpareButton2());
                this.Close();
            }
        }
    }
    public abstract class Window_NavigationBase : Window
    {
        private const float NavBarHeight = 35f;
        private const float NavButtonWidth = 105f;
        private const float ContentSpacing = 10f;
        private const float MyWindowPadding = 18f;

        public override Vector2 InitialSize => new Vector2(TradeWindowLayout.WindowWidth, TradeWindowLayout.WindowHeight);

        public abstract void DoNavWindowContents(Rect contentRect);

        public Window_NavigationBase()
        {
            this.forcePause = false;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.doWindowBackground = false;
            this.drawShadow = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (CMCTextures.WindowBg != null)
            {
                GUI.DrawTexture(inRect, CMCTextures.WindowBg);
            }

            Rect contentArea = inRect.ContractedBy(MyWindowPadding);

            float totalLayoutWidth = TradeWindowLayout.LeftPanelWidth + TradeWindowLayout.RightPanelWidth + TradeWindowLayout.PanelSpacing;
            float leftPanelWidth = (TradeWindowLayout.LeftPanelWidth / totalLayoutWidth) * contentArea.width;
            float rightPanelWidth = (TradeWindowLayout.RightPanelWidth / totalLayoutWidth) * contentArea.width;
            float panelSpacing = (TradeWindowLayout.PanelSpacing / totalLayoutWidth) * contentArea.width;

            Rect leftPanelRect = new Rect(contentArea.x, contentArea.y, leftPanelWidth, contentArea.height);
            Rect rightPanelRect = new Rect(leftPanelRect.xMax + panelSpacing, contentArea.y, rightPanelWidth, contentArea.height);

            Rect navRect = new Rect(leftPanelRect.x, leftPanelRect.y, leftPanelRect.width, NavBarHeight);

            Rect contentRect = new Rect(
                leftPanelRect.x,
                navRect.yMax + ContentSpacing,
                leftPanelRect.width,
                leftPanelRect.height - navRect.height - ContentSpacing);

            DrawNavigationBar(navRect);
            DoNavWindowContents(contentRect);

            bool isAttackMode = this is Window_OrbitalLogistics;
            TradeWindowDrawer.DrawRightPanel(rightPanelRect, isAttackMode);
        }
        private void DrawNavigationBar(Rect navRect)
        {
            float buttonSpacing = 2f;
            int numButtons = 5;
            float totalButtonsWidth = (numButtons * NavButtonWidth) + ((numButtons - 1) * buttonSpacing);
            float curX = navRect.x + (navRect.width - totalButtonsWidth) / 2f;
            if (TradeWindowDrawer.DrawCustomButton(new Rect(curX, navRect.y, NavButtonWidth, NavBarHeight), "CMC_NavHome".Translate())) NavigateTo(new Window_TradeInfo());
            curX += NavButtonWidth + buttonSpacing;

            if (TradeWindowDrawer.DrawCustomButton(new Rect(curX, navRect.y, NavButtonWidth, NavBarHeight), "CMC_NavPointsShop".Translate(), active: !(this is Window_PointsShop))) NavigateTo(new Window_PointsShop());
            curX += NavButtonWidth + buttonSpacing;

            if (TradeWindowDrawer.DrawCustomButton(new Rect(curX, navRect.y, NavButtonWidth, NavBarHeight), "CMC_NavTradeShuttle".Translate(), active: !(this is Window_TradeShuttle))) NavigateTo(new Window_TradeShuttle());
            curX += NavButtonWidth + buttonSpacing;
            bool unlocked = CMCResearchProjectDefOf.CMC_CAS_Init?.IsFinished ?? false;
            if (TradeWindowDrawer.DrawCustomButton(new Rect(curX, navRect.y, NavButtonWidth, NavBarHeight), "CMC_NavOrbitalLogistics".Translate(), active: unlocked && !(this is Window_OrbitalLogistics))) NavigateTo(new Window_OrbitalLogistics());
            //if (TradeWindowDrawer.DrawCustomButton(new Rect(curX, navRect.y, NavButtonWidth, NavBarHeight), "CMC_NavOrbitalLogistics".Translate(), active: !(this is Window_OrbitalLogistics))) NavigateTo(new Window_OrbitalLogistics());
            curX += NavButtonWidth + buttonSpacing;

            if (TradeWindowDrawer.DrawCustomButton(new Rect(curX, navRect.y, NavButtonWidth, NavBarHeight), "CMC_NavSubscribe".Translate(), active: !(this is Window_Subscription))) NavigateTo(new Window_Subscription());
        }

        protected void NavigateTo(Window newWindow)
        {
            Find.WindowStack.Add(newWindow);
            this.Close(doCloseSound: false);
        }
    }
    public class Window_PointsShop : Window_NavigationBase
    {
        private Vector2 scrollPosition = Vector2.zero;

        private class PointsShopItem
        {
            public ThingDef def;
            public int stackSize;
            public int basePointCost;
            public float discountPercent; 
            public int? maxTotalBuy;
            public float refreshRate;
            public PointsShopItem(ThingDef def, int stackSize, int basePointCost, float discountPercent = 0f, int? maxTotalBuy = null, float refreshRate = 1f)
            {
                this.def = def;
                this.stackSize = stackSize;
                this.basePointCost = basePointCost;
                this.discountPercent = Mathf.Clamp01(discountPercent);
                this.maxTotalBuy = maxTotalBuy;
                this.refreshRate = Mathf.Clamp01(refreshRate);
            }
            public int GetCurrentCost(GameComponent_CeleTech comp)
            {
                float factor = 1f - discountPercent;     
                if (comp != null)
                    factor *= comp.GetCycleDiscountFactor(def);

                int c = Mathf.RoundToInt(basePointCost * factor);
                return Mathf.Max(1, c);
            }
            public int GetRemainingBuyable(GameComponent_CeleTech comp)
            {
                if (!maxTotalBuy.HasValue) return int.MaxValue;
                int already = comp.GetPurchasedBundles(def);
                return Mathf.Max(0, maxTotalBuy.Value - already);
            }
        }
        private static readonly List<PointsShopItem> shopInventory = new List<PointsShopItem>();
        private readonly Dictionary<string, int> cart = new Dictionary<string, int>();
        static Window_PointsShop()
        {
            try
            {
                shopInventory.Add(new PointsShopItem(ThingDefOf.Steel, 500, 2500));
                shopInventory.Add(new PointsShopItem(ThingDefOf.Uranium, 400, 2500));
                shopInventory.Add(new PointsShopItem(ThingDefOf.Plasteel, 150, 2500));    
                shopInventory.Add(new PointsShopItem(ThingDefOf.Gold, 70, 3000));                  
                shopInventory.Add(new PointsShopItem(ThingDefOf.ComponentIndustrial, 50, 5000));
                shopInventory.Add(new PointsShopItem(ThingDefOf.ComponentSpacer, 10, 5000)); 
                ThingDef rifleDef = DefDatabase<ThingDef>.GetNamed("QBZ_fivenineFlame", false);
                if (rifleDef != null)
                    shopInventory.Add(new PointsShopItem(rifleDef, 1, 5000, maxTotalBuy: 3, refreshRate : 0.5f));
                ThingDef rifleDef1 = DefDatabase<ThingDef>.GetNamed("CMC_QBAfournineFlame", false);
                if (rifleDef1 != null)
                    shopInventory.Add(new PointsShopItem(rifleDef1, 1, 3000, maxTotalBuy: 4, refreshRate: 0.75f));
                ThingDef rifleDef2 = DefDatabase<ThingDef>.GetNamed("QBU71_QS", false);
                if (rifleDef2 != null)
                    shopInventory.Add(new PointsShopItem(rifleDef2, 1, 15000, maxTotalBuy: 2, refreshRate: 0.15f));
            }
            catch
            {
                Log.Error("CMC Trading system: Stock failed to generate");
            }
        }

        public Window_PointsShop()
        {
            foreach (var item in shopInventory)
                cart[item.def.defName] = 0;
        }

        private int GetCartCount(PointsShopItem item) => cart.TryGetValue(item.def.defName, out int v) ? v : 0;
        private void SetCartCount(PointsShopItem item, int count) => cart[item.def.defName] = Mathf.Max(0, count);

        private const float RowHeight = 45f;
        private const float IconSize = 35f;
        private const float FooterHeight = 60f;

        public override void DoNavWindowContents(Rect contentRect)
        {
            Widgets.DrawMenuSection(contentRect);
            Rect innerContentRect = contentRect.ContractedBy(10f);

            var gameComp = GameComponent_CeleTech.Instance;
            if (gameComp == null) return;

            int currentPoints = gameComp.CurrentPoint;

            Rect headerRect = new Rect(innerContentRect.x, innerContentRect.y, innerContentRect.width, 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "CMC_AvailablePoints".Translate(currentPoints));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect listRect = new Rect(innerContentRect.x, headerRect.yMax + 5f, innerContentRect.width,
                innerContentRect.height - headerRect.height - FooterHeight - 10f);
            DrawShopList(listRect, gameComp);

            Rect footerRect = new Rect(innerContentRect.x, listRect.yMax + 5f, innerContentRect.width, FooterHeight);
            DrawFooter(footerRect, gameComp);
        }
        private void DrawShopList(Rect rect, GameComponent_CeleTech gameComp)
        {
            Widgets.DrawMenuSection(rect);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, shopInventory.Count * RowHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;
            int index = 0;
            foreach (var item in shopInventory)
            {
                Rect rowRect = new Rect(0f, curY, viewRect.width, RowHeight);
                if (index % 2 == 0) Widgets.DrawHighlight(rowRect);
                DrawItemRow(rowRect, item, gameComp);
                curY += RowHeight;
                index++;
            }

            Widgets.EndScrollView();
        }
        private int CalcTotalCost(GameComponent_CeleTech gameComp)
        {
            int total = 0;
            foreach (var item in shopInventory)
            {
                int buy = GetCartCount(item);
                total += GetRowTotalCost(item, buy, gameComp);
            }
            return total;
        }
        private int GetHalfCost(int cost) => Mathf.Max(1, Mathf.RoundToInt(cost * 0.5f));

        private int GetRowTotalCost(PointsShopItem item, int buyCount, GameComponent_CeleTech gameComp)
        {
            if (buyCount <= 0) return 0;

            int unitCost = item.GetCurrentCost(gameComp);
            if (gameComp.IsFirstPurchase(item.def))
            {
                return GetHalfCost(unitCost) + unitCost * (buyCount - 1);
            }

            return unitCost * buyCount;
        }
        private void DrawItemRow(Rect rect, PointsShopItem item, GameComponent_CeleTech gameComp)
        {
            int buyCount = GetCartCount(item);
            int currentCost = item.GetCurrentCost(gameComp);
            int remaining = item.GetRemainingBuyable(gameComp);

            bool soldOut = remaining <= 0;
            bool firstPurchaseDiscount = gameComp.IsFirstPurchase(item.def);

            if (soldOut && buyCount > 0)
            {
                SetCartCount(item, 0);
                buyCount = 0;
            }

            if (soldOut)
                Widgets.DrawBoxSolid(rect, new Color(0.23f, 0.23f, 0.23f, 0.9f));

            Widgets.DrawHighlightIfMouseover(rect);

            float curX = rect.x + 5f;
            Color oldColor = GUI.color;

            Rect iconRect = new Rect(curX, rect.y + (rect.height - IconSize) / 2f, IconSize, IconSize);
            if (soldOut) GUI.color = new Color(1f, 1f, 1f, 0.67f);
            Widgets.ThingIcon(iconRect, item.def);
            GUI.color = oldColor;
            curX += IconSize + 10f;

            Rect infoRect = new Rect(curX, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Widgets.InfoCardButton(infoRect, item.def);
            curX += 30f;

            Rect nameRect = new Rect(curX, rect.y, 260f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            string remainingText = remaining == int.MaxValue ? "∞" : remaining.ToString();
            string remainingLabel = "CMC_PS_Remaining".Translate(remainingText);
            if (soldOut) GUI.color = new Color(0.78f, 0.78f, 0.78f);
            Widgets.Label(nameRect, "CMC_PS_NameWithRemaining".Translate(item.def.LabelCap, item.stackSize, remainingLabel));
            GUI.color = oldColor;
            curX += 260f;

            Rect priceRect = new Rect(curX, rect.y, 220f, rect.height);
            int discountPct = Mathf.RoundToInt((1f - (float)currentCost / Mathf.Max(1, item.basePointCost)) * 100f); // 合并折扣展示
            bool hasDiscount = discountPct > 0;

            if (soldOut) GUI.color = new Color(0.78f, 0.78f, 0.78f);
            else if (hasDiscount || firstPurchaseDiscount) GUI.color = new Color32(202, 255, 86, 255);

            string basePriceText = hasDiscount
                ? "CMC_PS_PriceWithDiscount".Translate(currentCost, discountPct)
                : "CMC_PS_PriceNormal".Translate(currentCost);

            if (firstPurchaseDiscount)
            {
                int half = GetHalfCost(currentCost);
                basePriceText += "  " + "CMC_PS_FirstBundleHalf".Translate(half);
            }

            Widgets.Label(priceRect, basePriceText);
            GUI.color = oldColor;
            curX += 220f;
            int increment = Event.current.control ? 100 : (Event.current.shift ? 10 : 1);
            float controlsWidth = 24f + 5f + 50f + 5f + 24f;
            Rect controlsRect = new Rect(curX, rect.y, controlsWidth, rect.height);

            if (!soldOut)
            {
                Rect minusBtnRect = new Rect(curX, rect.y + 10f, 24f, 24f);
                if (Widgets.ButtonText(minusBtnRect, "-"))
                {
                    SetCartCount(item, buyCount - increment);
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    buyCount = GetCartCount(item);
                }
                curX += 29f;

                Rect countRect = new Rect(curX, rect.y, 50f, rect.height);
                Text.Anchor = TextAnchor.MiddleCenter;
                if (buyCount == 0) GUI.color = Color.gray;
                Widgets.Label(countRect, buyCount.ToString());
                GUI.color = oldColor;
                Text.Anchor = TextAnchor.UpperLeft;
                curX += 55f;

                Rect plusBtnRect = new Rect(curX, rect.y + 10f, 24f, 24f);
                if (Widgets.ButtonText(plusBtnRect, "+"))
                {
                    int maxCanAdd = remaining == int.MaxValue ? increment : Mathf.Max(0, remaining - buyCount);
                    if (maxCanAdd > 0)
                    {
                        SetCartCount(item, buyCount + Mathf.Min(increment, maxCanAdd));
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    else
                    {
                        Messages.Message("CMC_PS_LimitReached".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
                curX += 45f;
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.78f, 0.78f, 0.78f);
                Widgets.Label(controlsRect, "CMC_PS_SoldOut".Translate());
                GUI.color = oldColor;
                Text.Anchor = TextAnchor.UpperLeft;
                curX += controlsWidth + 16f;
            }
            int rowTotal = GetRowTotalCost(item, GetCartCount(item), gameComp);
            if (rowTotal > 0)
            {
                Rect totalRect = new Rect(curX, rect.y, 110f, rect.height);
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = soldOut ? new Color(0.78f, 0.78f, 0.78f) : new Color(1f, 0.8f, 0.2f);
                Widgets.Label(totalRect, rowTotal.ToString());
                GUI.color = oldColor;
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }
        private bool HasAnyItemInCart()
        {
            foreach (var kv in cart)
                if (kv.Value > 0) return true;
            return false;
        }
        public static void RollCycleDiscounts(GameComponent_CeleTech comp)
        {
            if (comp == null) return;
            List<PointsShopItem> pool = new List<PointsShopItem>();
            foreach (var item in shopInventory)
            {
                if (item?.def == null) continue;
                if (comp.IsFirstPurchase(item.def)) continue;
                if (item.GetRemainingBuyable(comp) <= 0) continue;
                float rate = Mathf.Clamp01(item.refreshRate);
                if (!Rand.Chance(rate)) continue;
                pool.Add(item);
            }
            if (pool.Count == 0) return;
            int pickCount = Mathf.Min(pool.Count, Rand.RangeInclusive(1, 2));
            for (int i = 0; i < pickCount; i++)
            {
                int idx = Rand.Range(0, pool.Count);
                PointsShopItem picked = pool[idx];
                pool.RemoveAt(idx);
                float factor = Rand.Range(0.88f, 0.90f);
                comp.SetCycleDiscountFactor(picked.def, factor);
            }
        }
        private void DrawFooter(Rect rect, GameComponent_CeleTech gameComp)
        {
            int totalCost = CalcTotalCost(gameComp);
            bool canAfford = gameComp.CurrentPoint >= totalCost;
            bool hasItems = HasAnyItemInCart();
            bool mapReady = Find.CurrentMap != null;

            float buttonWidth = 140f;
            float buttonHeight = 40f;
            float spacing = 10f;

            Rect clearRect = new Rect(rect.x, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
            if (TradeWindowDrawer.DrawCustomButton(clearRect, "CMC_ClearCart".Translate(), active: hasItems))
            {
                foreach (var item in shopInventory) SetCartCount(item, 0);
                SoundDefOf.Click.PlayOneShotOnCamera();
            }

            Rect totalRect = new Rect(rect.x + buttonWidth + spacing, rect.y,
                rect.width - (buttonWidth * 2) - (spacing * 2), rect.height);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;

            string costLabel = "CMC_TotalCost".Translate(totalCost);
            if (!canAfford)
            {
                GUI.color = Color.red;
                costLabel += $" ({"CMC_InsufficientPoints".Translate()})";
            }
            else if (hasItems)
            {
                GUI.color = Color.green;
            }
            Widgets.Label(totalRect, costLabel);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect confirmRect = new Rect(rect.xMax - buttonWidth, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
            if (TradeWindowDrawer.DrawCustomButton(confirmRect, "CMC_ConfirmTrade".Translate(), canAfford && hasItems && mapReady))
            {
                ExecuteTrade(gameComp, totalCost);
            }
        }
        private void ExecuteTrade(GameComponent_CeleTech gameComp, int totalCost)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            gameComp.CurrentPoint -= totalCost;
            ActiveTransporterInfo podInfo = new ActiveTransporterInfo();

            foreach (var item in shopInventory)
            {
                int bundles = GetCartCount(item);
                if (bundles <= 0) continue;

                Thing t = ThingMaker.MakeThing(item.def);
                t.stackCount = item.stackSize * bundles;
                podInfo.innerContainer.TryAdd(t);
                gameComp.AddPurchasedBundles(item.def, bundles);

                SetCartCount(item, 0);
            }

            if (podInfo.innerContainer.Count > 0)
            {
                IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
                ThingDef airplaneDef = DefDatabase<ThingDef>.GetNamed("CMC_DeliveryAirplane");
                AirplaneDeliveryFlyer airplane = (AirplaneDeliveryFlyer)ThingMaker.MakeThing(airplaneDef);
                GenSpawn.Spawn(airplane, dropSpot, map, WipeMode.Vanish);
                airplane.SetupFlight(dropSpot, map, podInfo);
                Messages.Message("CMC_TradeComplete".Translate(totalCost), new TargetInfo(dropSpot, map), MessageTypeDefOf.PositiveEvent);
            }
        }
    }
    public class Window_TradeShuttle : Window_NavigationBase
    {
        private Vector2 scrollPosition = Vector2.zero;
        private const float ShuttleEntryHeight = 100f;
        private const int SupplyShuttleCooldownHours = 24;
        private const int ArsenalShuttleCooldownHours = 48;
        private const int SlaveShuttleCooldownHours = 144;
        private List<ShuttleEntry> shuttleEntries;
        private float totalViewHeight;

        private void InitializeShuttles()
        {
            if (this.shuttleEntries != null) return;
            this.shuttleEntries = new List<ShuttleEntry>
            {
                new ShuttleEntry(
                    shipThingName: "CMC_TraderShuttle",
                    traderDefName: "CMC_OrbitalTrader_Materials",
                    iconPath: "Things/Skyfaller/TradeShuttle",
                    bgColor: new Color(0.1f, 0.1f, 0.1f, 0.5f),
                    getCooldown: () => GameComponent_CeleTech.Instance.LastAuxHr,
                    resetCooldown: () => GameComponent_CeleTech.Instance.LastAuxHr = 0,
                    cooldownHours: SupplyShuttleCooldownHours,
                    earlyCost: 2500,
                    descriptionKey: "CMC_SupplyShuttleDesc"
                ),
                new ShuttleEntry(
                    shipThingName: "CMC_TraderShuttle_A",
                    traderDefName: "CMC_OrbitalTrader_Weapons",
                    iconPath: "Things/Skyfaller/TradeShuttle_A",
                    bgColor: new Color(0.33f, 0.33f, 0.33f, 0.5f),
                    getCooldown: () => GameComponent_CeleTech.Instance.LastArsHr,
                    resetCooldown: () => GameComponent_CeleTech.Instance.LastArsHr = 0,
                    cooldownHours: ArsenalShuttleCooldownHours,
                    earlyCost: 3000,
                    descriptionKey: "CMC_ArsenalShuttleDesc"
                ),
                new ShuttleEntry(
                    shipThingName: "CMC_TraderShuttle_S",
                    traderDefName: "CMC_OrbitalTrader_Slaves",
                    iconPath: "Things/Skyfaller/TradeShuttle_S",
                    bgColor: new Color(0.1f, 0.1f, 0.1f, 0.5f),
                    getCooldown: () => GameComponent_CeleTech.Instance.LastSSHr,
                    resetCooldown: () => GameComponent_CeleTech.Instance.LastSSHr = 0,
                    cooldownHours: SlaveShuttleCooldownHours,
                    earlyCost: 4500,
                    descriptionKey: "CMC_SlaveShuttleDesc"
                )
            };
            this.totalViewHeight = (ShuttleEntryHeight + 10f) * this.shuttleEntries.Count;
        }

        public override void DoNavWindowContents(Rect contentRect)
        {
            Widgets.DrawMenuSection(contentRect);
            Rect innerContentRect = contentRect.ContractedBy(10f);
            if (GameComponent_CeleTech.Instance == null)
            {
                return;
            }
            int colonySilver = (Find.CurrentMap != null) ? Find.CurrentMap.resourceCounter.GetCount(ThingDefOf.Silver) : 0;
            Rect headerRect = new Rect(innerContentRect.x, innerContentRect.y, innerContentRect.width, 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Small;
            Widgets.Label(headerRect, "CMC_ColonySilver".Translate(colonySilver));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            this.InitializeShuttles();
            Rect outRect = new Rect(innerContentRect.x, headerRect.yMax + 10f, innerContentRect.width, innerContentRect.height - headerRect.height - 10f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, this.totalViewHeight);

            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            if (this.shuttleEntries != null)
            {
                for (int i = 0; i < this.shuttleEntries.Count; i++)
                {
                    DrawGenericShuttleEntry(listing, this.shuttleEntries[i], i);
                }
            }
            listing.End();
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawGenericShuttleEntry(Listing_Standard listing, ShuttleEntry entry, int index)
        {
            if (entry.TraderKindDef == null) return;
            int currentHoursSinceCall = entry.GetCurrentCooldownHours();
            bool onCooldown = currentHoursSinceCall < entry.CooldownDurationHours;
            int hoursRemaining = entry.CooldownDurationHours - currentHoursSinceCall;
            if (hoursRemaining < 0) hoursRemaining = 0;
            Rect entryRect = listing.GetRect(ShuttleEntryHeight);
            if (index % 2 == 0) Widgets.DrawHighlight(entryRect);
            Widgets.DrawBoxSolid(entryRect, entry.BackgroundColor);
            Widgets.DrawHighlightIfMouseover(entryRect);
            Rect iconRect = new Rect(entryRect.x + 10f, entryRect.y + 10f, 80f, 80f);
            GUI.DrawTexture(iconRect, entry.Icon, ScaleMode.ScaleToFit);
            Rect buttonRect = new Rect(entryRect.xMax - 160f, entryRect.y + (entryRect.height - 40f) / 2f, 150f, 40f);
            float infoWidth = buttonRect.x - iconRect.xMax - 20f;
            Rect infoRect = new Rect(iconRect.xMax + 10f, entryRect.y + 10f, infoWidth, 80f);
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(infoRect.x, infoRect.y, infoRect.width, 30f);
            Widgets.Label(titleRect, entry.TraderKindDef.label.CapitalizeFirst());
            Text.Font = GameFont.Small;
            Rect descRect = new Rect(infoRect.x, titleRect.yMax, infoRect.width, 25f);
            Widgets.Label(descRect, entry.DescriptionKey.Translate());
            Rect statusRect = new Rect(infoRect.x, descRect.yMax, infoRect.width, 25f);
            string buttonLabel = "";
            bool canCall = false;
            int colonySilver = 0;
            if (Find.CurrentMap != null)
                colonySilver = Find.CurrentMap.resourceCounter.Silver;
            if (onCooldown)
            {
                Widgets.Label(statusRect, "CMC_CooldownHours".Translate(hoursRemaining));
                buttonLabel = "CMC_CallEarlyCost".Translate(entry.EarlyCallCost);
                if (colonySilver >= entry.EarlyCallCost)
                {
                    canCall = true;
                }
            }
            else
            {
                Widgets.Label(statusRect, "CMC_StatusReady".Translate());
                buttonLabel = "CMC_CallShuttle".Translate();
                canCall = true;
            }
            if (TradeWindowDrawer.DrawCustomButton(buttonRect, buttonLabel, canCall))
            {
                HandleShuttleCall(onCooldown, entry);
            }
        }

        private void HandleShuttleCall(bool wasOnCooldown, ShuttleEntry entry)
        {
            try
            {
                SpawnTradeShip spawnTradeShip = new SpawnTradeShip();
                if (spawnTradeShip.SpawnShip(entry.TraderKindDef.defName))
                {
                    if (wasOnCooldown)
                    {
                        Messages.Message("CMC_PaidEarlyCall".Translate(entry.EarlyCallCost), MessageTypeDefOf.PositiveEvent);
                        TradeUtility.LaunchSilver(Find.CurrentMap, entry.EarlyCallCost);
                    }
                    entry.ResetCooldown();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error calling shuttle {entry.TraderKindDef.defName}: {ex.Message}");
            }
        }
    }
    public class Window_OrbitalLogistics : Window_NavigationBase
    {
        private class AttackOption
        {
            public string StrikeId;
            public string LabelKey;
            public int Cost;
            public string DescKey;
            public Action OnSelect;
            public ThingDef IconDef;
            public int AircraftNeededPerCall; 
            public int AttackIntervalTicks; 
            public int RearmTicks;        

            public AttackOption(string strikeId, string label, int cost, string desc, string thingDefName,
                int aircraftNeededPerCall, int attackIntervalTicks, int rearmTicks, Action onSelect)
            {
                StrikeId = strikeId;
                LabelKey = label;
                Cost = cost;
                DescKey = desc;
                OnSelect = onSelect;
                AircraftNeededPerCall = Mathf.Max(1, aircraftNeededPerCall);
                AttackIntervalTicks = Mathf.Max(0, attackIntervalTicks);
                RearmTicks = Mathf.Max(0, rearmTicks);

                if (!string.IsNullOrEmpty(thingDefName))
                    IconDef = DefDatabase<ThingDef>.GetNamed(thingDefName, false);
            }
        }
        private List<AttackOption> options;
        private Vector2 scrollPosition = Vector2.zero;
        private static bool reopenOnSelect = true;
        public Window_OrbitalLogistics()
        {
            InitializeOptions();
        }

        private void InitializeOptions()
        {
            options = new List<AttackOption>();
            AirstrikeConfig ACConfig = new AirstrikeConfig
            {
                projectileDefName = "Bullet_HMG_HE",
                burstCount = 20,
                burstInterval = 1,
                strafeLength = 30f,
                strafeSpread = 3.1f,
                flightAltitude = 15f,
                speed = 60f,
                projectilepershot = 6
            };
            options.Add(new AttackOption(
                strikeId: "CMC_Airstrike_AC",
                label: "CMC_Airstrike_AC",
                cost: 500,
                desc: "CMC_Airstrike_ACDesc",
                thingDefName: "CMC_FighterJet",
                aircraftNeededPerCall: 1,
                attackIntervalTicks: 420,
                rearmTicks: GenDate.TicksPerHour * 3,
                onSelect: delegate { StartTargeting_Airstrike("CMC_Airstrike_AC", 500, ACConfig, 1, 420, GenDate.TicksPerHour * 3); }
            ));
            AirstrikeConfig carpetBombConfig = new AirstrikeConfig
            {
                projectileDefName = "CMC_Missile_ASG",
                burstCount = 8,
                strafeLength = 25f,
                strafeSpread = 3f,
                flightAltitude = 15f,
                speed = 50f,
                projectilepershot = 2
            };
            options.Add(new AttackOption(
                strikeId: "CMC_Airstrike",
                label: "CMC_Airstrike",
                cost: 500,
                desc: "CMC_AirstrikeDesc",
                thingDefName: "CMC_FighterJet",
                aircraftNeededPerCall: 1,
                attackIntervalTicks: 420,
                rearmTicks: GenDate.TicksPerHour * 3,
                onSelect: delegate { StartTargeting_Airstrike("CMC_Airstrike", 500, carpetBombConfig, 1, 420, GenDate.TicksPerHour * 3); }
            ));
            AirstrikeConfig atgmConfig = new AirstrikeConfig
            {
                projectileDefName = "CMC_Missile_ASG_Homing",
                burstCount = 2,
                burstInterval = 4,
                strafeLength = 5f,
                strafeSpread = 0.5f,
                flightAltitude = 15f,
                speed = 80f,
                homing = true,
                attackRange = 90f
            };
            options.Add(new AttackOption(
                strikeId: "CMC_ATGMStrike",
                label: "CMC_ATGMStrike",
                cost: 500,
                desc: "CMC_ATGMStrikeDesc",
                thingDefName: "CMC_FighterJetC",
                aircraftNeededPerCall: 1,
                attackIntervalTicks: 420,
                rearmTicks: GenDate.TicksPerHour * 4,
                onSelect: delegate { StartTargeting_Airstrike("CMC_ATGMStrike", 500, atgmConfig, 1, 420, GenDate.TicksPerHour * 4); }
            ));

            AirstrikeConfig atgmLConfig = new AirstrikeConfig
            {
                projectileDefName = "CMC_Missile_ASG_Homing_Huge",
                burstCount = 1,
                burstInterval = 0,
                strafeLength = 5f,
                strafeSpread = 0.5f,
                flightAltitude = 33f,
                speed = 90f,
                homing = true,
                attackRange = 120f
            };
            options.Add(new AttackOption(
                strikeId: "CMC_ATGM_LStrike",
                label: "CMC_ATGM_LStrike",
                cost: 800,
                desc: "CMC_ATGM_LStrikeDesc",
                thingDefName: "CMC_FighterJetC",
                aircraftNeededPerCall: 1,
                attackIntervalTicks: 420,
                rearmTicks: GenDate.TicksPerHour * 3,
                onSelect: delegate { StartTargeting_Airstrike("CMC_ATGM_LStrike", 800, atgmLConfig, 1, 420, GenDate.TicksPerHour * 5); }
            ));

            AirstrikeConfig precisionEMPConfig = new AirstrikeConfig
            {
                projectileDefName = "CMC_PrecisionBomb_EMP",
                burstCount = 1,
                burstInterval = 0,
                strafeLength = 0f,
                strafeSpread = 0.5f,
                flightAltitude = 25f,
                speed = 80f
            };
            options.Add(new AttackOption(
                strikeId: "CMC_PrecisionEMPStrike",
                label: "CMC_PrecisionEMPStrike",
                cost: 300,
                desc: "CMC_PrecisionEMPStrikeDesc",
                thingDefName: "CMC_FighterJetD",
                aircraftNeededPerCall: 1,
                attackIntervalTicks: 0,
                rearmTicks: GenDate.TicksPerHour * 3,
                onSelect: delegate { StartTargeting_Airstrike("CMC_PrecisionEMPStrike", 300, precisionEMPConfig, 1, 420, GenDate.TicksPerHour * 2); }
            ));

            AirstrikeConfig precisionConfig = new AirstrikeConfig
            {
                projectileDefName = "CMC_PrecisionBomb",
                burstCount = 1,
                burstInterval = 0,
                strafeLength = 0f,
                strafeSpread = 0.5f,
                flightAltitude = 25f,
                speed = 80f
            };
            options.Add(new AttackOption(
                strikeId: "CMC_PrecisionStrike",
                label: "CMC_PrecisionStrike",
                cost: 1500,
                desc: "CMC_PrecisionStrikeDesc",
                thingDefName: "CMC_FighterJetB",
                aircraftNeededPerCall: 1,
                attackIntervalTicks: 0,
                rearmTicks: GenDate.TicksPerHour * 3,
                onSelect: delegate { StartTargeting_Airstrike("CMC_PrecisionStrike", 1500, precisionConfig, 1, 420 * 4, GenDate.TicksPerHour * 3); }
            ));
        }
        private void StartTargeting_Airstrike(string strikeId, int cost, AirstrikeConfig config, int aircraftNeeded, int attackCdTicks, int rearmTicks)
        {
            Map currentMap = Find.CurrentMap;
            if (currentMap == null) return;

            GameComponent_CeleTech gameComp = GameComponent_CeleTech.Instance;
            if (gameComp == null) return;

            TargetingParameters parms = new TargetingParameters();
            parms.canTargetLocations = true;
            parms.canTargetPawns = true;
            parms.canTargetBuildings = true;

            Find.Targeter.BeginTargeting(parms, delegate (LocalTargetInfo strikeTarget)
            {
                if (!strikeTarget.IsValid) return;

                bool manualDirection = IsManualDirectionEnabled(gameComp, config);
                if (manualDirection)
                {
                    BeginDirectionTargeting(strikeId, cost, config, aircraftNeeded, attackCdTicks, rearmTicks, strikeTarget, currentMap, gameComp);
                }
                else
                {
                    TryLaunchAfterTargeting(strikeId, cost, config, aircraftNeeded, attackCdTicks, rearmTicks, strikeTarget, currentMap, gameComp, false, Vector3.zero);
                }
            }, null, null, ContentFinder<Texture2D>.Get("UI/Commands/Attack"));
        }
        private void BeginDirectionTargeting(
    string strikeId,
    int cost,
    AirstrikeConfig config,
    int aircraftNeeded,
    int attackCdTicks,
    int rearmTicks,
    LocalTargetInfo strikeTarget,
    Map map,
    GameComponent_CeleTech gameComp)
        {
            if (map == null || gameComp == null) return;
            if (!strikeTarget.IsValid || !strikeTarget.Cell.InBounds(map)) return;

            Messages.Message("CMC_PickDirectionNow".Translate(), MessageTypeDefOf.NeutralEvent);

            TargetingParameters dirParms = new TargetingParameters();
            dirParms.canTargetLocations = true;
            dirParms.canTargetPawns = false;
            dirParms.canTargetBuildings = false;

            Func<LocalTargetInfo, bool> validator = delegate (LocalTargetInfo x)
            {
                if (!x.IsValid) return false;
                if (!x.Cell.InBounds(map)) return false;
                if (x.Cell == strikeTarget.Cell) return false;
                return true;
            };

            Find.Targeter.BeginTargeting(
                targetParams: dirParms,
                action: delegate (LocalTargetInfo dirTarget)
                {
                    if (!dirTarget.IsValid || !dirTarget.Cell.InBounds(map))
                    {
                        Messages.Message("CMC_InvalidDirection".Translate(), MessageTypeDefOf.RejectInput);
                        return;
                    }

                    Vector3 manualDir = dirTarget.Cell.ToVector3Shifted() - strikeTarget.Cell.ToVector3Shifted();
                    manualDir.y = 0f;

                    if (manualDir.sqrMagnitude < 0.001f)
                    {
                        Messages.Message("CMC_InvalidDirection".Translate(), MessageTypeDefOf.RejectInput);
                        return;
                    }

                    TryLaunchAfterTargeting(
                        strikeId,
                        cost,
                        config,
                        aircraftNeeded,
                        attackCdTicks,
                        rearmTicks,
                        strikeTarget,
                        map,
                        gameComp,
                        true,
                        manualDir
                    );
                },
                highlightAction: null,
                targetValidator: validator,
                caster: null,
                actionWhenFinished: null,
                mouseAttachment: ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                playSoundOnAction: true,
                onGuiAction: null,
                onUpdateAction: delegate (LocalTargetInfo hoverTarget)
                {
                    AirstrikeAimUtility.DrawDirectionPickPreviewWithAnchor(strikeTarget.Cell, hoverTarget, map, config);
                }
            );
        }
        private void TryLaunchAfterTargeting(string strikeId, int cost, AirstrikeConfig config, int aircraftNeeded, int attackCdTicks, int rearmTicks,
    LocalTargetInfo strikeTarget, Map map, GameComponent_CeleTech gameComp, bool useManualDirection, Vector3 manualDirection)
        {
            string fail;
            bool ok = gameComp.TryConsumeForLaunch(strikeId, cost, attackCdTicks, rearmTicks, aircraftNeeded, out fail);
            if (!ok)
            {
                string msg = fail;
                if (string.IsNullOrEmpty(msg))
                {
                    msg = "CMC_ActionUnavailable".Translate().ToString();
                }
                Messages.Message(msg, MessageTypeDefOf.RejectInput);
                return;
            }

            if (!config.homing)
            {
                ExecuteAirstrike(strikeTarget.Cell, map, aircraftNeeded, config, useManualDirection, manualDirection);
            }
            else
            {
                ExecuteAirstrike_Homing(strikeTarget, map, aircraftNeeded, config);
            }

            if (reopenOnSelect)
            {
                Find.WindowStack.Add(new Window_OrbitalLogistics());
            }
        }
        private void ExecuteAirstrike(IntVec3 targetCell, Map map, int aircraftNeeded, AirstrikeConfig config, bool useManualDirection, Vector3 manualDirection)
        {
            ThingDef jetDef = DefDatabase<ThingDef>.GetNamed("CMC_FighterJet", false);
            if (jetDef == null) return;

            for (int i = 0; i < aircraftNeeded; i++)
            {
                AirplaneAttackFlyer jet = (AirplaneAttackFlyer)ThingMaker.MakeThing(jetDef);
                GenSpawn.Spawn(jet, targetCell, map, WipeMode.Vanish);
                jet.SetupAttackRun(new LocalTargetInfo(targetCell), map, config, useManualDirection, manualDirection);
            }

            Messages.Message("CMC_AirstrikeInbound".Translate(), new TargetInfo(targetCell, map), MessageTypeDefOf.PositiveEvent);
        }
        private void ExecuteAirstrike_Homing(LocalTargetInfo target, Map map, int aircraftNeeded, AirstrikeConfig config)
        {
            ThingDef jetDef = DefDatabase<ThingDef>.GetNamed("CMC_FighterJet", false);
            if (jetDef == null) return;

            for (int i = 0; i < aircraftNeeded; i++)
            {
                AirplaneAttackFlyer jet = (AirplaneAttackFlyer)ThingMaker.MakeThing(jetDef);
                GenSpawn.Spawn(jet, target.Cell, map, WipeMode.Vanish);
                jet.SetupAttackRun(target, map, config, false, Vector3.zero);
            }

            Messages.Message("CMC_AirstrikeInbound_Homing".Translate(target.Label), new TargetInfo(target.Cell, map), MessageTypeDefOf.PositiveEvent);
        }
        private bool IsManualDirectionEnabled(GameComponent_CeleTech gameComp, AirstrikeConfig config)
        {
            if (config == null) return false;
            if (config.homing) return false;
            if (gameComp == null) return false;
            if (!gameComp.CASManualDirectionEnabled) return false;
            if (CMCResearchProjectDefOf.CMC_CASDirectionControl == null) return false;
            return CMCResearchProjectDefOf.CMC_CASDirectionControl.IsFinished;
        }
        private const int HangarDisplayLines = 3;
        private Dictionary<string, string> strikeLabelKeyById;

        private void BuildStrikeLabelMap()
        {
            strikeLabelKeyById = new Dictionary<string, string>();
            for (int i = 0; i < options.Count; i++)
            {
                if (!strikeLabelKeyById.ContainsKey(options[i].StrikeId))
                    strikeLabelKeyById.Add(options[i].StrikeId, options[i].LabelKey);
            }
        }

        private string GetStrikeLabelById(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId)) return "N/A";
            string key;
            if (strikeLabelKeyById != null && strikeLabelKeyById.TryGetValue(strikeId, out key))
                return key.Translate();
            return strikeId;
        }

        public override void DoNavWindowContents(Rect contentRect)
        {
            Widgets.DrawMenuSection(contentRect);
            Rect inner = contentRect.ContractedBy(10f);
            GameComponent_CeleTech gameComp = GameComponent_CeleTech.Instance;
            if (gameComp == null) return;

            if (strikeLabelKeyById == null) BuildStrikeLabelMap();

            Rect headerRect = new Rect(inner.x, inner.y, inner.width, 30f);
            Rect checkRect = new Rect(headerRect.x, headerRect.y, 250f, headerRect.height);
            Widgets.CheckboxLabeled(checkRect, "CMC_ReopenWindow".Translate(), ref reopenOnSelect);

            Rect dirRect = new Rect(checkRect.xMax + 10f, headerRect.y, 260f, headerRect.height);
            bool dirUnlocked = CMCResearchProjectDefOf.CMC_CASDirectionControl != null
                && CMCResearchProjectDefOf.CMC_CASDirectionControl.IsFinished;

            if (dirUnlocked)
            {
                bool manual = gameComp.CASManualDirectionEnabled;
                Widgets.CheckboxLabeled(dirRect, "CMC_ManualDirectionToggle".Translate(), ref manual);
                gameComp.CASManualDirectionEnabled = manual;
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(dirRect, "CMC_ManualDirectionLocked".Translate());
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Small;
            Widgets.Label(headerRect, "CMC_AvailablePoints".Translate(gameComp.CurrentPoint));
            Text.Anchor = TextAnchor.UpperLeft;

            Rect hangarRect = new Rect(inner.x, headerRect.yMax + 6f, inner.width, 110f);
            DrawHangarPanel(hangarRect, gameComp);

            Rect listRect = new Rect(inner.x, hangarRect.yMax + 8f, inner.width, inner.height - (hangarRect.yMax - inner.y) - 8f);
            DrawOptionListBasic(listRect, gameComp);
        }
        private void DrawHangarPanel(Rect rect, GameComponent_CeleTech gameComp)
        {
            Widgets.DrawMenuSection(rect);
            Rect titleRect = new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 22f);
            Text.Font = GameFont.Small;
            Widgets.Label(titleRect, "CMC_HangarTitle".Translate());

            float lineY = titleRect.yMax + 2f;
            for (int i = 0; i < HangarDisplayLines; i++)
            {
                bool unlocked;
                string strikeId;
                int chargesLeft;
                int standbyLeft;
                int rearmLeft;
                int attackCdLeft;
                gameComp.GetAircraftSlotSnapshot(i, out unlocked, out strikeId, out chargesLeft, out standbyLeft, out rearmLeft, out attackCdLeft);
                string lineText;
                int lineNo = i + 1;
                if (!unlocked)
                {
                    lineText = "CMC_HangarLineUnavailable".Translate(lineNo);
                }
                else if (rearmLeft > 0)
                {
                    lineText = "CMC_HangarLineRearming".Translate(lineNo, rearmLeft.ToStringTicksToPeriod());
                }
                else if (!string.IsNullOrEmpty(strikeId) && chargesLeft > 0)
                {
                    string strikeLabel = GetStrikeLabelById(strikeId);
                    int fuelHours = Mathf.CeilToInt((float)standbyLeft / GenDate.TicksPerHour);

                    if (attackCdLeft > 0)
                        lineText = "CMC_HangarLineExecuting".Translate(lineNo, strikeLabel, chargesLeft, attackCdLeft.ToStringTicksToPeriod(), fuelHours);
                    else
                        lineText = "CMC_HangarLineStandbyMounted".Translate(lineNo, strikeLabel, chargesLeft, fuelHours);
                }
                else
                {
                    lineText = "CMC_HangarLineStandby".Translate(lineNo);
                }
                Color oldColor = GUI.color;
                if (!unlocked)
                {
                    GUI.color = Color.gray;
                }
                else if (rearmLeft > 0)
                {
                    GUI.color = new Color(1f, 0.55f, 0.2f); 
                }
                else if (chargesLeft > 0)
                {
                    GUI.color = Color.cyan;
                }
                else
                {
                    GUI.color = Color.white;
                }
                Rect lineRect = new Rect(rect.x + 8f, lineY, rect.width - 16f, 24f);
                Widgets.Label(lineRect, lineText);
                GUI.color = oldColor;
                lineY += 24f;
            }
        }
        private void DrawOptionListBasic(Rect outRect, GameComponent_CeleTech gameComp)
        {
            float rowHeight = 92f;
            float gapHeight = 6f;
            float totalContentHeight = options.Count * (rowHeight + gapHeight);

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, totalContentHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            for (int i = 0; i < options.Count; i++)
            {
                AttackOption opt = options[i];
                Rect rowRect = listing.GetRect(rowHeight);

                if (i % 2 == 0) Widgets.DrawHighlight(rowRect);
                Widgets.DrawHighlightIfMouseover(rowRect);

                float iconSize = 64f;
                Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + (rowHeight - iconSize) / 2f, iconSize, iconSize);
                if (opt.IconDef != null) Widgets.ThingIcon(iconRect, opt.IconDef);
                else Widgets.DrawTextureFitted(iconRect, BaseContent.BadTex, 1f);

                float textLeft = iconRect.xMax + 12f;
                Rect textRect = new Rect(textLeft, rowRect.y + 6f, rowRect.width - textLeft - 150f, rowHeight - 12f);

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, 26f), opt.LabelKey.Translate());

                Text.Font = GameFont.Small;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(textRect.x, textRect.y + 24f, textRect.width, 20f), opt.DescKey.Translate());
                GUI.color = Color.white;

                int maxUses = gameComp.GetStrikeMaxUses(opt.StrikeId);
                Widgets.Label(new Rect(textRect.x, textRect.y + 46f, textRect.width, 20f),
                    "CMC_RowStrikeBasicInfo".Translate(maxUses, opt.AttackIntervalTicks.ToStringTicksToPeriod(), opt.RearmTicks.ToStringTicksToPeriod()));

                Rect btnRect = new Rect(rowRect.xMax - 140f, rowRect.y + (rowHeight - 38f) / 2f, 130f, 38f);
                string btnText = "CMC_CallStrikeButton".Translate(opt.Cost).ToString();
                int idleCount = gameComp.GetIdleAircraftCount();
                int mountedCharges = gameComp.GetMountedChargesTotal(opt.StrikeId);
                bool noIdleAndNoOwnCharge = (idleCount <= 0 && mountedCharges <= 0);
                // bool noIdleAndNoOwnCharge = gameComp.GetAvailableAircraftForStrike(opt.StrikeId) < opt.AircraftNeededPerCall;

                bool buttonActive = !noIdleAndNoOwnCharge;
                if (TradeWindowDrawer.DrawCustomButton(btnRect, btnText, active: buttonActive))
                {
                    if (!buttonActive)
                    {
                        Messages.Message("CMC_NoAircraftForThisLoadout".Translate(), MessageTypeDefOf.RejectInput);
                        continue;
                    }

                    string fail;
                    bool can = gameComp.CanLaunchStrike(
                        opt.StrikeId,
                        opt.Cost,
                        opt.AttackIntervalTicks,
                        opt.RearmTicks,
                        opt.AircraftNeededPerCall,
                        out fail
                    );

                    if (!can)
                    {
                        string msg = fail;
                        if (string.IsNullOrEmpty(msg))
                            msg = "CMC_ActionUnavailable".Translate().ToString();

                        Messages.Message(msg, MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        Close();
                        opt.OnSelect();
                    }
                }
                listing.Gap(gapHeight);
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
    public class Window_Subscription : Window_NavigationBase
    {
        public override void DoNavWindowContents(Rect contentRect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(contentRect, "CMC_SubscriptionPlaceholder".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
    public class Window_SpareButton1 : Window_NavigationBase
    {
        public override void DoNavWindowContents(Rect contentRect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(contentRect, "CMC_SparePage1Placeholder".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
    public class Window_SpareButton2 : Window_NavigationBase
    {
        public override void DoNavWindowContents(Rect contentRect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(contentRect, "CMC_SparePage2Placeholder".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
    public class Building_TradeConsole : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            yield return new Command_Action
            {
                defaultLabel = "CMC_OpenTradeInfo".Translate(),
                defaultDesc = "CMC_OpenTradeInfoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Trade"),
                action = () =>
                {
                    Find.WindowStack.Add(new Window_TradeInfo());
                }
            };
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Add 1000 Pts",
                    defaultDesc = "Debug: Add 1000 currency points immediately.",
                    icon = null,
                    action = () =>
                    {
                        if (GameComponent_CeleTech.Instance != null)
                        {
                            GameComponent_CeleTech.Instance.CurrentPoint += 1000;
                            Messages.Message("DEV: Added 1000 Points", MessageTypeDefOf.TaskCompletion);
                        }
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset CD",
                    defaultDesc = "Debug: Reset cooldowns for all trade shuttles.",
                    icon = null,
                    action = () =>
                    {
                        if (GameComponent_CeleTech.Instance != null)
                        {
                            GameComponent_CeleTech.Instance.LastAuxHr = 48;
                            GameComponent_CeleTech.Instance.LastArsHr = 72;
                            GameComponent_CeleTech.Instance.LastSSHr = 144;
                            Messages.Message("DEV: Shuttle Cooldowns Reset", MessageTypeDefOf.TaskCompletion);
                        }
                    }
                };
            }
        }
    }
}