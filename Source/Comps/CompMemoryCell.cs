using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class CompProperties_MemoryCell : CompProperties
{
    public JobDef jobDef;

    public CompProperties_MemoryCell()
    {
        compClass = typeof(CompMemoryCell);
    }
}

public class CompMemoryCell : ThingComp
{
    public MemoryCellData MemoryCellData;
    public CompProperties_MemoryCell CellProps => (CompProperties_MemoryCell)props;

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.CompInspectStringExtra());
        sb.AppendLine(MemoryCellData.GetInspectString());

        return sb.ToString().Trim();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Command_Action eraseCommand = new()
        {
            action = EraseMemory,
            defaultLabel = "USH_GE_CommandEraseMemory".Translate(),
            defaultDesc = "USH_GE_CommandEraseMemoryDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EraseMemory")
        };
        yield return eraseCommand;
    }

    private void EraseMemory()
    {
        IntVec3 pos = parent.Position;
        Map map = parent.Map;
        parent.Destroy();

        var newThing = ThingMaker.MakeThing(USH_DefOf.USH_MemoryCellEmpty);

        GenPlace.TryPlaceThing(newThing, pos, map, ThingPlaceMode.Near);

        Find.Selector.Select(newThing, playSound: false, forceDesignatorDeselect: false);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Deep.Look(ref MemoryCellData, nameof(MemoryCellData));
    }

    public override bool AllowStackWith(Thing other)
    {
        return false;
    }
}