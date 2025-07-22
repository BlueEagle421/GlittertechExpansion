using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_MemoryCellContainer : CompProperties_ThingContainer
{
    public SoundDef insertedSoundDef;
    public CompProperties_MemoryCellContainer() => compClass = typeof(CompMemoryCellContainer);
}

public class CompMemoryCellContainer : CompThingContainer
{
    public CompProperties_MemoryCellContainer PropsSampleContainer => (CompProperties_MemoryCellContainer)props;
    private CompMemoryCell _cellComp;
    public CompMemoryCell ContainedCellComp => _cellComp;

    public Action OnInserted, OnExtracted;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _cellComp = ContainedThing?.TryGetComp<CompMemoryCell>();
    }

    public override string CompInspectStringExtra()
    {
        return "Contents".Translate() + ": " + (ContainedCellComp == null ? ((string)"Nothing".Translate()) : ContainedCellComp.parent.LabelCap);
    }

    public virtual void Notify_CellInserted(Pawn doer)
    {
        _cellComp = ContainedThing?.TryGetComp<CompMemoryCell>();

        SoundDef insertedSoundDef = PropsSampleContainer.insertedSoundDef;
        insertedSoundDef?.PlayOneShot(SoundInfo.InMap(parent));

        OnInserted?.Invoke();
    }

    public virtual void Notify_CellExtracted(Pawn doer)
    {
        _cellComp = null;

        OnExtracted?.Invoke();
    }


    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Command_Action ejectCommand = new()
        {
            action = EjectContent,
            defaultLabel = "USH_GE_CommandMemoryContainerEject".Translate(),
            defaultDesc = "USH_GE_CommandMemoryContainerEjectDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc8,
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectMemoryCell")
        };

        if (ContainedThing == null)
            ejectCommand.Disable("USH_GE_CommandMemoryContainerEjectFailEmpty".Translate());

        yield return ejectCommand;
    }

    public void EjectContent()
    {
        USH_DefOf.USH_Eject?.PlayOneShot(SoundInfo.InMap(parent));
        innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
        Notify_CellExtracted(null);
    }

    public static CompMemoryCellContainer FindFor(CompMemoryCell memoryCell, Pawn traveler, bool ignoreOtherReservations = false)
    {
        CompMemoryCellContainer compContainer =
        GenClosest.ClosestThingReachable(
            memoryCell.parent.PositionHeld,
            memoryCell.parent.MapHeld,
            ThingRequest.ForDef(USH_DefOf.USH_MemoryPylon),
            PathEndMode.InteractionCell,
            TraverseParms.For(traveler),
            9999f,
            Validator)
            .TryGetComp<CompMemoryCellContainer>();

        bool Validator(Thing x) =>
            x.TryGetComp(out CompMemoryCellContainer comp)
            && comp.Accepts(memoryCell.parent)
            && traveler.CanReserve(comp.parent, 1, -1, null, ignoreOtherReservations
        );

        if (compContainer != null)
            return compContainer;


        return null;
    }
}
