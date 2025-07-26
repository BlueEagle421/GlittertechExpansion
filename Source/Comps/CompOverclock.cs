using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class CompProperties_Overclock : CompProperties
{
    public List<StatModifier> statFactors;
    public List<StatModifier> statOffsets;
    public CompProperties_Overclock() => compClass = typeof(CompOverclock);
}

public class CompOverclock : ThingComp
{
    public bool IsOverclocked;
    public CompProperties_Overclock Props => (CompProperties_Overclock)props;

    public override string TransformLabel(string label)
    {
        if (!IsOverclocked)
            return base.TransformLabel(label);

        return $"{"USH_GE_Overclocked".Translate()} {label.UncapitalizeFirst()}";
    }

    public override float GetStatFactor(StatDef stat)
    {
        if (!IsOverclocked)
            return 1;

        return 1f * Props.statFactors.GetStatFactorFromList(stat);
    }

    public override float GetStatOffset(StatDef stat)
    {
        if (!IsOverclocked)
            return 0;

        return Props.statOffsets.GetStatOffsetFromList(stat); ;
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string indent = "")
    {
        if (!IsOverclocked)
            return;

        StringBuilder overclockBuilder = new();

        foreach (var mod in Props.statOffsets)
            if (mod.stat == stat && !Mathf.Approximately(mod.value, 0f))
                overclockBuilder.AppendLine($"{indent}{"USH_GE_Overclock".Translate()}: {stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Offset)}");

        foreach (var mod in Props.statFactors)
            if (mod.stat == stat && !Mathf.Approximately(mod.value, 1f))
                overclockBuilder.AppendLine($"{indent}{"USH_GE_Overclock".Translate()}: {stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor)}");

        if (overclockBuilder.Length > 0)
            sb.Append(overclockBuilder.ToString());
    }


    public override string GetDescriptionPart()
    {
        StringBuilder sb = new();
        sb.AppendLine("USH_GE_OverclockDesc".Translate());

        foreach (var mod in Props.statOffsets)
            if (!Mathf.Approximately(mod.value, 0f))
                sb.AppendLine($"    {mod.stat.LabelCap}: {mod.stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Offset)}");

        foreach (var mod in Props.statFactors)
            if (!Mathf.Approximately(mod.value, 1f))
                sb.AppendLine($"    {mod.stat.LabelCap}: {mod.stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor)}");

        return sb.ToString();
    }
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (!DebugSettings.ShowDevGizmos)
            yield break;

        yield return new Command_Action
        {
            action = () => IsOverclocked = !IsOverclocked,
            defaultLabel = "Toggle overclock"
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref IsOverclocked, "IsOverclocked");
    }
}