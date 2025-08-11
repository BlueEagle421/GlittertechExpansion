using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class MemoryCell : ThingWithComps, IBillGiver, IBillGiverWithTickAction
{
    public int expireTicks = 7200000; //to do: add MemoryCellModExtension
    public MemoryCellData MemoryCellData;

    private const int TICK_RARE = 250;
    private float _expireTicks;
    public float ExpireTicksLeft
    {
        get => _expireTicks;
        set
        {
            _expireTicks = Mathf.Max(0, value);

            if (Mathf.Approximately(_expireTicks, 0f)) Expire();
        }
    }

    private float _expireTimeMultiplier = 1f;
    public float ExpireTimeMultiplier
    {
        get => _expireTimeMultiplier;
        set => Mathf.Max(0f, value);
    }

    protected BillStack _billStack;
    public BillStack BillStack => _billStack;
    public IEnumerable<IntVec3> IngredientStackCells
        => [GenAdj.CellsAdjacent8Way(this).Where(x => x.Walkable(Map)).RandomElement()];

    public MemoryCell()
    {
        _billStack = new BillStack(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        foreach (Bill item in _billStack)
            item.ValidateSettings();
    }

    public override void PostPostMake()
    {
        base.PostPostMake();

        _expireTicks = expireTicks;
    }

    public override void TickRare()
    {
        base.TickRare();

        _expireTicks -= TICK_RARE * _expireTimeMultiplier;

        if (_expireTicks < 0)
            Expire();
    }

    private void Expire()
    {
        if (holdingOwner.Owner is CompMemoryCellContainer container)
            container.EjectContent();
        else if (holdingOwner.Owner is not Map map)
            holdingOwner.TryDrop(this, ThingPlaceMode.Near, out _);

        Thing newThing = EraseMemory(false);
        Messages.Message("USH_GE_Expired".Translate(Label), new LookTargets(newThing), MessageTypeDefOf.NegativeEvent);
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.GetInspectString());
        sb.AppendLine("USH_GE_ExpiresIn".Translate() + ": " + ((int)(_expireTicks * _expireTimeMultiplier)).ToStringTicksToPeriod());
        sb.AppendLine(MemoryCellData.GetInspectString());

        return sb.ToString().Trim();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
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
            action = () => _expireTicks = expireTicks,
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
        IntVec3 pos = Position;
        Map map = Map;

        Destroy();

        var newThing = ThingMaker.MakeThing(USH_DefOf.USH_MemoryCellEmpty);

        GenPlace.TryPlaceThing(newThing, pos, map, ThingPlaceMode.Near);

        if (autoSelect) Find.Selector.Select(newThing, playSound: false, forceDesignatorDeselect: false);
        return newThing;
    }

    // public override string TransformLabel(string label)
    // {
    //     string moodOffset = MemoryCellData.moodOffset.ToString();
    //     string colorizedMood = moodOffset.Colorize(MemoryUtils.GetThoughtColor(MemoryCellData.IsPositive()));
    //     return $"{label} ({colorizedMood})";
    // }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref _billStack, nameof(_billStack), this);
        Scribe_Values.Look(ref _expireTimeMultiplier, nameof(_expireTimeMultiplier));
        Scribe_Values.Look(ref _expireTicks, nameof(_expireTicks));
        Scribe_Deep.Look(ref MemoryCellData, nameof(MemoryCellData));
    }

    public override bool CanStackWith(Thing other) => false;
    public bool CurrentlyUsableForBills() => true;
    public bool UsableForBillsAfterFueling() => true;
    public void Notify_BillDeleted(Bill bill) { }
    public void UsedThisTick() { }

    public struct MemoryModData
    {
        public string label;
        public int count;
        public int maxCount;
    }
}