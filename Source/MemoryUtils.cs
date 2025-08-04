using System.Collections.Generic;
using System.Linq;
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
        thing.TryGetComp<CompMemoryCell>().MemoryCellData = thought.ToCellData();

        GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
    }

    public static void CreatePostExtractionHediff(Pawn p, Thought_Memory memory)
    {
        int targetTickDuration = memory.DurationTicks - memory.age;

        HediffDef def = USH_DefOf.USH_PostMemoryExtraction;

        Hediff created = p.health.AddHediff(def);
        created.TryGetComp<HediffComp_Disappears>().SetDuration(targetTickDuration);
    }


    public static float MoodOffsetForClonedMemory(Pawn p, MemoryCellData cellData)
    {
        if (cellData.IsPositive())
            return cellData.moodOffset * GE_Mod.Settings.PositiveMoodMultiplier.Value;

        if (CanEnjoyNegativeMemory(p, cellData))
            return -cellData.moodOffset * GE_Mod.Settings.NegativeMoodMultiplier.Value;

        return cellData.moodOffset;
    }

    private static bool CanEnjoyNegativeMemory(Pawn p, MemoryCellData cellData)
    {
        return ThoughtUtility.ThoughtNullified(p, cellData.thoughtDef);
    }

    public static Color GetThoughtColor(bool positive)
    {
        return positive ? NeedsCardUtility.MoodColor : NeedsCardUtility.MoodColorNegative;
    }
}