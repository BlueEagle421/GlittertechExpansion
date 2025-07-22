using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class CompProperties_MemoryCell : CompProperties
{
    public int expireTicks;

    public CompProperties_MemoryCell()
    {
        compClass = typeof(CompMemoryCell);
    }
}

public class CompMemoryCell : ThingComp
{
    public MemoryCellData MemoryCellData;
    public CompProperties_MemoryCell CellProps => (CompProperties_MemoryCell)props;

    private const int TICK_RARE = 250;
    private int _expireTicks;
    public int ExpireTicks => _expireTicks;

    public override void PostPostMake()
    {
        base.PostPostMake();

        _expireTicks = CellProps.expireTicks;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        if (respawningAfterLoad && _expireTicks == 0)
            _expireTicks = ExpireTicks;
    }

    public override void CompTickRare()
    {
        base.CompTickRare();

        _expireTicks -= TICK_RARE;

        if (_expireTicks < 0)
            Expire();
    }

    private void Expire()
    {
        if (parent.holdingOwner.Owner is CompMemoryCellContainer container)
            container.EjectContent();
        else if (parent.holdingOwner.Owner is not Map)
            parent.holdingOwner.TryDrop(parent, ThingPlaceMode.Near, out _);

        Thing newThing = EraseMemory(false);
        Messages.Message("USH_GE_Expired".Translate(parent.Label), new LookTargets(newThing), MessageTypeDefOf.NegativeEvent);
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.CompInspectStringExtra());
        sb.AppendLine("USH_GE_ExpiresIn".Translate() + ": " + _expireTicks.ToStringTicksToPeriod());
        sb.AppendLine(MemoryCellData.GetInspectString());

        return sb.ToString().Trim();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Command_Action eraseCommand = new()
        {
            action = () => EraseMemory(),
            defaultLabel = "USH_GE_CommandEraseMemory".Translate(),
            defaultDesc = "USH_GE_CommandEraseMemoryDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EraseMemory")
        };
        yield return eraseCommand;

        if (!DebugSettings.ShowDevGizmos)
            yield break;

        yield return new Command_Action
        {
            action = () => _expireTicks = CellProps.expireTicks,
            defaultLabel = "DEV: Reset expire time"
        };

        yield return new Command_Action
        {
            action = () => _expireTicks = 2500,
            defaultLabel = "DEV: Expire in 1 hour"
        };
    }

    private Thing EraseMemory(bool autoSelect = true)
    {
        IntVec3 pos = parent.Position;
        Map map = parent.Map;
        parent.Destroy();

        var newThing = ThingMaker.MakeThing(USH_DefOf.USH_MemoryCellEmpty);

        GenPlace.TryPlaceThing(newThing, pos, map, ThingPlaceMode.Near);

        if (autoSelect) Find.Selector.Select(newThing, playSound: false, forceDesignatorDeselect: false);
        return newThing;
    }

    public override string TransformLabel(string label)
    {
        string moodOffset = MemoryCellData.moodOffset.ToString();
        string colorizedMood = moodOffset.Colorize(MemoryUtils.GetThoughtColor(MemoryCellData.IsPositive()));
        return $"{label} ({colorizedMood})";
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref _expireTicks, nameof(_expireTicks));
        Scribe_Deep.Look(ref MemoryCellData, nameof(MemoryCellData));
    }

    public override bool AllowStackWith(Thing other) => false;
}