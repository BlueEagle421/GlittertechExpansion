using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace USH_GE;

public class Thought_ClonedMemory : Thought_Situational
{
    private Hediff_MemoryProjector _cachedRelevantHediff;
    private Hediff_MemoryProjector RelevantHediff
    {
        get
        {
            _cachedRelevantHediff ??= pawn.health?.hediffSet?
                .GetFirstHediffOfDef(USH_DefOf.USH_InstalledMemoryProjector) as Hediff_MemoryProjector;

            return _cachedRelevantHediff;
        }
    }

    private MemoryCellData MemoryCellData => RelevantHediff.ContainedCell.MemoryCellData;

    private IEnumerable<MemoryMoodMultiplier> AllMultipliers
        => MemoryUtils.PawnMoodMultipliers(pawn, MemoryCellData);

    public override float MoodOffset()
    {
        if (RelevantHediff.ContainedCell == null)
            return 0;

        float m = AllMultipliers.Aggregate(1f, (acc, m) => acc * m.value);
        return MemoryCellData.moodOffset * m;
    }

    public override string Description
    {
        get
        {
            StringBuilder sb = new();

            sb.AppendLine(base.Description);

            sb.AppendLine();
            sb.AppendLine("USH_GE_MoodMultipliers".Translate() + ":");
            sb.AppendLine(MemoryUtils.FormatMoodMultipliers(AllMultipliers));

            sb.AppendLine(MemoryCellData.GetInspectString());

            return sb.ToString().Trim();
        }
    }

    public override bool VisibleInNeedsTab => RelevantHediff.ContainedCell != null;
}