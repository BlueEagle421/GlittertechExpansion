using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

public class Thought_MemoryPylon : Thought_Memory
{
    private CompMemoryPylon _sourceCompPylon;

    public CompMemoryPylon SourceCompPylon
    {
        get
        {
            _sourceCompPylon ??= SourceThing.TryGetComp<CompMemoryPylon>();
            return _sourceCompPylon;
        }
    }

    public ThingWithComps SourceThing;
    public MemoryCellData MemoryCellData;
    public override string Description
    {
        get
        {
            StringBuilder sb = new();

            sb.AppendLine(def.stages[0].description);

            sb.AppendLine();
            sb.AppendLine("USH_GE_MoodMultipliers".Translate() + ":");
            sb.AppendLine(MemoryUtils.FormatMoodMultipliers(AllMultipliers));

            sb.AppendLine(MemoryCellData.GetInspectString());

            return sb.ToString().Trim();
        }
    }

    private IEnumerable<MemoryMoodMultiplier> AllMultipliers
    {
        get
        {
            foreach (var entry in MemoryUtils.PawnMoodMultipliers(pawn, MemoryCellData))
                yield return entry;

            yield return new()
            {
                desc = "USH_GE_MemPylon".Translate(),
                value = GE_Mod.Settings.PylonMoodMultiplier.Value,
            };
        }
    }

    public override float MoodOffset()
    {
        float m = AllMultipliers.Aggregate(1f, (acc, m) => acc * m.value);
        return MemoryCellData.moodOffset * m;
    }
    public override bool TryMergeWithExistingMemory(out bool showBubble)
    {
        showBubble = false;
        return false;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref MemoryCellData, nameof(MemoryCellData));
        Scribe_References.Look(ref SourceThing, nameof(SourceThing));
    }
}