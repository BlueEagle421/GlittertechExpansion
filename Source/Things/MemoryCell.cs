using System;
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
    private Dictionary<string, MemoryModData> _modDataMap =
        new(StringComparer.OrdinalIgnoreCase);

    private List<string> _exposeList1;
    private List<MemoryModData> _exposeList2;

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

    public virtual void Notify_ModInstalled(ModExtension_UseModifyCellBill modExt)
    {
        if (_modDataMap.TryGetValue(modExt.label, out var entry))
            entry.count++;
        else
            _modDataMap[modExt.label] = new MemoryModData
            {
                label = modExt.label,
                count = 1,
                maxCount = modExt.maxCount
            };
    }

    public int GetInstalledModCount(ModExtension_UseModifyCellBill modExt)
        => _modDataMap.TryGetValue(modExt.label, out var entry) ? entry.count : 0;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        foreach (Bill item in _billStack)
            item.ValidateSettings();
    }

    public override void PostPostMake()
    {
        base.PostPostMake();

        ExpireTicksLeft = expireTicks;
    }

    public override void TickRare()
    {
        base.TickRare();

        ExpireTicksLeft -= TICK_RARE * _expireTimeMultiplier;

        if (ExpireTicksLeft < 0)
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

        if (!_modDataMap.NullOrEmpty())
        {
            sb.AppendLine("Installed modifiers:");
            _modDataMap.Values.ToList().ForEach(x => sb.AppendLine("  - " + x.ToString()));
        }

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

    public override string Label
    {
        get
        {
            string moodOffset = MemoryCellData.moodOffset.ToString();
            string colorizedMood = moodOffset.Colorize(MemoryUtils.GetThoughtColor(MemoryCellData.IsPositive()));
            return $"{base.Label} ({colorizedMood})";
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref _billStack, nameof(_billStack), this);

        Scribe_Collections.Look(ref _modDataMap, nameof(_modDataMap), LookMode.Value, LookMode.Deep, ref _exposeList1, ref _exposeList2);

        Scribe_Values.Look(ref _expireTimeMultiplier, nameof(_expireTimeMultiplier));
        Scribe_Values.Look(ref _expireTicks, nameof(_expireTicks));
        Scribe_Deep.Look(ref MemoryCellData, nameof(MemoryCellData));
    }

    public override bool CanStackWith(Thing other) => false;
    public bool CurrentlyUsableForBills() => true;
    public bool UsableForBillsAfterFueling() => true;
    public void Notify_BillDeleted(Bill bill) { }
    public void UsedThisTick() { }

    private class MemoryModData() : IExposable
    {
        public string label;
        public int count;
        public int maxCount;

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, nameof(label));
            Scribe_Values.Look(ref count, nameof(count));
            Scribe_Values.Look(ref maxCount, nameof(maxCount));
        }

        public override string ToString()
            => $"{label} x{count}" + (maxCount == -1 ? "" : $" (max {maxCount})");
    }

}