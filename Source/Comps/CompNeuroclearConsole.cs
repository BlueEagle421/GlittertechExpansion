﻿using RimWorld;
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

        Color egesColor = WillWork(room, def) ? ColorLibrary.BabyBlue : Color.red;

        GenDraw.DrawFieldEdges([.. room.Cells], egesColor, null);
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
    Map _currentMap;
    CompRefuelable _refuelableComp;

    public CompProperties_NeuroclearConsole ModuleProps => (CompProperties_NeuroclearConsole)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _currentMap = parent.Map;
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

    private void SpawnFleckEffect(IntVec3 position) => FleckMaker.Static(position, _currentMap, ModuleProps.fleckDef, Rand.Range(0.8f, 1.25f));

    private void PlaySoundEffect() => ModuleProps.soundDef.PlayOneShot(new TargetInfo(parent.Position, parent.Map, false));

    private IEnumerable<IntVec3> RoomCells
        => parent.GetRoom().Cells;

    private List<Pawn> PawnsToDesensitize()
    {
        List<Pawn> result = [];

        foreach (var cell in RoomCells)
            foreach (Thing thing in _currentMap.thingGrid.ThingsAt(cell))
                if (thing is Pawn pawn)
                    result.Add(pawn);

        return result;
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
}