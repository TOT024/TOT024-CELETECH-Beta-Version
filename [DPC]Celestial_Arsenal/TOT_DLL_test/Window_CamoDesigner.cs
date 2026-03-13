using RimWorld;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
     public class CamoPreset : IExposable
    {
        public string label;
        public Color c1;
        public Color c2;
        public Color c3;
        public string maskPath;
        public Vector2 scale = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public CamoPreset()
        {
        }
        public CamoPreset(string label, Color c1, Color c2, Color c3, string maskPath, Vector2 scale, Vector2 offset)
        {
            this.label = label;
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
            this.maskPath = maskPath;
            this.scale = scale;
            this.offset = offset;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref c1, "c1");
            Scribe_Values.Look(ref c2, "c2");
            Scribe_Values.Look(ref c3, "c3");
            Scribe_Values.Look(ref maskPath, "maskPath");
            Scribe_Values.Look(ref scale, "scale", Vector2.one);
            Scribe_Values.Look(ref offset, "offset", Vector2.zero);
        }
     }
    public class Window_CamoDesigner : Window
    {
        private Building_WeaponModificationBench _bench;
        private Comp_WeaponRenderStatic _comp;

        private Vector2 _scrollPositionLeft;
        private Vector2 _scrollPositionRight;
        private float _viewHeightLeft = 2000f;

        private List<Texture2D> _loadedPatterns = new List<Texture2D>();
        private string _debugSearchPath = "";

        private Texture2D _cachedCamoPreview;
        private int _lastHash;
        private Texture2D _centerBackgroundTex;
        private Texture2D _texSlotFull;
        private Texture2D _texSlotEmpty;
        private Texture2D _texSlotLocked;

        private Color _tempColorOne;
        private Color _tempColorTwo;
        private Color _tempColorThree;
        private string _tempMaskPath;

        private Vector2 _maskOffset = Vector2.zero;
        private Vector2 _maskScale = Vector2.one;
        private List<Thing> _selectedAccessories = new List<Thing>();

        private List<CamoPreset> _officialPresets = new List<CamoPreset>();
        private Dictionary<string, string> _fieldBuffers = new Dictionary<string, string>();

        public Window_CamoDesigner(Building_WeaponModificationBench bench)
        {
            this._bench = bench;
            var weaponHolder = bench.GetComp<CompWeaponHolder>();
            this._comp = weaponHolder.HeldWeapon?.TryGetComp<Comp_WeaponRenderStatic>();

            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.draggable = true;
            this.resizeable = false;

            if (_comp != null)
            {
                _tempColorOne = _comp.colorOne;
                _tempColorTwo = _comp.colorTwo;
                _tempColorThree = _comp.colorThree;
                _tempMaskPath = _comp.currentMaskPath;
                _maskOffset = _comp.maskOffset;
                _maskScale = _comp.maskScale;
                if (_maskScale == Vector2.zero) _maskScale = Vector2.one;
            }

            LoadAllPatterns();
            InitOfficialPresets();

            if (_centerBackgroundTex == null) _centerBackgroundTex = ContentFinder<Texture2D>.Get("UI/CMC/CenterBackgroundTex", false);
            if (_texSlotFull == null) _texSlotFull = ContentFinder<Texture2D>.Get("UI/CMC/Slot_Full", false);
            if (_texSlotEmpty == null) _texSlotEmpty = ContentFinder<Texture2D>.Get("UI/CMC/Slot_Empty", false);
            if (_texSlotLocked == null) _texSlotLocked = ContentFinder<Texture2D>.Get("UI/CMC/Slot_Locked", false);
        }

        public override Vector2 InitialSize => new Vector2(1000f, 760f);

        public override void PostClose()
        {
            base.PostClose();
            if (_cachedCamoPreview != null) Object.Destroy(_cachedCamoPreview);
        }

        private void InitOfficialPresets()
        {
            _officialPresets.Add(new CamoPreset("TOT_Camo_Preset_CeleTechElite".Translate(),
                new Color(0.93f, 0.93f, 0.93f, 1f),
                new Color(0.81f, 0.81f, 0.81f, 1f),
                new Color(0.71f, 0.71f, 0.71f, 1f),
                "Patterns/Hex", Vector2.one, Vector2.zero));

            _officialPresets.Add(new CamoPreset("TOT_Camo_Preset_VoidStandard".Translate(),
                new Color(0.08f, 0.09f, 0.12f, 1f),
                new Color(0.25f, 0.45f, 0.7f, 1f),
                new Color(0.15f, 0.18f, 0.22f, 1f),
                "Patterns/Digital", Vector2.one, Vector2.zero));

            _officialPresets.Add(new CamoPreset("TOT_Camo_Preset_XenoInfiltrator".Translate(),
                new Color(0.35f, 0.15f, 0.4f, 1f),
                new Color(0.6f, 0.8f, 0.3f, 1f),
                new Color(0.2f, 0.1f, 0.25f, 1f),
                "Patterns/Digital", Vector2.one, Vector2.zero));

            _officialPresets.Add(new CamoPreset("TOT_Camo_Preset_RedPlanet".Translate(),
                new Color(0.77f, 0.706f, 0.64f, 1f),
                new Color(0.62f, 0.545f, 0.462f, 1f),
                new Color(0.298f, 0.298f, 0.204f, 1f),
                "Patterns/Marine", Vector2.one, Vector2.zero));

            _officialPresets.Add(new CamoPreset("TOT_Camo_Preset_InternalSec".Translate(),
                new Color(0.12f, 0.12f, 0.14f, 1f),
                new Color(0.8f, 0.15f, 0.1f, 1f),
                new Color(0.25f, 0.1f, 0.1f, 1f),
                "Patterns/Hex", Vector2.one, Vector2.zero));
        }

        private void LoadAllPatterns()
        {
            _loadedPatterns.Clear();
            string myPackageId = "TOT.CeleTech.MKIII";
            var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(
                m => m.PackageId.Equals(myPackageId, System.StringComparison.InvariantCultureIgnoreCase));

            if (mod == null)
            {
                mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(
                    m => m.assemblies.loadedAssemblies.Contains(typeof(Window_CamoDesigner).Assembly));
            }

            if (mod == null) return;

            string patternsDir = Path.Combine(mod.RootDir, "Textures", "Patterns");
            _debugSearchPath = patternsDir;

            if (!Directory.Exists(patternsDir)) return;

            string[] files = Directory.GetFiles(patternsDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg")).ToArray();

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string loadPath = "Patterns/" + fileName;
                Texture2D tex = ContentFinder<Texture2D>.Get(loadPath, false);
                if (tex != null)
                {
                    tex.name = loadPath;
                    _loadedPatterns.Add(tex);
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_comp == null || _bench == null || _bench.Destroyed)
            {
                this.Close();
                return;
            }

            Text.Font = GameFont.Small;
            Rect contentRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 60f);

            Rect leftRect = new Rect(contentRect.x, contentRect.y, contentRect.width * 0.32f, contentRect.height);
            Rect centerRect = new Rect(leftRect.xMax + 10f, contentRect.y, contentRect.width * 0.36f - 20f, contentRect.height);
            Rect rightRect = new Rect(centerRect.xMax + 10f, contentRect.y, contentRect.width * 0.32f, contentRect.height);

            DrawLeftPanel(leftRect);
            DrawCenterPanel(centerRect);
            DrawRightPanel(rightRect);

            Rect bottomRect = new Rect(inRect.x, inRect.height - 50f, inRect.width, 50f);
            DrawBottomButtons(bottomRect);
        }

        private void DrawLeftPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Rect viewRect = new Rect(0, 0, innerRect.width - 16f, _viewHeightLeft);

            Widgets.BeginScrollView(innerRect, ref _scrollPositionLeft, viewRect);
            Listing_Standard list = new Listing_Standard();
            list.Begin(new Rect(0, 0, viewRect.width, float.MaxValue));

            list.Label("<b>" + "TOT_Camo_PresetsTitle".Translate() + "</b>");
            list.GapLine();

            list.Label("<color=grey>" + "TOT_Camo_Official".Translate() + "</color>");
            foreach (var preset in _officialPresets)
            {
                DrawPresetRow(list, preset, false);
            }

            list.Gap(10f);
            list.Label("<color=grey>" + "TOT_Camo_Custom".Translate() + "</color>");

            var storedPresets = GameComponent_CeleTech.Instance.CustomCamoPresets;

            CamoPreset presetToDelete = null;
            if (storedPresets.Count == 0)
            {
                list.Label("TOT_Camo_NoCustomPresets".Translate());
            }
            else
            {
                foreach (var preset in storedPresets)
                {
                    if (DrawPresetRow(list, preset, true))
                    {
                        presetToDelete = preset;
                    }
                }
            }
            if (presetToDelete != null)
            {
                storedPresets.Remove(presetToDelete);
            }

            if (list.ButtonText("TOT_Camo_SavePreset".Translate()))
            {
                storedPresets.Add(new CamoPreset("Custom " + (storedPresets.Count + 1),
                    _tempColorOne, _tempColorTwo, _tempColorThree, _tempMaskPath, _maskScale, _maskOffset));
            }

            list.Gap(24f);

            list.Label("<b>" + "TOT_Camo_ColorMixerTitle".Translate() + "</b>");
            list.GapLine();
            DrawColorSelectorVertical(list, "TOT_Camo_ColorPrimary".Translate(), ref _tempColorOne, "C1");
            list.Gap(6f);
            DrawColorSelectorVertical(list, "TOT_Camo_ColorSecondary".Translate(), ref _tempColorTwo, "C2");
            list.Gap(6f);
            DrawColorSelectorVertical(list, "TOT_Camo_ColorDetail".Translate(), ref _tempColorThree, "C3");

            list.Gap(24f);

            list.Label($"<b>{"TOT_Camo_PatternsTitle".Translate()} ({_loadedPatterns.Count})</b>");
            list.GapLine();

            string currentName = string.IsNullOrEmpty(_tempMaskPath) ? "TOT_Camo_None".Translate().ToString() : Path.GetFileName(_tempMaskPath);
            list.Label("TOT_Camo_CurrentPattern".Translate(currentName).Colorize(Color.grey));

            if (list.ButtonText("TOT_Camo_NoPattern".Translate()))
            {
                _tempMaskPath = "";
            }

            foreach (var tex in _loadedPatterns)
            {
                string label = Path.GetFileName(tex.name);
                bool isSelected = _tempMaskPath == tex.name;

                if (isSelected) GUI.color = Color.green;
                if (list.ButtonText(label))
                {
                    _tempMaskPath = tex.name;
                }
                GUI.color = Color.white;
            }
            list.Gap(24f);

            list.Label("<b>" + "TOT_Camo_PatternTransformTitle".Translate() + "</b>");
            list.GapLine();

            list.Label("TOT_Camo_Offset".Translate());
            DrawSliderWithBox(list, "X", ref _maskOffset.x, -0.5f, 0.5f, "OffX");
            DrawSliderWithBox(list, "Y", ref _maskOffset.y, -0.5f, 0.5f, "OffY");

            list.Gap(6f);

            list.Label("TOT_Camo_Scale".Translate());
            DrawSliderWithBox(list, "X", ref _maskScale.x, 0.1f, 5f, "SclX");
            DrawSliderWithBox(list, "Y", ref _maskScale.y, 0.1f, 5f, "SclY");

            list.Gap(4f);

            if (list.ButtonText("TOT_Camo_ResetTransform".Translate()))
            {
                _maskOffset = Vector2.zero;
                _maskScale = Vector2.one;
                _fieldBuffers.Remove("OffX");
                _fieldBuffers.Remove("OffY");
                _fieldBuffers.Remove("SclX");
                _fieldBuffers.Remove("SclY");
            }
            list.End();
            _viewHeightLeft = list.CurHeight + 50f;
            Widgets.EndScrollView();
        }

        private bool DrawPresetRow(Listing_Standard list, CamoPreset preset, bool allowDelete)
        {
            Rect rect = list.GetRect(64f);
            Widgets.DrawMenuSection(rect);

            Rect labelRect = new Rect(rect.x + 5f, rect.y + 2f, rect.width - 90f, 20f);
            Widgets.Label(labelRect, preset.label);

            float boxSize = 20f;
            float boxY = rect.y + 25f;
            Rect box1 = new Rect(rect.x + 5f, boxY, boxSize, boxSize);
            Rect box2 = new Rect(rect.x + 30f, boxY, boxSize, boxSize);
            Rect box3 = new Rect(rect.x + 55f, boxY, boxSize, boxSize);

            Widgets.DrawBoxSolid(box1, preset.c1); Widgets.DrawBox(box1);
            Widgets.DrawBoxSolid(box2, preset.c2); Widgets.DrawBox(box2);
            Widgets.DrawBoxSolid(box3, preset.c3); Widgets.DrawBox(box3);

            Rect btnLoad = new Rect(rect.xMax - 60f, rect.y + 12f, 55f, 40f);
            if (allowDelete)
            {
                btnLoad = new Rect(rect.xMax - 85f, rect.y + 12f, 55f, 40f);
                Rect btnDelete = new Rect(rect.xMax - 25f, rect.y + 12f, 20f, 40f);

                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (Widgets.ButtonText(btnDelete, "X"))
                {
                    GUI.color = Color.white;
                    return true;
                }
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(btnLoad, "TOT_Camo_Load".Translate()))
            {
                ApplyPreset(preset);
            }

            list.Gap(4f);
            return false;
        }

        private void ApplyPreset(CamoPreset preset)
        {
            _tempColorOne = preset.c1;
            _tempColorTwo = preset.c2;
            _tempColorThree = preset.c3;
            _tempMaskPath = preset.maskPath;
            _maskScale = preset.scale;
            _maskOffset = preset.offset;

            _lastHash = 0;
            _fieldBuffers.Clear();
        }

        private void DrawSliderWithBox(Listing_Standard list, string label, ref float val, float min, float max, string uniqueKey)
        {
            Rect rect = list.GetRect(22f);
            float labelWidth = 20f;
            if (label.Length > 3) labelWidth = 100f;

            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, 22f), label);

            float inputWidth = 50f;
            float padding = 5f;
            float sliderWidth = rect.width - labelWidth - inputWidth - padding;

            Rect sliderRect = new Rect(rect.x + labelWidth, rect.y + 2f, sliderWidth, 18f);
            Rect inputRect = new Rect(rect.xMax - inputWidth, rect.y, inputWidth, 22f);

            float oldVal = val;
            val = Widgets.HorizontalSlider(sliderRect, val, min, max);

            if (!_fieldBuffers.TryGetValue(uniqueKey, out string buffer))
            {
                buffer = val.ToString("F2");
            }

            if (Mathf.Abs(val - oldVal) > 1E-4f)
            {
                buffer = val.ToString("F2");
            }

            Widgets.TextFieldNumeric(inputRect, ref val, ref buffer, min, max);
            _fieldBuffers[uniqueKey] = buffer;
        }

        private void DrawColorSelectorVertical(Listing_Standard list, string label, ref Color color, string keyPrefix)
        {
            Rect headerRect = list.GetRect(24f);
            Widgets.Label(new Rect(headerRect.x, headerRect.y, headerRect.width - 50f, 24f), label);

            Rect colorBox = new Rect(headerRect.xMax - 40f, headerRect.y, 40f, 24f);
            Widgets.DrawBoxSolid(colorBox, color);
            Widgets.DrawBox(colorBox);

            Color original = color;
            float r = color.r;
            float g = color.g;
            float b = color.b;
            float a = color.a;

            DrawSliderWithBox(list, "<color=red>R</color>", ref r, 0f, 1f, keyPrefix + "_R");
            DrawSliderWithBox(list, "<color=green>G</color>", ref g, 0f, 1f, keyPrefix + "_G");
            DrawSliderWithBox(list, "<color=blue>B</color>", ref b, 0f, 1f, keyPrefix + "_B");
            DrawSliderWithBox(list, "<color=grey>A</color>", ref a, 0f, 1f, keyPrefix + "_A");

            if (r != original.r || g != original.g || b != original.b || a != original.a)
            {
                color = new Color(r, g, b, a);
            }
        }

        private void DrawCenterPanel(Rect rect)
        {
            if (_centerBackgroundTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, _centerBackgroundTex, ScaleMode.StretchToFill);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            }

            Widgets.DrawBox(rect);

            Rect titleRect = new Rect(rect.x, rect.y + 10f, rect.width, 30f);
            Text.Anchor = TextAnchor.UpperCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, _comp.parent.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            string baseTexPath = _comp.parent.def.graphicData.texPath;
            Texture2D baseTex = ContentFinder<Texture2D>.Get(baseTexPath, false);
            if (baseTex == null) return;

            string camoTexPath = _comp.Props.TexturePath_Camo;
            Texture2D camoSourceTex = ContentFinder<Texture2D>.Get(camoTexPath, false);

            float aspectRatio = (float)baseTex.width / baseTex.height;
            float drawWidth, drawHeight;

            if (aspectRatio > rect.width / rect.height)
            {
                drawWidth = rect.width - 40f;
                drawHeight = drawWidth / aspectRatio;
            }
            else
            {
                drawHeight = rect.height - 80f;
                drawWidth = drawHeight * aspectRatio;
            }

            Rect previewRect = new Rect(
                rect.center.x - drawWidth / 2,
                rect.center.y - drawHeight / 2 + 10f,
                drawWidth,
                drawHeight
            );

            GUI.color = Color.white;
            GUI.DrawTexture(previewRect, baseTex);

            if (camoSourceTex != null)
            {
                Material mat = _comp.GetMaterial_Camo;
                if (mat != null)
                {
                    int currentHash = _tempColorOne.GetHashCode() ^
                                      _tempColorTwo.GetHashCode() ^
                                      _tempColorThree.GetHashCode() ^
                                      (_tempMaskPath ?? "").GetHashCode() ^
                                      _maskOffset.GetHashCode() ^
                                      _maskScale.GetHashCode();

                    if (_cachedCamoPreview == null || currentHash != _lastHash)
                    {
                        if (_cachedCamoPreview != null) Object.Destroy(_cachedCamoPreview);

                        mat.mainTexture = camoSourceTex;
                        mat.mainTextureScale = Vector2.one;
                        mat.SetColor("_ColorOne", _tempColorOne);
                        mat.SetColor("_ColorTwo", _tempColorTwo);
                        mat.SetColor("_ColorThree", _tempColorThree);

                        mat.SetTextureScale("_MaskTex", _maskScale);
                        mat.SetTextureOffset("_MaskTex", _maskOffset);

                        if (!string.IsNullOrEmpty(_tempMaskPath))
                        {
                            Texture2D maskTex = ContentFinder<Texture2D>.Get(_tempMaskPath, false);
                            if (maskTex != null)
                            {
                                mat.SetTexture("_MaskTex", maskTex);
                            }
                            else
                            {
                                mat.SetTexture("_MaskTex", Texture2D.whiteTexture);
                            }
                        }
                        else
                        {
                            mat.SetTexture("_MaskTex", Texture2D.whiteTexture);
                        }

                        _cachedCamoPreview = TextureBaker.Bake(camoSourceTex, mat);
                        _lastHash = currentHash;
                    }

                    if (_cachedCamoPreview != null)
                    {
                        GUI.DrawTexture(previewRect, _cachedCamoPreview);
                    }
                }
            }
        }

        private void DrawRightPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);

            var weapon = _comp.parent;
            var accessoryComp = weapon.TryGetComp<CompAccessoryHolder>();

            Rect statusRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
            if (accessoryComp != null)
            {
                DrawSlotStatus(statusRect, accessoryComp);
            }

            Rect listRect = new Rect(innerRect.x, innerRect.y + 65f, innerRect.width, innerRect.height - 65f);
            Widgets.BeginScrollView(listRect, ref _scrollPositionRight, new Rect(0, 0, listRect.width - 16f, 1000f));
            Listing_Standard list = new Listing_Standard();
            list.Begin(new Rect(0, 0, listRect.width - 16f, float.MaxValue));

            if (accessoryComp != null)
            {
                list.Label("<b>" + "TOT_Camo_InstalledAccessories".Translate() + "</b>");
                list.GapLine();

                if (accessoryComp.InstalledAccessories != null && accessoryComp.InstalledAccessories.Count > 0)
                {
                    Thing accessoryToRemove = null;
                    foreach (var installed in accessoryComp.InstalledAccessories)
                    {
                        if (DrawInstalledAccessoryRow(list, installed, accessoryComp))
                        {
                            accessoryToRemove = installed;
                        }
                    }
                    if (accessoryToRemove != null)
                    {
                        _bench.StartUninstallation(accessoryToRemove);
                    }
                }
                else
                {
                    list.Label("<color=grey>" + "TOT_Camo_NoInstalledAccessories".Translate() + "</color>");
                }
                list.Gap(24f);
            }

            list.Label("<b>" + "TOT_Camo_ConnectedAccessories".Translate() + "</b>");
            list.GapLine();

            if (accessoryComp == null)
            {
                GUI.color = Color.red;
                list.Label("TOT_Camo_IncompatibleWeapon".Translate());
                GUI.color = Color.white;
            }
            else if (accessoryComp.IsFull)
            {
                GUI.color = Color.yellow;
                list.Label("TOT_Camo_SlotsFull".Translate());
                GUI.color = Color.white;
            }
            else
            {
                var allowedDefs = accessoryComp.Props.allowedAccessoryDefs;
                var connectedAccessories = GetAccessoriesFromCabinets();

                if (connectedAccessories.Count == 0)
                {
                    list.Label("<color=grey>" + "TOT_Camo_NoAccessoriesFound".Translate() + "</color>");
                }
                else
                {
                    foreach (var acc in connectedAccessories)
                    {
                        bool isCompatible = true;
                        if (allowedDefs != null && allowedDefs.Any())
                        {
                            isCompatible = allowedDefs.Contains(acc.def.defName);
                        }
                        DrawAccessoryRow(list, acc, isCompatible);
                    }
                }
            }

            list.End();
            Widgets.EndScrollView();
        }

        private List<Thing> GetAccessoriesFromCabinets()
        {
            var results = new List<Thing>();
            if (_bench == null || !_bench.Spawned) return results;
            var buildings = _bench.Map.listerBuildings.AllBuildingsColonistOfClass<Building_AccessoryCabinet>();
            foreach (var cabinet in buildings)
            {
                if (cabinet.Position.DistanceTo(_bench.Position) <= 12.9f)
                {
                    var storedItems = cabinet.StoredAccessories;
                    if (storedItems != null)
                    {
                        foreach (var t in storedItems)
                        {
                            if (t.TryGetComp<CompAccessoryStats>() != null)
                            {
                                results.Add(t);
                            }
                        }
                    }
                }
            }
            return results;
        }

        private void DrawSlotStatus(Rect rect, CompAccessoryHolder comp)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width, 20f), "TOT_Camo_Capacity".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            int maxSlots = comp.Props.maxAccessories;
            int occupiedCount = 0;
            if (comp.InstalledAccessories != null) occupiedCount = comp.InstalledAccessories.Count;

            float boxSize = 32f;
            float spacing = 10f;
            float totalWidth = (boxSize * 4) + (spacing * 3);
            float startX = rect.center.x - (totalWidth / 2);
            float startY = rect.y + 24f;

            for (int i = 0; i < 4; i++)
            {
                Rect iconRect = new Rect(startX + (i * (boxSize + spacing)), startY, boxSize, boxSize);
                Texture2D iconToDraw = _texSlotLocked;

                if (i < occupiedCount)
                {
                    iconToDraw = _texSlotFull;
                }
                else if (i < maxSlots)
                {
                    iconToDraw = _texSlotEmpty;
                }

                if (iconToDraw != null)
                {
                    GUI.DrawTexture(iconRect, iconToDraw);
                }
            }
        }

        // --- 修改 3: 底部按钮逻辑 ---
        private void DrawBottomButtons(Rect rect)
        {
            float buttonWidth = 150f;
            float spacing = 20f;
            float startX = rect.center.x - (buttonWidth + spacing / 2);

            Rect btnCancel = new Rect(startX, rect.y + 10f, buttonWidth, 35f);
            if (Widgets.ButtonText(btnCancel, "TOT_Camo_Cancel".Translate(), true, true, true))
            {
                this.Close();
            }

            string confirmLabel = "TOT_Camo_Apply".Translate();
            if (_selectedAccessories.Count > 0)
            {
                confirmLabel = "TOT_Camo_ApplyInsert".Translate();
            }

            Rect btnConfirm = new Rect(startX + buttonWidth + spacing, rect.y + 10f, buttonWidth, 35f);
            if (Widgets.ButtonText(btnConfirm, confirmLabel, true, true, true))
            {
                _bench.StartModification(
                    _selectedAccessories,
                    true,
                    _tempColorOne,
                    _tempColorTwo,
                    _tempColorThree,
                    _tempMaskPath,
                    _maskScale,
                    _maskOffset
                );
                this.Close();
            }
        }
        private bool DrawInstalledAccessoryRow(Listing_Standard list, Thing installed, CompAccessoryHolder comp)
        {
            float rowHeight = 36f;
            Rect rect = list.GetRect(rowHeight);
            Widgets.DrawMenuSection(rect);

            Rect iconRect = new Rect(rect.x + 4f, rect.y + 2f, 32f, 32f);
            if (installed.def.uiIcon != null)
            {
                GUI.color = installed.DrawColor;
                GUI.DrawTexture(iconRect, installed.def.uiIcon);
                GUI.color = Color.white;
            }

            Rect labelRect = new Rect(rect.x + 45f, rect.y, rect.width - 130f, rowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, installed.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect btnRect = new Rect(rect.xMax - 85f, rect.y + 4f, 80f, 28f);
            if (Widgets.ButtonText(btnRect, "TOT_Camo_Uninstall".Translate()))
            {
                return true;
            }

            list.Gap(4f);
            return false;
        }
        private void DrawAccessoryRow(Listing_Standard list, Thing acc, bool isCompatible)
        {
            float rowHeight = 44f;
            Rect rect = list.GetRect(rowHeight);
            bool isSelected = _selectedAccessories.Contains(acc);

            if (isSelected)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.6f, 0.5f, 0.1f, 0.4f));
                Widgets.DrawBox(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect iconRect = new Rect(rect.x + 4f, rect.y + 2f, 40f, 40f);
            if (acc.def.uiIcon != null)
            {
                GUI.color = isCompatible ? acc.DrawColor : Color.gray;
                GUI.DrawTexture(iconRect, acc.def.uiIcon);
                GUI.color = Color.white;
            }

            string label = acc.LabelCap + (isCompatible ? "" : "TOT_Camo_IncompatibleTag".Translate().ToString());
            Rect labelRect = new Rect(rect.x + 50f, rect.y, rect.width - 50f, rowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;

            if (!isCompatible)
            {
                GUI.color = Color.gray;
            }
            else if (isSelected)
            {
                GUI.color = Color.yellow;
            }
            else
            {
                GUI.color = Color.white;
            }

            Widgets.Label(labelRect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(rect))
            {
                if (isCompatible)
                {
                    if (isSelected)
                    {
                        _selectedAccessories.Remove(acc);
                    }
                    else
                    {
                        var holder = _bench.GetComp<CompWeaponHolder>().HeldWeapon.TryGetComp<CompAccessoryHolder>();
                        if (holder != null && (holder.InstalledAccessories.Count + _selectedAccessories.Count) < holder.Props.maxAccessories)
                        {
                            _selectedAccessories.Add(acc);
                        }
                        else
                        { Messages.Message("Slots Full", MessageTypeDefOf.RejectInput); }
                    }
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                else
                {
                    Messages.Message("CMC_NotCompatible".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            list.Gap(4f);
        }
    }
}