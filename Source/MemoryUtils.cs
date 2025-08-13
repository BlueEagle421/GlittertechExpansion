using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using USH_GE;
using Verse;

public static class MemoryUtils
{
    public static AcceptanceReport CanHaveMemoryExtract(this Pawn p)
    {
        if (p.needs.mood == null)
            return "USH_GE_NoEmotion".Translate();

        if (p.needs.mood.thoughts == null)
            return "USH_GE_NoMemories".Translate();

        if (p.needs.mood.thoughts.memories.Memories.NullOrEmpty())
            return "USH_GE_NoMemories".Translate();

        if (p.health.hediffSet.HasHediff(USH_DefOf.USH_PostMemoryExtraction))
            return "USH_GE_RecentExtraction".Translate();

        return true;
    }

    public static Thought GetThoughtForExtraction(this Pawn p)
    {
        Log.Message(p.Label);

        List<Thought> moodThoughts = [];
        p.needs.mood.thoughts.GetAllMoodThoughts(moodThoughts);


        return p.needs.mood.thoughts.memories.Memories.GetMostMoodEffecting();
    }

    public static Thought GetMostMoodEffecting(this List<Thought_Memory> memories)
    {
        if (memories.NullOrEmpty())
            return null;

        return memories
            .Where(t => t.MoodOffset() != 0)
            .OrderByDescending(t => Mathf.Abs(t.MoodOffset()))
            .ThenByDescending(t => t.MoodOffset())
            .FirstOrDefault();
    }

    public static bool IsPositive(this Thought thought) => thought.MoodOffset() > 0f;
    public static bool IsPositive(this MemoryCellData cellData) => cellData.moodOffset > 0f;

    public static MemoryCellData ToCellData(this Thought thought)
    {
        var result = new MemoryCellData()
        {
            label = thought.LabelCap,
            description = thought.Description,
            moodOffset = (int)thought.MoodOffset(),
            sourcePawnLabel = thought.pawn.LabelCap,
            thoughtDef = thought.def,
        };

        return result;
    }

    public static void CreateNewMemoryCell(Map map, List<IntVec3> cells, Thought thought)
    {
        ThingDef thingDef = USH_DefOf.USH_MemoryCellPositive;

        if (!thought.IsPositive())
            thingDef = USH_DefOf.USH_MemoryCellNegative;

        if (map == null)
            return;

        var cell = cells.FirstOrDefault(c => c.Walkable(map));
        if (cell == default)
            return;

        var thing = ThingMaker.MakeThing(thingDef);
        thing.stackCount = 1;

        MemoryCell createdCell = (MemoryCell)thing;

        createdCell.MemoryCellData = thought.ToCellData();
        createdCell.ExpireTicksLeft = thought.DurationTicks * 5;

        GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
    }

    public static void CreatePostExtractionHediff(Pawn p, Thought_Memory memory)
    {
        int targetTickDuration = memory.DurationTicks - memory.age;

        HediffDef def = USH_DefOf.USH_PostMemoryExtraction;

        Hediff created = p.health.AddHediff(def);
        created.TryGetComp<HediffComp_Disappears>().SetDuration(targetTickDuration);
    }

    public static IEnumerable<MemoryMoodMultiplier> PawnMoodMultipliers(Pawn p, MemoryCellData cellData)
    {
        yield return new()
        {
            desc = StatDefOf.PsychicSensitivity.LabelCap,
            value = p.GetStatValue(StatDefOf.PsychicSensitivity)
        };

        if (cellData.IsPositive())
        {
            yield return new()
            {
                desc = "USH_GE_PositiveMem".Translate(),
                value = GE_Mod.Settings.PositiveMoodMultiplier.Value
            };

            yield break;
        }

        string msg = ThoughtUtility.ThoughtNullifiedMessage(p, cellData.thoughtDef);
        if (msg != "")
            yield return new()
            {
                desc = msg,
                value = -GE_Mod.Settings.NegativeMoodMultiplier.Value
            };
    }

    public static string FormatMoodMultipliers(IEnumerable<MemoryMoodMultiplier> multipliers)
    {
        StringBuilder sb = new();

        foreach (var entry in multipliers)
            sb.AppendLine($"  - {entry.desc}: {entry.value.ToStringPercent()}");

        return sb.ToString();
    }

    private static bool CanEnjoyNegativeMemory(Pawn p, MemoryCellData cellData)
        => p.story.traits.IsThoughtDisallowed(cellData.thoughtDef);

    public static Color GetThoughtColor(bool positive)
        => positive ? NeedsCardUtility.MoodColor : NeedsCardUtility.MoodColorNegative;

    public static bool TryGetIMemoryCellHolder(this Thing thing, out IMemoryCellHolder memoryCellHolder)
    {
        memoryCellHolder = null;

        if (thing is ThingWithComps thingWithComps && thingWithComps.AllComps
            .Find(x => x is IMemoryCellHolder cellHolder) is IMemoryCellHolder fromComp)
        {
            memoryCellHolder = fromComp;
            return true;
        }

        if (thing is Pawn pawn && pawn.health?.hediffSet?.hediffs?
            .Find(x => x is IMemoryCellHolder cellHolder) is IMemoryCellHolder fromHediff)
        {
            memoryCellHolder = fromHediff;
            return true;
        }

        return false;
    }
}