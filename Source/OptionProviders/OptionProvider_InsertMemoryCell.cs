using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_GE;

public class FloatMenuOptionProvider_InsertMemoryCell : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    private static readonly TargetingParameters targetingParameters;

    static FloatMenuOptionProvider_InsertMemoryCell()
    {
        targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetItems = false,
            canTargetBuildings = true,
            validator = new Predicate<TargetInfo>(TargetValidator)
        };
    }


    private static bool TargetValidator(TargetInfo target)
    {
        if (target.Thing is not Building building)
            return false;

        if (building.TryGetComp<CompMemoryCellContainer>() == null)
            return false;

        return true;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!clickedThing.TryGetComp(out CompMemoryCell memoryCell))
            yield break;

        CompMemoryCellContainer containerComp =
        CompMemoryCellContainer.FindFor(memoryCell, context.FirstSelectedPawn);

        if (containerComp == null)
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
            if (target.Thing.TryGetComp(out CompMemoryCellContainer container))
            {
                var report = container.CanInsert();
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
        Building targetBuilding = target.Thing as Building;

        Job job = JobMaker.MakeJob(USH_DefOf.USH_InsertMemoryCell, item, targetBuilding, targetBuilding.InteractionCell);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
    }

}