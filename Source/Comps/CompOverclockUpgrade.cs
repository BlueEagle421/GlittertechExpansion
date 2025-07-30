using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_GE;

public class CompProperties_OverclockUpgrade : CompProperties
{
    public string upgradeLabel;
    public bool preventsIncidents;
    public List<StatModifier> statFactors;
    public List<StatModifier> statOffsets;
    public List<StatModifierQuality> statFactorsQuality = [];
    public List<StatModifierQuality> statOffsetsQuality = [];
    public CompProperties_OverclockUpgrade() => compClass = typeof(CompOverclockUpgrade);
}

public class CompOverclockUpgrade : ThingComp
{
    public CompProperties_OverclockUpgrade Props => (CompProperties_OverclockUpgrade)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
    }
}
