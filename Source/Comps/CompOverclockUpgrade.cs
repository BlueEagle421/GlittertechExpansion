using System.Collections.Generic;
using System.Text;
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

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("Stat_ThingUniqueWeaponTrait_Desc".Translate());
        stringBuilder.AppendLine();
        List<StatModifier> allMods = [];

        StatCategoryDef categoryDef = StatCategoryDefOf.EquippedStatOffsets;

        if (!Props.statOffsets.NullOrEmpty())
            foreach (var mod in Props.statOffsets)
            {
                string content = mod.stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor);
                yield return new StatDrawEntry(categoryDef, mod.stat.LabelCap, content, mod.stat.description, 1104);
            }
        if (!Props.statFactors.NullOrEmpty())
            foreach (var mod in Props.statFactors)
            {
                string content = mod.stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor);
                yield return new StatDrawEntry(categoryDef, mod.stat.LabelCap, content, mod.stat.description, 1104);
            }
    }

}
