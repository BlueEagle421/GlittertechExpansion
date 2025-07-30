using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_Overclock : CompProperties_ThingContainer
{
    public SoundDef insertedSoundDef;
    public List<StatModifier> statFactors;
    public List<StatModifier> statOffsets;
    public CompProperties_Overclock() => compClass = typeof(CompOverclock);
}

public class CompOverclock : ThingComp, IThingHolder
{
    public bool IsOverclocked;
    public CompProperties_Overclock OverclockProps => (CompProperties_Overclock)props;

    private CompOverclockUpgrade _cellComp;
    public CompOverclockUpgrade ContainedCellComp => _cellComp;

    protected ThingOwner innerContainer;
    public bool HasAnyContents => innerContainer.Count > 0;
    public Thing ContainedThing
    {
        get
        {
            if (innerContainer.Count != 0)
            {
                return innerContainer[0];
            }
            return null;
        }
    }

    public ThingOwner SearchableContents => innerContainer;

    public CompOverclock()
    {
        innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
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

        return 1f * OverclockProps.statFactors.GetStatFactorFromList(stat);
    }

    public override float GetStatOffset(StatDef stat)
    {
        if (!IsOverclocked)
            return 0;

        return OverclockProps.statOffsets.GetStatOffsetFromList(stat); ;
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
            defaultLabel = "USH_GE_CommandMemoryContainerEject".Translate(),
            defaultDesc = "USH_GE_CommandMemoryContainerEjectDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc8,
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectMemoryCell")
        };

        if (ContainedThing == null)
            ejectCommand.Disable("USH_GE_CommandMemoryContainerEjectFailEmpty".Translate());

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

    public void Notify_UpgradeInserted(Pawn doer)
    {
        _cellComp = ContainedThing?.TryGetComp<CompOverclockUpgrade>();

        SoundDef insertedSoundDef = OverclockProps.insertedSoundDef;
        insertedSoundDef?.PlayOneShot(SoundInfo.InMap(parent));
    }

    public void Notify_UpgradeExtracted(Pawn doer)
    {

    }

    public AcceptanceReport CanInsert()
    {
        if (ContainedCellComp != null)
            return "USH_GE_ContainerFull".Translate(parent.Named("BUILDING"));

        if (!IsOverclocked)
            return "Not overclocked";

        return true;
    }

    public override string CompInspectStringExtra()
    {
        return "Contents".Translate() + ": " + (ContainedCellComp == null ? ((string)"Nothing".Translate()) : ContainedCellComp.parent.LabelCap);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref IsOverclocked, "IsOverclocked");
    }
}