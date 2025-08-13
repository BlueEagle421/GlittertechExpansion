using System.Text;
using RimWorld;
using Verse;

namespace USH_GE;

public struct MemoryCellData : IExposable
{
    public string label;
    public string description;
    public string sourcePawnLabel;
    public int moodOffset;
    public ThoughtDef thoughtDef;

    public MemoryCellData() { }

    public readonly string GetInspectString()
    {
        StringBuilder sb = new();

        sb.AppendLine("Cloned from: " + sourcePawnLabel);

        string moodFormatted = moodOffset.ToString()
            .Colorize(MemoryUtils.GetThoughtColor(moodOffset > 0));

        sb.AppendLine($"{label} ({moodFormatted})");
        sb.AppendLine("Description: " + description);

        return sb.ToString().Trim();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref label, nameof(label));
        Scribe_Values.Look(ref description, nameof(description));
        Scribe_Values.Look(ref moodOffset, nameof(moodOffset));
        Scribe_Values.Look(ref sourcePawnLabel, nameof(sourcePawnLabel));
        Scribe_Defs.Look(ref thoughtDef, nameof(thoughtDef));
    }
}

public struct MemoryMoodMultiplier
{
    public string desc;
    public float value;
}