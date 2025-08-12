using RimWorld;
using Verse;

namespace USH_GE;

public class Thought_ClonedMemory : Thought_Situational
{
    private MemoryCellData? _cachedMemoryCellData;
    private MemoryCellData? MemoryCellData
    {
        get
        {
            if (_cachedMemoryCellData == null)
            {
                Hediff relevantHediff = pawn.health.hediffSet.GetFirstHediffOfDef(USH_DefOf.USH_InstalledMemoryProjector);
                _cachedMemoryCellData = (relevantHediff as Hediff_MemoryProjector).ContainedCell.MemoryCellData;
            }

            return _cachedMemoryCellData;
        }
    }

    private float? _cachedClonedMoodOffset;
    private float? ClonedMoodOffset
    {
        get
        {
            _cachedClonedMoodOffset ??= MemoryUtils.MoodOffsetForClonedMemory(pawn, MemoryCellData.Value);
            return _cachedClonedMoodOffset;
        }
    }

    public override float MoodOffset() => ClonedMoodOffset.Value;
    public override string Description => base.Description + "\n\n" + MemoryCellData.Value.GetInspectString();
}