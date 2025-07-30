using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_GE;

public class CompProperties_OverclockUpgrade : CompProperties
{
    public List<StatModifier> statFactors;
    public List<StatModifier> statOffsets;
    public CompProperties_OverclockUpgrade() => compClass = typeof(CompOverclockUpgrade);

}

public class CompOverclockUpgrade : ThingComp
{
    public CompProperties_OverclockUpgrade Props => (CompProperties_OverclockUpgrade)props;

    private CompQuality _compQuality;

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);

        _compQuality = parent.GetComp<CompQuality>();
    }
}