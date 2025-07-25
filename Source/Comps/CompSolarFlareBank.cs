using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace USH_GE;

public class MapComponent_SolarFlareBank(Map map) : MapComponent(map)
{
    private readonly HashSet<CompSolarFlareBank> _solarBanks = [];
    public List<CompSolarFlareBank> AllAvailableSolarBanks => [.. _solarBanks.Where(x => x.CanInterceptReport())];
    public void Register(CompSolarFlareBank comp) => _solarBanks.Add(comp);
    public void Unregister(CompSolarFlareBank comp) => _solarBanks.Remove(comp);
}

public class CompProperties_SolarFlareBank : CompProperties_Power
{
    public int fuelConsumption = 1;
    public int dischargeTicks = 60000 * 6; // 1 day * 6
    public string activeOverlayPath;
    public CompProperties_SolarFlareBank() => compClass = typeof(CompSolarFlareBank);
}


[StaticConstructorOnStartup]
public class CompSolarFlareBank : CompPowerPlant
{
    public CompProperties_SolarFlareBank BankProps => (CompProperties_SolarFlareBank)props;
    private static readonly Vector2 BarSize = new(1.2f, 0.14f);
    private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new(0.5f, 0.475f, 0.1f));
    private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new(0.15f, 0.15f, 0.15f));

    private bool _isDischarging;
    private int _dischargeTicksLeft = 0;
    private const int DISCHARGE_INTERVAL = 2000;
    private const float Z_OFFSET = .018292684f;
    private const float Y_OFFSET = 0.5f;

    private Material _cachedOverlayMat;
    private Material OverlayMaterial
    {
        get
        {
            _cachedOverlayMat ??= MaterialPool.MatFrom(BankProps.activeOverlayPath, ShaderDatabase.Transparent);

            return _cachedOverlayMat;
        }
    }

    public virtual void Notify_SolarFlareIntercepted()
    {
        StartGenerating();
    }

    public override void CompTick()
    {
        base.CompTick();

        if (!parent.IsHashIntervalTick(DISCHARGE_INTERVAL))
            return;

        _dischargeTicksLeft -= DISCHARGE_INTERVAL;

        if (_dischargeTicksLeft <= 0)
            StopGenerating();
    }

    private void StartGenerating()
    {
        if (!CanInterceptReport())
            return;

        refuelableComp.ConsumeFuel(BankProps.fuelConsumption);
        _isDischarging = true;
        _dischargeTicksLeft = BankProps.dischargeTicks;
        UpdateDesiredPowerOutput();
    }

    private void StopGenerating()
    {
        _isDischarging = false;
        _dischargeTicksLeft = 0;
    }

    protected override float DesiredPowerOutput
    {
        get
        {
            if (!_isDischarging)
                return 0;

            return _dischargeTicksLeft / (float)BankProps.dischargeTicks * -Props.PowerConsumption;
        }
    }

    public override void UpdateDesiredPowerOutput() => PowerOutput = _isDischarging ? DesiredPowerOutput : 0;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        parent.Map.GetComponent<MapComponent_SolarFlareBank>().Register(this);

        refuelableComp = parent.GetComp<CompRefuelable>();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        map.GetComponent<MapComponent_SolarFlareBank>().Unregister(this);
        base.PostDeSpawn(map, mode);
    }

    public override void PostDraw()
    {
        base.PostDraw();

        DrawActiveOverlay(parent.DrawPos);

        DrawBar();
    }

    private void DrawBar()
    {
        if (!_isDischarging)
            return;

        GenDraw.FillableBarRequest r = new()
        {
            center = parent.DrawPos + Vector3.up * Z_OFFSET + Vector3.forward * Y_OFFSET,
            size = BarSize,
            fillPercent = PowerOutput / (0f - Props.PowerConsumption),
            filledMat = PowerPlantSolarBarFilledMat,
            unfilledMat = PowerPlantSolarBarUnfilledMat,
            margin = 0.15f
        };
        Rot4 rotation = parent.Rotation;
        rotation.Rotate(RotationDirection.Clockwise);
        r.rotation = rotation;
        GenDraw.DrawFillableBar(r);
    }

    private void DrawActiveOverlay(Vector3 drawLoc)
    {
        if (!_isDischarging)
            return;

        Vector3 loc = drawLoc;
        loc += parent.def.graphicData.drawOffset;
        loc.y += Z_OFFSET;

        OverlayMaterial.color = new Color(1f, 1f, 1f, Mathf.Abs(Mathf.PingPong(Find.TickManager.TicksGame * 0.004f, 1f)));

        Mesh mesh = parent.Graphic.MeshAt(Rot4.North);
        Quaternion quat = parent.Graphic.QuatFromRot(parent.Rotation);

        Graphics.DrawMesh(mesh, loc, quat, OverlayMaterial, 0);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref _isDischarging, "_isDischarging");
        Scribe_Values.Look(ref _dischargeTicksLeft, "_dischargeTicksLeft");
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.CompInspectStringExtra());

        if (_isDischarging)
            sb.AppendLine("USH_GE_DischargeTimeLeft".Translate(_dischargeTicksLeft.ToStringTicksToPeriod()));

        if (!CanInterceptReport())
            sb.AppendLine("USH_GE_CantIntercept".Translate(CanInterceptReport().Reason).Colorize(Color.red));

        return sb.ToString().TrimEnd();
    }

    public AcceptanceReport CanInterceptReport()
    {
        if (refuelableComp?.Fuel < BankProps.fuelConsumption)
            return "NoFuel".Translate();

        return true;
    }
}