using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_GE;

public class OptionProvider_InsertOverclockUpgrade : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    private static readonly TargetingParameters targetingParameters;

    static OptionProvider_InsertOverclockUpgrade()
    {
        targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetItems = true,
            canTargetBuildings = false,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = new Predicate<TargetInfo>(TargetValidator),
        };
    }


    private static bool TargetValidator(TargetInfo target)
    {
        if (target.Thing is not ThingWithComps thing)
            return false;

        if (!thing.TryGetComp(out CompEquippable _))
            return false;

        return true;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!clickedThing.TryGetComp(out CompOverclockUpgrade compOverclockUpgrade))
            yield break;

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
            "USH_GE_InstallUpgrade".Translate(clickedThing.Label), delegate
            {
                CreateInsertJobTargeter(context.FirstSelectedPawn, clickedThing);
            }), context.FirstSelectedPawn, new LocalTargetInfo(clickedThing));
    }

    private static void CreateInsertJobTargeter(Pawn p, Thing item)
    {
        Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo target)
        {
            GiveJobToPawn(p, target, item);
        }, null, null, null, null, null, playSoundOnAction: true, OnGuiAction);
    }

    private static void OnGuiAction(LocalTargetInfo target)
    {
        if (target == null)
        {
            Widgets.MouseAttachedLabel("USH_GE_CommandChooseFirearm".Translate());
            return;
        }

        if (!target.Thing.TryGetComp(out CompOverclock compOverclock))
        {
            var msg = $"{"USH_GE_CannotInstall".Translate()}: {"USH_GE_NotOverclockable".Translate()}"
                .Colorize(ColorLibrary.RedReadable);

            Widgets.MouseAttachedLabel(msg);
            return;
        }

        var report = compOverclock.CanInstall();
        if (!report.Accepted)
        {
            var msg = $"{"USH_GE_CannotInstall".Translate()}: {report.Reason.CapitalizeFirst()}"
                      .Colorize(ColorLibrary.RedReadable);

            Widgets.MouseAttachedLabel(msg);
        }
    }

    private static void GiveJobToPawn(Pawn p, LocalTargetInfo target, Thing item)
    {
        if (!target.Thing.TryGetComp(out CompOverclock compOverclock))
            return;

        var report = compOverclock.CanInstall();
        if (!report.Accepted)
            return;

        Job job = JobMaker.MakeJob(USH_DefOf.USH_InstallOverclockUpgrade, item, target.Thing, target.Thing.Position);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
    }

}