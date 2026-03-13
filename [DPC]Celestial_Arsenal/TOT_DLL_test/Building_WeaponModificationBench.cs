using Aardvark.Base;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_WeaponModificationBench : Building
    {
        private CompWeaponHolder holderComp;
        private CompPowerTrader powerComp;
        private bool currentlyWorking = false;
        private int workTicksRemaining = 0;
        private int totalWorkTicks = 0;
        private const int Ticks_Per_Installation = 2500;
        private const int Ticks_Painting = 600;
        private List<ThingDef> accessoriesToInstall = new List<ThingDef>();
        private bool _pendingPaintJob = false;
        private Color _pendingC1, _pendingC2, _pendingC3;
        private string _pendingMaskPath;
        private Vector2 _pendingMaskScale = Vector2.one;
        private Vector2 _pendingMaskOffset = Vector2.zero;
        private static Texture2D texPaint = ContentFinder<Texture2D>.Get("UI/UI_CMCPaint", true);
        private static Texture2D texUninstall = ContentFinder<Texture2D>.Get("UI/UI_Cancel", true);
        private static Texture2D texImport = ContentFinder<Texture2D>.Get("UI/UI_Claim", true);
        private static readonly Material DecoTexture = MaterialPool.MatFrom("Things/Buildings/WeaponModBench_Deco", ShaderDatabase.Cutout);
        private static readonly Material LightTexture = MaterialPool.MatFrom("Things/Buildings/WeaponModBench_Deco_Light", ShaderDatabase.MoteGlow);
        private static readonly Vector3 vec = new Vector3(3f, 0f, 3f);
        private float DecoDrawHeight = 0f;
        private bool isIncreasing = true;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.holderComp = this.GetComp<CompWeaponHolder>();
            this.powerComp = this.GetComp<CompPowerTrader>();
        }
        public override void DrawExtraSelectionOverlays()
        {
            Material material = Building_CMCTurretGun.RangeMat;
            GenDraw.DrawCircleOutline(DrawPos, 8.9f, material);
            GenDraw.DrawCircleOutline(DrawPos, 8.8f, material);
            GenDraw.DrawCircleOutline(DrawPos, 8.7f, material);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentlyWorking, "currentlyWorking", false);
            Scribe_Values.Look(ref workTicksRemaining, "workTicksRemaining", 0);
            Scribe_Values.Look(ref totalWorkTicks, "totalWorkTicks", 0);
            Scribe_Values.Look(ref isIncreasing, "isIncreasing", true);
            Scribe_Values.Look(ref DecoDrawHeight, "DecoDrawHeight", 0f);
            Scribe_Collections.Look(ref accessoriesToInstall, "accessoriesToInstall", LookMode.Def);
            if (accessoriesToInstall == null) accessoriesToInstall = new List<ThingDef>();
            Scribe_Values.Look(ref _pendingPaintJob, "pendingPaintJob", false);
            Scribe_Values.Look(ref _pendingC1, "pendingC1");
            Scribe_Values.Look(ref _pendingC2, "pendingC2");
            Scribe_Values.Look(ref _pendingC3, "pendingC3");
            Scribe_Values.Look(ref _pendingMaskPath, "pendingMaskPath");
            Scribe_Values.Look(ref _pendingMaskScale, "pendingMaskScale", Vector2.one);
            Scribe_Values.Look(ref _pendingMaskOffset, "pendingMaskOffset", Vector2.zero);
        }
        public void StartModification(List<Thing> accessories, bool doPainting, Color c1, Color c2, Color c3, string maskPath, Vector2 maskScale, Vector2 maskOffset)
        {
            ResetWork();

            string messageText = "";
            string weaponLabel = holderComp.HeldWeapon?.LabelCap ?? "WeaponModificationBench_DefaultWeaponLabel".Translate();
            int calculatedTicks = 0;
            if (accessories != null && accessories.Count > 0)
            {
                foreach (var acc in accessories)
                {
                    this.accessoriesToInstall.Add(acc.def);
                    acc.SplitOff(1).Destroy(DestroyMode.Vanish);
                }
                calculatedTicks += (accessories.Count * Ticks_Per_Installation);
                messageText += "WeaponModificationBench_StartedInstall".Translate(accessories.Count, weaponLabel);
            }
            if (doPainting)
            {
                this._pendingPaintJob = true;
                this._pendingC1 = c1;
                this._pendingC2 = c2;
                this._pendingC3 = c3;
                this._pendingMaskPath = maskPath;
                this._pendingMaskScale = maskScale;
                this._pendingMaskOffset = maskOffset;

                calculatedTicks += Ticks_Painting;

                if (!string.IsNullOrEmpty(messageText)) messageText += "\n";
                messageText += "WeaponModificationBench_PaintingStarted".Translate(weaponLabel);
            }
            if (calculatedTicks > 0)
            {
                this.workTicksRemaining = calculatedTicks;
                this.totalWorkTicks = calculatedTicks;
                this.currentlyWorking = true;
                Messages.Message(messageText, this, MessageTypeDefOf.TaskCompletion);
            }
        }
        public void StartInstallation(Thing accessoryThing)
        {
            if (accessoryThing == null) return;
            var list = new List<Thing> { accessoryThing };
            StartModification(list, false, Color.white, Color.white, Color.white, null, Vector2.one, Vector2.zero);
        }
        public void StartPainting(Color c1, Color c2, Color c3, string maskPath, Vector2 maskScale, Vector2 maskOffset)
        {
            StartModification(null, true, c1, c2, c3, maskPath, maskScale, maskOffset);
        }

        public void StartUninstallation(Thing accessory)
        {
            var weaponHolder = this.GetComp<CompWeaponHolder>();
            var weapon = weaponHolder.HeldWeapon;
            if (weapon == null) return;
            var accComp = weapon.TryGetComp<CompAccessoryHolder>();
            if (accComp == null) return;

            accComp.UninstallAccessory(accessory);
            SoundDefOf.Click.PlayOneShot(SoundInfo.InMap(this));

            if (accessory.Spawned == false)
            {
                GenPlace.TryPlaceThing(accessory, this.InteractionCell, this.Map, ThingPlaceMode.Near);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (currentlyWorking && holderComp.IsOccupied && (powerComp == null || powerComp.PowerOn))
            {
                workTicksRemaining--;
                if (isIncreasing)
                {
                    this.DecoDrawHeight += 0.005f;
                    if (this.DecoDrawHeight >= 0.33f) { this.DecoDrawHeight = 0.33f; isIncreasing = false; }
                }
                else
                {
                    this.DecoDrawHeight -= 0.005f;
                    if (this.DecoDrawHeight <= -0.25f) { this.DecoDrawHeight = -0.2f; isIncreasing = true; }
                }

                if (workTicksRemaining <= 0)
                {
                    FinishWork();
                }
            }
            else if (currentlyWorking && (!holderComp.IsOccupied || (powerComp != null && !powerComp.PowerOn)))
            {
                ResetWork();
                Messages.Message("WeaponModificationBench_Interrupted".Translate(), this, MessageTypeDefOf.NegativeEvent);
            }
        }

        private void FinishWork()
        {
            var weapon = holderComp.HeldWeapon;
            if (weapon == null)
            {
                ResetWork();
                return;
            }

            string successMsg = "";
            if (_pendingPaintJob)
            {
                var camoComp = weapon.TryGetComp<Comp_WeaponRenderStatic>();
                if (camoComp != null)
                {
                    camoComp.colorOne = _pendingC1;
                    camoComp.colorTwo = _pendingC2;
                    camoComp.colorThree = _pendingC3;
                    camoComp.currentMaskPath = _pendingMaskPath;
                    camoComp.maskScale = _pendingMaskScale;
                    camoComp.maskOffset = _pendingMaskOffset;
                    camoComp.UpdateSkin();

                    successMsg += "WeaponModificationBench_PaintSuccess".Translate(weapon.LabelCap);
                }
            }
            if (accessoriesToInstall != null && accessoriesToInstall.Count > 0)
            {
                var accessoryComp = weapon.TryGetComp<CompAccessoryHolder>();
                if (accessoryComp != null)
                {
                    int installedCount = 0;
                    foreach (var accDef in accessoriesToInstall)
                    {
                        if (!accessoryComp.IsFull)
                        {
                            Thing newAccessory = ThingMaker.MakeThing(accDef);
                            if (accessoryComp.TryInstallAccessory(newAccessory))
                            {
                                installedCount++;
                            }
                        }
                    }

                    if (installedCount > 0)
                    {
                        if (!string.IsNullOrEmpty(successMsg)) successMsg += "\n";
                        successMsg += "WeaponModificationBench_BatchInstallSuccess".Translate(installedCount, weapon.LabelCap);
                    }
                }
            }

            if (!string.IsNullOrEmpty(successMsg))
            {
                Messages.Message(successMsg, weapon, MessageTypeDefOf.PositiveEvent);
            }

            ResetWork();
        }

        private void ResetWork()
        {
            currentlyWorking = false;
            workTicksRemaining = 0;
            totalWorkTicks = 0;

            accessoriesToInstall.Clear();

            _pendingPaintJob = false;
            _pendingMaskScale = Vector2.one;
            _pendingMaskOffset = Vector2.zero;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip = false);
            Matrix4x4 matrix = default;
            Vector3 pos = this.DrawPos + Altitudes.AltIncVect + this.def.graphicData.drawOffset;
            pos.z += DecoDrawHeight;
            pos.y = AltitudeLayer.Item.AltitudeFor() + 0.3f;
            matrix.SetTRS(pos, UnityEngine.Quaternion.identity, vec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, DecoTexture, 0);

            if (holderComp != null && holderComp.IsOccupied)
            {
                Thing eq = holderComp.HeldWeapon as Thing;
                Vector3 size = new Vector3(eq.Graphic.drawSize.x, 1f, eq.Graphic.drawSize.y);
                UnityEngine.Quaternion rotation = UnityEngine.Quaternion.AngleAxis(0f, Vector3.up);
                Matrix4x4 matrixWeapon = Matrix4x4.TRS(drawLoc + new Vector3(0f, 0.25f, 0.33f), rotation, size);
                Mesh mesh = MeshPool.plane10;
                Material material;

                if (eq.Graphic is Graphic_StackCount graphic_StackCount)
                {
                    material = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingleFor(eq);
                }
                else
                {
                    material = eq.Graphic.MatSingleFor(eq);
                }
                Graphics.DrawMesh(mesh, matrixWeapon, material, 0);
                Vector3 overlayPos = drawLoc;
                overlayPos.y += 0.253f;
                overlayPos.z += 0.33f;
                Matrix4x4 matrixOverlay = Matrix4x4.TRS(overlayPos, rotation, size);
                Comp_WeaponRenderDynamic compDynamic = eq.TryGetComp<Comp_WeaponRenderDynamic>();
                Comp_WeaponRenderStatic compStatic = eq.TryGetComp<Comp_WeaponRenderStatic>();
                compStatic?.PostDrawExtraGlower(mesh, matrixOverlay);
                if (currentlyWorking && totalWorkTicks > 0)
                {
                    pos.y -= 0.15f;
                    matrix.SetTRS(pos, UnityEngine.Quaternion.identity, vec);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, LightTexture, 0);
                    Vector3 barDrawPos = this.DrawPos + new Vector3(-0.45f, 3f, -0.78f);
                    barDrawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();

                    float fillPercent = 1f - ((float)workTicksRemaining / (float)totalWorkTicks);
                    Color barColor = new Color(0.2f, 0.2f, 0.67f, 1f);
                    if (_pendingPaintJob && accessoriesToInstall.Count > 0) barColor = new Color(0.2f, 0.67f, 0.67f, 1f);
                    else if (_pendingPaintJob) barColor = new Color(0.87f, 0.87f, 0.87f, 1f);

                    GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
                    {
                        center = barDrawPos,
                        size = new Vector2(0.6f, 0.06f),
                        fillPercent = fillPercent,
                        filledMat = SolidColorMaterials.SimpleSolidColorMaterial(barColor),
                        unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.13f, 0.13f, 0.13f)),
                        margin = 0.05f
                    });
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var baseOption in base.GetFloatMenuOptions(selPawn)) yield return baseOption;

            if (currentlyWorking)
            {
                yield return new FloatMenuOption("WeaponModificationBench_Working".Translate(), null);
                yield break;
            }
            if (holderComp.IsOccupied)
            {
                yield return new FloatMenuOption("WeaponModificationBench_Occupied".Translate(), null);
                yield break;
            }
            if (!selPawn.CanReserveAndReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("WeaponModificationBench_Unreachable".Translate(), null);
                yield break;
            }
            var weaponsToOffer = new List<Thing>();
            if (selPawn.equipment?.Primary != null &&
               (selPawn.equipment.Primary.TryGetComp<CompAccessoryHolder>() != null ||
                selPawn.equipment.Primary.TryGetComp<Comp_WeaponRenderStatic>() != null))
            {
                weaponsToOffer.Add(selPawn.equipment.Primary);
            }
            if (selPawn.inventory?.innerContainer != null)
            {
                weaponsToOffer.AddRange(selPawn.inventory.innerContainer.Where(t =>
                    t.TryGetComp<CompAccessoryHolder>() != null ||
                    t.TryGetComp<Comp_WeaponRenderStatic>() != null));
            }

            if (weaponsToOffer.Count == 0)
            {
                yield return new FloatMenuOption("WeaponModificationBench_NoWeapons".Translate(), null);
            }
            else
            {
                foreach (Thing weapon in weaponsToOffer)
                {
                    string label = "WeaponModificationBench_Place".Translate(weapon.LabelCap);
                    yield return new FloatMenuOption(label, () =>
                    {
                        var job = JobMaker.MakeJob(CMC_Def.PlaceCarriedWeaponOnBench, weapon, this);
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.High);
                }
            }
        }
        public void ForceMountWeapon(Thing weapon)
        {
            if (weapon == null || !weapon.Spawned) return;
            if (holderComp != null && !holderComp.IsOccupied)
            {
                holderComp.InstallWeapon(weapon);
                SoundDefOf.Click.PlayOneShot(SoundInfo.InMap(this));
                ResetWork();
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            if (holderComp != null && holderComp.IsOccupied)
            {
                Command_Action openCamoDesigner = new Command_Action
                {
                    defaultLabel = "CMC_OpenCamoDesigner".Translate(),
                    defaultDesc = "CMC_OpenCamoDesignerDesc".Translate(),
                    icon = texPaint,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Window_CamoDesigner(this));
                    }
                };

                if (currentlyWorking) openCamoDesigner.Disable("WeaponModificationBench_Working".Translate());
                else if (powerComp != null && !powerComp.PowerOn) openCamoDesigner.Disable("NoPower".Translate().CapitalizeFirst());

                yield return openCamoDesigner;
                var uninstallGizmo = new Command_Action
                {
                    defaultLabel = "WeaponModificationBench_UninstallWeapon".Translate(),
                    defaultDesc = "WeaponModificationBench_UninstallWeaponDesc".Translate(),
                    icon = texUninstall,
                    action = () =>
                    {
                        Thing uninstalledWeapon = holderComp.UninstallWeapon();
                        if (uninstalledWeapon != null)
                        {
                            GenPlace.TryPlaceThing(uninstalledWeapon, this.Position, this.Map, ThingPlaceMode.Near);
                            ResetWork();
                        }
                    }
                };

                if (currentlyWorking) uninstallGizmo.Disable("WeaponModificationBench_Working".Translate());
                yield return uninstallGizmo;
            }
            if (holderComp != null && !holderComp.IsOccupied && !currentlyWorking)
            {
                Command_Action searchWeaponGizmo = new Command_Action
                {
                    defaultLabel = "CMC_ImportNearbyWeapon_Label".Translate(),
                    defaultDesc = "CMC_ImportNearbyWeapon_Desc".Translate(),
                    icon = texImport,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Window_NearbyWeaponSelector(this));
                    }
                };
                yield return searchWeaponGizmo;
            }
        }
    }
}