using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using System.Text;
using System.Linq;

namespace GlitterworldUprising
{
    [StaticConstructorOnStartup]
    public class Building_GlittertechFabricator : Building_WorkTableAutonomous
    {

        public Bill_Glittertech GlitterBill => ActiveBill as Bill_Glittertech;

        private static readonly Material FormingCycleBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.98f, 0.46f, 0f), false);
        private static readonly Material FormingCycleUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f), false);
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

        private bool _recacheGraphic = true;
        private Graphic _cachedGraphic;

        public Graphic FormingGraphic
        {
            get
            {
                if (_cachedGraphic == null || _recacheGraphic)
                {
                    if (ActiveBill?.recipe?.products[0] == null)
                        return null;

                    _cachedGraphic = ActiveBill.recipe.products[0].thingDef.graphic;

                    if (_cachedGraphic is Graphic_StackCount graphic_StackCount)
                        _cachedGraphic = graphic_StackCount.SubGraphicForStackCount(ActiveBill.recipe.products[0].count, ActiveBill.recipe.products[0].thingDef);

                    _cachedGraphic = _cachedGraphic.GetCopy(_cachedGraphic.drawSize * 0.6f, null);
                }

                return _cachedGraphic;
            }
        }

        private EffecterHandler _electricEffecterHandler;

        private const float Y_OFFSET = .018292684f;
        private const float FORMING_ALPHA_MULTIPLIER = .5f;
        private const float FADE_DURATION_TICKS = 300f;
        private float _fadeTicks = FADE_DURATION_TICKS;
        private bool _lastPoweredOn = true;

        public override void PostMake()
        {
            base.PostMake();
            _electricEffecterHandler = new EffecterHandler(this, USHDefOf.USH_ElectricForming);
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            _electricEffecterHandler = new EffecterHandler(this, USHDefOf.USH_ElectricForming);
        }

        public override void Notify_StartForming(Pawn billDoer)
        {
            if (!HasStoredPower(GlitterBill.GlittertechExt.powerNeeded))
                return;

            base.Notify_StartForming(billDoer);

            DrawPowerFromNet(GlitterBill.GlittertechExt.powerNeeded);

            if (Spawned && Map != null)
                _electricEffecterHandler.StartMaintaining(360, GlitterBill.GlittertechExt.analyzerOffsetY);

            SoundDefOf.MechGestatorCycle_Started.PlayOneShot(this);

            _recacheGraphic = true;
        }

        public override void Notify_FormingCompleted()
        {
            innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            SoundDefOf.MechGestatorBill_Completed.PlayOneShot(this);
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

            if (billStack.FirstShouldDoNow is Bill_Glittertech firstBill and not null
                && firstBill.GlittertechExt is { powerNeeded: var powerNeeded }
                && firstBill.recipe.products.FirstOrDefault()?.thingDef.label is string productLabel)
            {
                bool hasStoredPower = HasStoredPower(powerNeeded);

                string key = hasStoredPower
                    ? "USH_GU_WillDraw"
                    : "USH_GU_NoPowerStored";

                var args = hasStoredPower
                    ? new object[] { powerNeeded, productLabel }
                    : [productLabel, powerNeeded];

                var color = hasStoredPower
                    ? Color.cyan
                    : Color.red;

#pragma warning disable CS0618 // Type or member is obsolete
                sb.AppendLine(key.Translate(args).Colorize(color));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (GlitterBill?.State is FormingState.Gathering)
            {
                var totalPeriod = GetTotalTimeForActiveBill().ToStringTicksToPeriod();
                sb.AppendLine("USH_GU_FormTimeTotal".Translate(totalPeriod));
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

            if (FormingGraphic == null)
                return;

            Vector3 loc = drawLoc;
            loc.y += Y_OFFSET;

            loc.z += GlitterBill.GlittertechExt.analyzerOffsetY;
            loc.z += Mathf.PingPong(Find.TickManager.TicksGame * 0.0005f, 0.08f); ;

            float t = _fadeTicks / FADE_DURATION_TICKS;

            float alpha = _lastPoweredOn
                ? Mathf.Lerp(0f, 1f, t)
                : Mathf.Lerp(1f, 0f, t);

            Material transparentMat = MaterialPool.MatFrom(FormingGraphic.path, ShaderDatabase.Transparent);
            transparentMat.color = new Color(1f, 1f, 1f, alpha * FORMING_ALPHA_MULTIPLIER);

            Mesh mesh = FormingGraphic.MeshAt(Rot4.North);
            Quaternion quat = FormingGraphic.QuatFromRot(Rot4.North);

            Graphics.DrawMesh(mesh, loc, quat, transparentMat, 0);
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

        public bool HasStoredPower(float powerNeeded, bool considerStats = true)
        {
            if (DebugSettings.unlimitedPower)
                return true;

            if (considerStats)
                powerNeeded *= this.GetStatValue(USHDefOf.USH_GlittertechPowerStored);

            return PowerStoredInNet(PowerTrader.PowerNet) >= powerNeeded;
        }

        private float PowerStoredInNet(PowerNet powerNet)
        {
            float power = 0;

            if (powerNet == null)
                return 0;

            foreach (CompPowerBattery battery in powerNet.batteryComps)
                power += battery.StoredEnergy;

            return power;
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
    }
}
