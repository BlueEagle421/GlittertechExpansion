using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_Overclock : CompProperties
{
    public string insertedSoundDefName;
    public List<StatModifier> statFactors = [];
    public List<StatModifier> statOffsets = [];
    public CompProperties_Overclock() => compClass = typeof(CompOverclock);
}

public class CompOverclock : ThingComp, IThingHolder, ISearchableContents
{
    public bool IsOverclocked;
    public CompProperties_Overclock OverclockProps => (CompProperties_Overclock)props;

    private CompOverclockUpgrade _cellComp;
    public CompOverclockUpgrade UpgradeLens => _cellComp;

    protected ThingOwner innerContainer;
    public bool HasAnyContents => innerContainer.Count > 0;
    public bool BlocksSelfIgnite => UpgradeLens != null && UpgradeLens.Props.preventsIncidents;
    public Thing ContainedThing
    {
        get
        {
            if (innerContainer.Count != 0)
                return innerContainer[0];

            return null;
        }
    }

    private SoundDef _insertedSoundDef;

    public CompOverclock()
    {
        innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _cellComp = ContainedThing?.TryGetComp<CompOverclockUpgrade>();
    }

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

        float result = OverclockProps.statFactors.GetStatFactorFromList(stat);

        if (UpgradeLens != null)
            result *= UpgradeLens.Props.statFactors.GetStatFactorFromList(stat);

        return 1f * result;
    }

    public override float GetStatOffset(StatDef stat)
    {
        if (!IsOverclocked)
            return 0;

        float result = OverclockProps.statOffsets.GetStatOffsetFromList(stat);

        if (UpgradeLens != null)
            result += UpgradeLens.Props.statOffsets.GetStatOffsetFromList(stat);

        return result;
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string indent = "")
    {
        if (!IsOverclocked)
            return;

        StringBuilder overclockBuilder = new();

        foreach (var mod in OverclockProps.statOffsets)
            if (mod.stat == stat && !Mathf.Approximately(mod.value, 0f))
                overclockBuilder.AppendLine($"{indent}{"USH_GE_Overclock".Translate()}: {stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Offset)}");

        foreach (var mod in OverclockProps.statFactors)
            if (mod.stat == stat && !Mathf.Approximately(mod.value, 1f))
                overclockBuilder.AppendLine($"{indent}{"USH_GE_Overclock".Translate()}: {stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor)}");

        if (overclockBuilder.Length > 0)
            sb.Append(overclockBuilder.ToString());

        GetUpgradeStatsExplanation(stat, sb, indent);
    }

    private void GetUpgradeStatsExplanation(StatDef stat, StringBuilder sb, string indent = "")
    {
        if (!IsOverclocked)
            return;

        if (UpgradeLens == null)
            return;

        string upgradePrefix = $"{indent}{UpgradeLens.parent.Label.CapitalizeFirst()}";

        StringBuilder upgradeBuilder = new();

        if (!UpgradeLens.Props.statOffsets.NullOrEmpty())
            foreach (var mod in UpgradeLens.Props.statOffsets)
                if (mod.stat == stat && !Mathf.Approximately(mod.value, 0f))
                    upgradeBuilder.AppendLine($"{upgradePrefix}: {stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Offset)}");

        if (!UpgradeLens.Props.statFactors.NullOrEmpty())
            foreach (var mod in UpgradeLens.Props.statFactors)
                if (mod.stat == stat && !Mathf.Approximately(mod.value, 1f))
                    upgradeBuilder.AppendLine($"{upgradePrefix}: {stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor)}");

        if (upgradeBuilder.Length > 0)
            sb.Append(upgradeBuilder.ToString());
    }


    public override string GetDescriptionPart()
    {
        if (!IsOverclocked)
            return string.Empty;

        StringBuilder sb = new();
        sb.AppendLine("USH_GE_OverclockDesc".Translate());

        foreach (var mod in OverclockProps.statOffsets)
            if (!Mathf.Approximately(mod.value, 0f))
                sb.AppendLine($"    {mod.stat.LabelCap}: {mod.stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Offset)}");

        foreach (var mod in OverclockProps.statFactors)
            if (!Mathf.Approximately(mod.value, 1f))
                sb.AppendLine($"    {mod.stat.LabelCap}: {mod.stat.Worker.ValueToString(mod.value, finalized: false, ToStringNumberSense.Factor)}");

        return sb.ToString();
    }
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        Command_Action ejectCommand = new()
        {
            action = EjectContent,
            defaultLabel = "USH_GE_CommandUpgradeEject".Translate(),
            defaultDesc = "USH_GE_CommandUpgradeEjectDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc8,
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectUpgrade")
        };

        if (ContainedThing == null)
            ejectCommand.Disable("USH_GE_CommandUpgradeEjectFailEmpty".Translate());

        if (IsOverclocked)
            yield return ejectCommand;

        if (!DebugSettings.ShowDevGizmos)
            yield break;

        yield return new Command_Action
        {
            action = () => IsOverclocked = !IsOverclocked,
            defaultLabel = "Toggle overclock"
        };
    }

    public void EjectContent()
    {
        USH_DefOf.USH_Eject?.PlayOneShot(SoundInfo.InMap(parent));
        innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
        Notify_UpgradeExtracted(null);
    }

    public void Notify_UpgradeInstalled(Pawn doer)
    {
        _cellComp = ContainedThing?.TryGetComp<CompOverclockUpgrade>();

        _insertedSoundDef = DefDatabase<SoundDef>.GetNamedSilentFail(OverclockProps.insertedSoundDefName);
        _insertedSoundDef?.PlayOneShot(SoundInfo.InMap(parent));
    }

    public void Notify_UpgradeExtracted(Pawn doer)
    {
        _cellComp = null;
    }

    public AcceptanceReport CanInstall()
    {
        if (UpgradeLens != null)
            return "USH_GE_SlotTaken".Translate();

        if (!IsOverclocked)
            return "USH_GE_NotOverclocked".Translate();

        return true;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (!IsOverclocked)
            yield break;

        StatCategoryDef categoryDef = StatCategoryDefOf.Weapon_Ranged;

        string desc = UpgradeLens == null ?
            "USH_GE_SocketEmptyDesc".Translate() : UpgradeLens.parent.def.description;

        yield return new StatDrawEntry(
            categoryDef,
            "USH_GE_UpgradeSlot".Translate(),
            GetUpgradeInspectString(),
            desc,
            1104
        );
    }


    public override string CompInspectStringExtra()
    {
        if (!IsOverclocked)
            return string.Empty;

        return "USH_GE_UpgradeSlot".Translate() + ": " + GetUpgradeInspectString();
    }

    private string GetUpgradeInspectString()
        => UpgradeLens == null ? ((string)"USH_GE_Empty".Translate()) : UpgradeLens.Props.upgradeLabel.CapitalizeFirst();

    public ThingOwner GetDirectlyHeldThings()
        => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
        => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public ThingOwner SearchableContents => innerContainer;

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref IsOverclocked, "IsOverclocked");
    }
}