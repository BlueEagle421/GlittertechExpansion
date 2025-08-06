using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class CompProperties_OverclockUpgrade : CompProperties
{
    public string upgradeLabel;
    public bool preventsIncidents;
    public float burstShotSpeedMultiplier = 1f;
    public List<StatModifier> statFactors;
    public List<StatModifier> statOffsets;
    public CompProperties_OverclockUpgrade() => compClass = typeof(CompOverclockUpgrade);
}

public class CompOverclockUpgrade : ThingComp
{
    public CompProperties_OverclockUpgrade Props => (CompProperties_OverclockUpgrade)props;

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        StatCategoryDef categoryDef = StatCategoryDefOf.EquippedStatOffsets;

        if (!Mathf.Approximately(Props.burstShotSpeedMultiplier, 1f))
        {
            string content = Props.burstShotSpeedMultiplier.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor);
            yield return new StatDrawEntry(categoryDef, "USH_GE__BurstShotSpeedMultiplier".Translate(), content, "USH_GE__BurstShotSpeedMultiplierDesc".Translate(), 1104);
        }

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
