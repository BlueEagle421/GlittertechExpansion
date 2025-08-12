using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_MemoryCellContainer : CompProperties_ThingContainer
{
    public SoundDef insertedSoundDef;
    public CompProperties_MemoryCellContainer() => compClass = typeof(CompMemoryCellContainer);
}

public class CompMemoryCellContainer : CompThingContainer, IMemoryCellHolder
{
    public CompProperties_MemoryCellContainer PropsSampleContainer => (CompProperties_MemoryCellContainer)props;
    private MemoryCell _cellComp;
    public MemoryCell ContainedCell => _cellComp;

    public Thing SourceThing => parent;
    public IntVec3 InsertPos => parent.InteractionCell;

    public Action OnInserted, OnExtracted;
    public ThingOwner MemoryCellOwner => innerContainer;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _cellComp = (MemoryCell)ContainedThing;
    }

    public override string CompInspectStringExtra()
    {
        return "Contents".Translate() + ": " + (ContainedCell == null ? ((string)"Nothing".Translate()) : ContainedCell.LabelCap);
    }

    public virtual void Notify_CellInserted(MemoryCell memoryCell, Pawn doer)
    {
        _cellComp = (MemoryCell)ContainedThing;

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

    public AcceptanceReport CanInsertCell(MemoryCell memoryCell)
    {
        if (Full)
            return "USH_GE_ContainerFull".Translate(parent.Named("BUILDING"));

        return true;
    }
}
