using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class MapComponent_RepairManager(Map map) : MapComponent(map)
{
    private int _ticksPassed;
    private const int TICK_CHECK_INTERVAL = 250;
    private readonly List<CompGlittertechRepairer> repairers = [];
    public List<Thing> ToRepair = [];

    public void Register(CompGlittertechRepairer comp) => repairers.Add(comp);
    public void Unregister(CompGlittertechRepairer comp) => repairers.Remove(comp);

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        _ticksPassed++;

        if (_ticksPassed >= TICK_CHECK_INTERVAL)
        {
            ToRepair = map.listerBuildingsRepairable.RepairableBuildings(Faction.OfPlayer);
            repairers.ForEach(x => x.TryToStartRepairing());
            _ticksPassed = 0;
        }
    }

    public void RemoveRepaired(Thing thing)
    {
        ToRepair.Remove(thing);
        repairers.ForEach(x => x.TryToStartRepairing());
    }
}

public class CompProperties_GlittertechRepairer : CompProperties
{
    public float repairRadius = 8.9f;
    public float repairInterval = 10;
    public string activeOverlayPath;

    public CompProperties_GlittertechRepairer() => compClass = typeof(CompGlittertechRepairer);
}

public class CompGlittertechRepairer : ThingComp
{
    public CompProperties_GlittertechRepairer Props => (CompProperties_GlittertechRepairer)props;

    private MapComponent_RepairManager _cachedManager;
    private MapComponent_RepairManager Manager
    {
        get
        {
            _cachedManager ??= parent.Map.GetComponent<MapComponent_RepairManager>();
            return _cachedManager;
        }
    }

    private bool _isRepairing;
    private Thing _currentlyRepairing;
    private Effecter _repairEffecter;
    private int _repairTickCounter;

    private CompPowerTrader _compPower;
    private CompStunnable _compStunnable;
    private CompGlower _compGlower;

    private float _cachedRadiusSquared = -1;
    private float RepairRadiusSquared
    {
        get
        {
            if (_cachedRadiusSquared == -1)
                _cachedRadiusSquared = Props.repairRadius * Props.repairRadius;

            return _cachedRadiusSquared;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Manager.Register(this);

        _compStunnable = parent.GetComp<CompStunnable>();
        _compPower = parent.GetComp<CompPowerTrader>();
        _compGlower = parent.GetComp<CompGlower>();

        RepairStopped();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        previousMap.GetComponent<MapComponent_RepairManager>().Unregister(this);
    }

    public void TryToStartRepairing()
    {
        if (Manager.ToRepair.Count == 0)
            return;

        _currentlyRepairing = Manager.ToRepair.Find(CanRepairThing);

        if (_currentlyRepairing != null) RepairStarted();
    }

    public override void CompTick()
    {
        base.CompTick();

        _repairEffecter?.EffectTick(new TargetInfo(_currentlyRepairing), new TargetInfo(parent));

        RepairTick();
    }

    private void RepairTick()
    {
        if (!_isRepairing)
            return;

        if (!CanRepair() || _currentlyRepairing == null)
        {
            RepairStopped();
            return;
        }

        _repairTickCounter++;

        if (_repairTickCounter < Props.repairInterval)
            return;

        if (_currentlyRepairing.Destroyed)
        {
            RepairStopped();
            return;
        }

        _repairTickCounter = 0;

        _currentlyRepairing.HitPoints = Mathf.Min(_currentlyRepairing.MaxHitPoints, _currentlyRepairing.HitPoints + 1);

        if (_currentlyRepairing.HitPoints == _currentlyRepairing.MaxHitPoints)
        {
            Manager.RemoveRepaired(_currentlyRepairing);
            RepairStopped();
        }
    }

    private void RepairStarted()
    {
        if (_isRepairing)
            return;

        _compPower.PowerOutput = -_compPower.Props.PowerConsumption;
        _compGlower.GlowColor = ColorInt.FromHdrColor(Color.white);

        _repairEffecter?.Cleanup();
        _repairEffecter = USH_DefOf.USH_GlittertechRepair.Spawn();
        _repairEffecter.Trigger(new TargetInfo(_currentlyRepairing), new TargetInfo(parent), -1);

        _isRepairing = true;
    }

    private void RepairStopped()
    {
        if (!_isRepairing)
            return;

        _compPower.PowerOutput = -_compPower.Props.idlePowerDraw;
        _compGlower.GlowColor = ColorInt.FromHdrColor(Color.clear);

        _repairEffecter?.Cleanup();
        _repairEffecter = null;

        _isRepairing = false;
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        if (_currentlyRepairing != null)
            sb.AppendLine($"Repairing: {_currentlyRepairing.Label}");

        if (Prefs.DevMode)
        {
            sb.AppendLine($"Can repair: {CanRepair()}");
            sb.AppendLine($"To repair (manager): {string.Join(", ", Manager.ToRepair)}");
        }

        return sb.ToString().TrimEnd();
    }

    private bool CanRepair()
    {
        if (!_compPower.PowerOn)
            return false;

        if (_compStunnable.StunHandler.Stunned)
            return false;

        return true;
    }

    private bool CanRepairThing(Thing t)
    {
        if (t.Destroyed)
            return false;

        if (!t.def.useHitPoints)
            return false;

        if (t.Position.DistanceToSquared(parent.Position) > RepairRadiusSquared)
            return false;

        return true;
    }
}