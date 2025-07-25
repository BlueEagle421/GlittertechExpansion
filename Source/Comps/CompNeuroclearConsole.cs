using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_NeuroclearConsole : CompProperties_Interactable
{
    public int fuelConsumption;
    public HediffDef hediffDefToRemove;
    public FleckDef fleckDef;
    public SoundDef soundDef;

    public CompProperties_NeuroclearConsole() => compClass = typeof(CompNeuroclearConsole);
}

public class CompNeuroclearConsole : CompInteractable
{
    Map _currentMap;
    CompFacility _facilityComp;
    CompRefuelable _refuelableComp;

    public CompProperties_NeuroclearConsole ModuleProps => (CompProperties_NeuroclearConsole)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _currentMap = parent.Map;
        _facilityComp = parent.GetComp<CompFacility>();
        _refuelableComp = parent.GetComp<CompRefuelable>();
    }

    protected override void OnInteracted(Pawn caster)
    {
        base.OnInteracted(caster);

        DesensitizePawns();
    }

    protected override bool TryInteractTick()
    {
        if (_facilityComp.LinkedBuildings.Count == 0)
            return false;

        if (_refuelableComp.Fuel < ModuleProps.fuelConsumption)
            return false;

        if (PawnsInLinkedFacilities().Count == 0)
            return false;

        return true;
    }

    private void DesensitizePawns()
    {
        foreach (Pawn pawn in PawnsInLinkedFacilities())
        {
            Hediff toRemove = pawn.health.hediffSet.GetFirstHediffOfDef(ModuleProps.hediffDefToRemove);

            if (toRemove == null)
                continue;

            pawn.health.RemoveHediff(toRemove);
            _refuelableComp.ConsumeFuel(ModuleProps.fuelConsumption);

            PlaySoundEffect();

            SpawnFleckEffect(pawn.Position);
            SpawnFleckEffect(parent.Position);
        }
    }

    private void SpawnFleckEffect(IntVec3 position) => FleckMaker.Static(position, _currentMap, ModuleProps.fleckDef);

    private void PlaySoundEffect() => ModuleProps.soundDef.PlayOneShot(new TargetInfo(parent.Position, parent.Map, false));

    private List<Pawn> PawnsInLinkedFacilities()
    {
        List<Pawn> result = [];

        foreach (Thing building in _facilityComp.LinkedBuildings)
            foreach (Thing thing in _currentMap.thingGrid.ThingsAt(building.Position))
                if (thing as Pawn != null)
                    result.Add(thing as Pawn);

        return result;
    }

    public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
    {
        if (_refuelableComp.Fuel < ModuleProps.fuelConsumption)
            return "NoFuel".Translate();

        return base.CanInteract(activateBy);
    }

    public override string CompInspectStringExtra()
        => "USH_GE_FuelCost".Translate(_refuelableComp.Props.FuelLabel, ModuleProps.fuelConsumption);
}