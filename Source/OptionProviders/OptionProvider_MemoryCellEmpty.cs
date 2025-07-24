using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_GE;

public class FloatMenuOptionProvider_MemoryCellEmpty : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    private static readonly TargetingParameters targetingParameters;

    static FloatMenuOptionProvider_MemoryCellEmpty()
    {
        targetingParameters = new TargetingParameters
        {
            canTargetPawns = true,
            canTargetItems = false,
            canTargetBuildings = false,
        };
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing.def != USH_DefOf.USH_MemoryCellEmpty)
            yield break;

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("USH_GE_UseMemoryCellEmpty".Translate(clickedThing.Label), delegate
            {
                CreateUseJobTargeter(context.FirstSelectedPawn, clickedThing);
            }), context.FirstSelectedPawn, new LocalTargetInfo(clickedThing));
    }

    private static void CreateUseJobTargeter(Pawn p, Thing item)
    {
        Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo target)
        {
            GiveJobToPawn(p, target, item);
        }, null, null, null, null, null, playSoundOnAction: true, delegate (LocalTargetInfo target)
        {
            if (target.Pawn is Pawn pTarget)
            {
                var report = pTarget.CanHaveMemoryExtract();
                var msg = "";
                if (!report.Accepted)
                    msg = $"{"USH_GE_CannotExtract".Translate()}: {report.Reason.CapitalizeFirst()}"
                       .Colorize(ColorLibrary.RedReadable);
                else
                    msg = $"{"USH_GE_WillExtract".Translate()}: {pTarget.GetThoughtForExtraction().LabelCap}"
                       .Colorize(ColorLibrary.Cyan);

                Widgets.MouseAttachedLabel(msg);
                return;
            }

            Widgets.MouseAttachedLabel("USH_GE_CommandChoosePawnToExtract".Translate());
        });
    }

    private static void GiveJobToPawn(Pawn p, LocalTargetInfo target, Thing item)
    {
        Pawn pTarget = target.Pawn;

        Job job = JobMaker.MakeJob(USH_DefOf.USH_CloneMemory, pTarget, item);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job);
    }
}