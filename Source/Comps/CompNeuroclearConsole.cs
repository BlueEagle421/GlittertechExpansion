using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class PlaceWorker_NeuroclearConsole : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        Map currentMap = Find.CurrentMap;
        Room room = center.GetRoom(currentMap);

        if (room == null)
            return;

        if (!room.ProperRoom)
            return;

        Color edgesColor = WillWork(room, def) ? ColorLibrary.BabyBlue : Color.red;

        GenDraw.DrawFieldEdges([.. room.Cells], edgesColor, null);
    }

    private bool WillWork(Room room, ThingDef thingDef)
    {
        if (room == null)
            return false;

        if (room.CellCount > thingDef.GetCompProperties<CompProperties_NeuroclearConsole>().maxRoomCellSize)
            return false;

        if (room.OpenRoofCount != 0)
            return false;

        return true;
    }
}

public class CompProperties_NeuroclearConsole : CompProperties_Interactable
{
    public int fuelConsumption;
    public HediffDef hediffDefToRemove;
    public FleckDef fleckDef;
    public SoundDef soundDef;
    public int maxRoomCellSize;

    public CompProperties_NeuroclearConsole() => compClass = typeof(CompNeuroclearConsole);
}

public class CompNeuroclearConsole : CompInteractable
{
    CompRefuelable _refuelableComp;

    public bool AutoUse => _autoUse;
    private bool _autoUse = true;

    private Texture2D _allowTex;
    public Texture2D IconAllow
    {
        get
        {
            _allowTex ??= ContentFinder<Texture2D>.Get("UI/Gizmos/DesensitizeAuto");
            return _allowTex;
        }
    }

    public CompProperties_NeuroclearConsole ModuleProps => (CompProperties_NeuroclearConsole)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _refuelableComp = parent.GetComp<CompRefuelable>();
    }

    protected override void OnInteracted(Pawn caster)
    {
        base.OnInteracted(caster);

        DesensitizePawns();
    }

    protected override bool TryInteractTick()
    {
        if (_refuelableComp.Fuel < ModuleProps.fuelConsumption)
            return false;

        if (PawnsToDesensitize().Count == 0)
            return false;

        return true;
    }

    private void DesensitizePawns()
    {
        foreach (Pawn pawn in PawnsToDesensitize())
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

        float fleckOnCellChance = 0.35f;

        foreach (var cell in RoomCells)
            if (Rand.Chance(fleckOnCellChance))
                SpawnFleckEffect(cell);
    }

    private void SpawnFleckEffect(IntVec3 position) => FleckMaker.Static(position, parent.Map, ModuleProps.fleckDef, Rand.Range(0.8f, 1.25f));

    private void PlaySoundEffect() => ModuleProps.soundDef.PlayOneShot(new TargetInfo(parent.Position, parent.Map, false));

    private IEnumerable<IntVec3> RoomCells
        => parent.GetRoom().Cells;

    private List<Pawn> PawnsToDesensitize()
    {
        List<Pawn> result = [];

        foreach (var cell in RoomCells)
            foreach (Thing thing in parent.Map.thingGrid.ThingsAt(cell))
                if (thing is Pawn pawn)
                    result.Add(pawn);

        return result;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Command_Toggle commandToggle = new()
        {
            icon = IconAllow,
            isActive = () => _autoUse,
            defaultLabel = "USH_GE_CommandAutoUsage".Translate(),
            defaultDesc = "USH_GE_CommandAutoUsageDesc".Translate(),
            toggleAction = () => _autoUse = !_autoUse,
        };

        yield return commandToggle;
    }

    public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
    {
        Room currentRoom = parent.GetRoom();

        if (currentRoom == null)
            return "USH_GE_NoRoom".Translate();

        if (parent.GetRoom().CellCount > ModuleProps.maxRoomCellSize)
            return "USH_GE_RoomTooBig".Translate(ModuleProps.maxRoomCellSize);

        if (parent.GetRoom().OpenRoofCount != 0)
            return "USH_GE_RoomUnroofed".Translate();

        if (PawnsToDesensitize().NullOrEmpty())
            return "USH_GE_NoAnesthesia".Translate();

        if (_refuelableComp.Fuel < ModuleProps.fuelConsumption)
            return "NoFuel".Translate();

        return base.CanInteract(activateBy);
    }

    public override string CompInspectStringExtra()
        => "USH_GE_FuelCost".Translate(_refuelableComp.Props.FuelLabel, ModuleProps.fuelConsumption);

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref _autoUse, nameof(_autoUse));
    }
}