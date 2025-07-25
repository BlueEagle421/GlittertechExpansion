using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using System.Text;
using System.Linq;

namespace USH_GE
{
    [StaticConstructorOnStartup]
    public class Building_GlittertechFabricator : Building_WorkTableAutonomous
    {

        public Bill_Glittertech GlitterBill => ActiveBill as Bill_Glittertech;

        private static readonly Material FormingCycleBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.98f, 0.46f, 0f), false);
        private static readonly Material FormingCycleUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f), false);
        private EffecterHandler _electricEffecterHandler;
        private const float FADE_DURATION_TICKS = 300f;
        private float _fadeTicks = FADE_DURATION_TICKS;
        private bool _lastPoweredOn = true;
        public bool PoweredOn => PowerTrader.PowerOn;

        private CompPowerTrader _powerTrader;
        public CompPowerTrader PowerTrader
        {
            get
            {
                _powerTrader ??= this.TryGetComp<CompPowerTrader>();

                return _powerTrader;
            }
        }

        private CompAffectedByFacilities _compFacilities;
        public CompAffectedByFacilities CompFacilities
        {
            get
            {
                _compFacilities ??= this.TryGetComp<CompAffectedByFacilities>();
                return _compFacilities;
            }
        }

        private bool _recacheMaterial = true;
        private Material _cachedMaterial;
        public Material FormingMaterial
        {
            get
            {
                if (_cachedMaterial == null || _recacheMaterial)
                {
                    if (GlitterBill == null)
                        return null;

                    Graphic graphic = GlitterBill.GetProductGraphic;
                    graphic = graphic.GetCopy(graphic.drawSize * GlitterBill.GlittertechExt.fabricatorScale, null);
                    _cachedMaterial = MaterialPool.MatFrom(graphic.path, ShaderDatabase.Transparent);
                }

                return _cachedMaterial;
            }
        }

        public CompOverclock OverclockingGun => innerContainer.Select(t => t.TryGetComp<CompOverclock>()).FirstOrDefault();

        public override void PostMake()
        {
            base.PostMake();
            _electricEffecterHandler = new EffecterHandler(this, USH_DefOf.USH_ElectricForming);
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            _electricEffecterHandler = new EffecterHandler(this, USH_DefOf.USH_ElectricForming);

            CommonSenseCheck.CheckForAssemblyPresence();
        }

        public override void Notify_StartForming(Pawn billDoer)
        {
            if (!HasStoredPower(GlitterBill.GlittertechExt.powerNeeded))
                return;

            base.Notify_StartForming(billDoer);

            DrawPowerFromNet(GlitterBill.GlittertechExt.powerNeeded);

            if (Spawned && Map != null)
                _electricEffecterHandler.StartMaintaining(360, GlitterBill.GlittertechExt.fabricatorOffsetY);

            SoundDefOf.MechGestatorCycle_Started.PlayOneShot(this);

            _recacheMaterial = true;
        }

        public override void Notify_FormingCompleted()
        {
            Thing thing = activeBill.CreateProducts();
            if (thing != null)
                innerContainer.Remove(thing);

            innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            innerContainer.TryAdd(thing);

            SoundDefOf.MechGestatorBill_Completed.PlayOneShot(this);
        }

        public override void Notify_RecipeProduced(Pawn pawn)
        {
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        }

        public override void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            thing.def.soundDrop.PlayOneShot(this);
        }


        protected override void Tick()
        {
            base.Tick();

            if (activeBill != null && PoweredOn)
                activeBill.BillTick();

            if (PoweredOn != _lastPoweredOn)
                _fadeTicks = 0f;

            _fadeTicks = Mathf.Min(_fadeTicks + 1f, FADE_DURATION_TICKS);
            _lastPoweredOn = PoweredOn;

            _electricEffecterHandler.Tick();
        }

        protected override string GetInspectStringExtra()
        {
            var sb = new StringBuilder();

            if (CommonSenseCheck.CheckForSetting())
                sb.AppendLine(CommonSenseCheck.MESSAGE_CONTENT.Colorize(ColorLibrary.RedReadable));

            sb.AppendLine(USH_DefOf.USH_GlittertechPowerStored.LabelCap + ": " + this.GetStatValue(USH_DefOf.USH_GlittertechPowerStored).ToStringPercent());

            if (billStack.FirstShouldDoNow is Bill_Glittertech firstBill and not null
                && firstBill.GlittertechExt is { powerNeeded: var powerNeeded }
                && firstBill.recipe.products.FirstOrDefault()?.thingDef.label is string productLabel
                && firstBill.State == FormingState.Gathering)
            {
                bool hasStoredPower = HasStoredPower(powerNeeded);
                float powerMultiplied = PowerNeededWithStat(powerNeeded);

                if (hasStoredPower)
                    sb.AppendLine("USH_GE_WillDraw".Translate(powerMultiplied, productLabel).Colorize(Color.cyan));
                else
                    sb.AppendLine("USH_GE_NoPowerStored".Translate(powerMultiplied, productLabel, StoredPower()).Colorize(Color.red));
            }

            if (GlitterBill is not null && GlitterBill.State is not (FormingState.Gathering or FormingState.Formed))
            {
                var totalPeriod = GetTotalTimeForActiveBill().ToStringTicksToPeriod();
                sb.AppendLine("USH_GE_FormTimeTotal".Translate(totalPeriod));
            }

            return sb.ToString().TrimEnd();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            DrawFormingThing(drawLoc);

            DrawBar(drawLoc);
        }

        private void DrawFormingThing(Vector3 drawLoc)
        {
            if (activeBill == null || activeBill.State == FormingState.Gathering)
                return;

            if (FormingMaterial == null)
                return;

            Material mat = GetFormingThingMat();
            Mesh mesh = MeshPool.GridPlane(Vector2.one * GlitterBill.GlittertechExt.fabricatorScale);
            Quaternion quat = Quaternion.Euler(new(0, GlitterBill.GlittertechExt.fabricatorRotationY, 0));

            Graphics.DrawMesh(mesh, GetFormingThingLoc(drawLoc), quat, mat, 0);
        }

        private Vector3 GetFormingThingLoc(Vector3 drawLoc)
        {
            Vector3 result = drawLoc;

            float bobHeight = 0.04f;
            float bobSpeedDivideBy = 300f;
            float fullOscillation = (float)Math.PI * 2f;
            float yOffset = .02f;

            result.y += yOffset;
            result.z += GlitterBill.GlittertechExt.fabricatorOffsetY;
            result.z += Mathf.Sin(fullOscillation * GenTicks.TicksGame / bobSpeedDivideBy) * bobHeight;

            return result;
        }

        private float GetFormingThingAlpha()
        {
            float t = _fadeTicks / FADE_DURATION_TICKS;

            float alpha = _lastPoweredOn
                ? Mathf.Lerp(0f, 1f, t)
                : Mathf.Lerp(1f, 0f, t);

            return alpha;
        }

        private Material GetFormingThingMat()
        {
            float alphaMax = 0.5f;
            float alpha = GetFormingThingAlpha();

            FormingMaterial.color = new Color(1f, 1f, 1f, alpha * alphaMax);

            return FormingMaterial;
        }

        private void DrawBar(Vector3 drawLoc)
        {
            GenDraw.FillableBarRequest barDrawData = BarDrawData;
            barDrawData.center = drawLoc;
            barDrawData.fillPercent = CurrentBillFormingPercent;
            barDrawData.filledMat = FormingCycleBarFilledMat;
            barDrawData.unfilledMat = FormingCycleUnfilledMat;
            barDrawData.rotation = Rotation;
            GenDraw.DrawFillableBar(barDrawData);
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            if (!DebugSettings.ShowDevGizmos)
                yield break;

            if (GlitterBill != null && GlitterBill.State != FormingState.Gathering && GlitterBill.State != FormingState.Formed)
            {
                yield return new Command_Action
                {
                    action = new Action(GlitterBill.ForceCompleteAllCycles),
                    defaultLabel = "DEV: Complete all cycles"
                };
            }
        }

        private int GetTotalTimeForActiveBill()
        {
            float wholeCycleTicks = (GlitterBill.recipe.gestationCycles - GlitterBill.GestationCyclesCompleted - 1) * GlitterBill.recipe.formingTicks;
            float currentCycleTicks = GlitterBill.formingTicks;

            return Mathf.CeilToInt((wholeCycleTicks + currentCycleTicks) / GlitterBill.FormingSpeedMultiplier());
        }

        private float StoredPower()
        {
            if (PowerTrader.PowerNet == null)
                return 0;

            return PowerTrader.PowerNet.CurrentStoredEnergy();
        }

        public bool HasStoredPower(float powerNeeded, bool considerStats = true)
        {
            if (DebugSettings.unlimitedPower)
                return true;

            if (considerStats)
                powerNeeded = PowerNeededWithStat(powerNeeded);

            return StoredPower() >= powerNeeded;
        }

        private void DrawPowerFromNet(float powerToDraw)
        {
            foreach (CompPowerBattery battery in PowerTrader.PowerNet.batteryComps)
            {
                if (powerToDraw >= battery.StoredEnergy)
                {
                    powerToDraw -= battery.StoredEnergy;
                    battery.DrawPower(battery.StoredEnergy);
                }
                else
                {
                    battery.DrawPower(powerToDraw);
                    break;
                }
            }
        }

        public float PowerNeededWithStat(Bill_Glittertech bill)
        {
            return PowerNeededWithStat(bill.GlittertechExt.powerNeeded);
        }

        public float PowerNeededWithStat(float initialPowerNeeded)
        {
            return initialPowerNeeded * this.GetStatValue(USH_DefOf.USH_GlittertechPowerStored);
        }
    }
}
