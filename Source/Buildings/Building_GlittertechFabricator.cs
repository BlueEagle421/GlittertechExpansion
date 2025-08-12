using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using System.Text;
using System.Linq;

namespace USH_GE;

[StaticConstructorOnStartup]
public class Building_GlittertechFabricator : Building_WorkTableAutonomous
{

    public Bill_Glittertech GlitterBill => ActiveBill as Bill_Glittertech;

    private static readonly Material FormingCycleBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.98f, 0.46f, 0f), false);
    private static readonly Material FormingCycleUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f), false);
    private EffecterHandler _electricEffecterHandler;
    private const float FULL_OSCILLATION = Mathf.PI * 2f;
    private const float FADE_DURATION_TICKS = 300f;
    private float _fadeTicks = FADE_DURATION_TICKS;
    private bool _lastPoweredOn = true;

    private float _lastZPos;
    private float _bobTicks = 0f;
    private int _lastLocGlobalTick = GenTicks.TicksGame;

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

    private readonly MaterialPropertyBlock _matProps = new();
    private Material _sharedMatCached;
    public Material SharedFormingMaterial
    {
        get
        {
            _sharedMatCached ??= MaterialPool.MatFrom(GlitterBill.GetProductGraphic.path, ShaderDatabase.Transparent);

            return _sharedMatCached;
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

        USH_DefOf.USH_GlitterFabricationStart.PlayOneShot(this);

        _matProps.SetTexture("_MainTex", ContentFinder<Texture2D>.Get(GlitterBill.GetProductGraphic.path));
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
        base.Notify_RecipeProduced(pawn);
    }

    public override void Notify_HauledTo(Pawn hauler, Thing thing, int count)
    {
        base.Notify_HauledTo(hauler, thing, count);

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

        if (SharedFormingMaterial == null)
            return;

        _matProps.SetColor("_Color", new Color(1, 1, 1, GetFormingThingAlpha()));
        Material mat = SharedFormingMaterial;
        Mesh mesh = MeshPool.GridPlane(Vector2.one * GlitterBill.GlittertechExt.fabricatorScale);
        Quaternion quat = Quaternion.Euler(new(0, GlitterBill.GlittertechExt.fabricatorRotationY, 0));

        Graphics.DrawMesh(mesh, GetFormingThingLoc(drawLoc), quat, mat, 0, null, 0, _matProps);
    }

    private Vector3 GetFormingThingLoc(Vector3 drawLoc)
    {
        Vector3 result = drawLoc;

        float bobHeight = 0.04f;
        float bobSpeedDivideBy = 300f;
        float yOffset = .02f;

        result.y += yOffset;
        result.z += GlitterBill.GlittertechExt.fabricatorOffsetY;

        int currentTick = GenTicks.TicksGame;
        int deltaTicks = currentTick - _lastLocGlobalTick;
        _lastLocGlobalTick = currentTick;

        if (!WaitingForManualInspection)
            _bobTicks += deltaTicks;


        float angle = _bobTicks * FULL_OSCILLATION / bobSpeedDivideBy;
        _lastZPos = Mathf.Sin(angle) * bobHeight;

        result.z += _lastZPos;
        return result;
    }

    private float GetFormingThingAlpha()
    {
        float m = 0.5f;
        float t = _fadeTicks / FADE_DURATION_TICKS;

        float alpha = _lastPoweredOn
            ? Mathf.Lerp(0f, 1f, t)
            : Mathf.Lerp(1f, 0f, t);

        if (WaitingForManualInspection)
        {
            float bobSpeedDivideBy = 350f;
            float minAlpha = 0.25f;
            alpha *= Mathf.Max(minAlpha, Mathf.Abs(Mathf.Sin(FULL_OSCILLATION * GenTicks.TicksGame / bobSpeedDivideBy)));
        }

        return alpha * m;
    }

    private bool WaitingForManualInspection
        => GlitterBill != null
            && (GlitterBill.State == FormingState.Preparing || GlitterBill.State == FormingState.Formed);



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

    public float StoredPower()
    {
        if (DebugSettings.unlimitedPower)
            return 99999;

        if (PowerTrader.PowerNet == null)
            return 0;

        return PowerTrader.PowerNet.CurrentStoredEnergy();
    }

    public bool HasStoredPower(float powerNeeded, bool considerStats = true)
    {
        if (considerStats)
            powerNeeded = PowerNeededWithStat(powerNeeded);

        return StoredPower() >= powerNeeded;
    }

    private void DrawPowerFromNet(float powerToDraw)
    {
        if (PowerTrader.PowerNet == null)
            return;

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
