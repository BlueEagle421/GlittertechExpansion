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

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("USH_GE_InsertMemoryCell".Translate(clickedThing.Label), delegate
            {
                CreateInsertJobTargeter(context.FirstSelectedPawn, clickedThing);
            }), context.FirstSelectedPawn, new LocalTargetInfo(clickedThing));

    }

    private static void CreateInsertJobTargeter(Pawn p, Thing item)
    {
        Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo target)
        {
            GiveJobToPawn(p, target, item);
        }, null, null, null, null, null, playSoundOnAction: true, delegate (LocalTargetInfo target)
        {
            if (target.Thing.TryGetComp(out CompOverclock compOverclock))
            {
                var report = compOverclock.CanInsert();
                if (!report.Accepted)
                {
                    var msg = $"{"USH_GE_CannotInsert".Translate()}: {report.Reason.CapitalizeFirst()}"
                              .Colorize(ColorLibrary.RedReadable);

                    Widgets.MouseAttachedLabel(msg);
                    return;
                }
            }

            Widgets.MouseAttachedLabel("USH_GE_CommandChooseContainer".Translate());
        });
    }

    private static void GiveJobToPawn(Pawn p, LocalTargetInfo target, Thing item)
    {
        Job job = JobMaker.MakeJob(USH_DefOf.USH_InsertOverclockUpgrade, item, target.Thing, target.Thing.Position);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
    }

}