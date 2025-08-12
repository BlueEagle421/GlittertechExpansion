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
            canTargetPawns = true,
            canTargetItems = false,
            canTargetBuildings = true,
            validator = new Predicate<TargetInfo>(TargetValidator)
        };
    }

    private static bool TargetValidator(TargetInfo target)
    {
        if (!target.Thing.TryGetIMemoryCellHolder(out _))
            return false;

        return true;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not MemoryCell memoryCell)
            yield break;

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("USH_GE_InsertMemoryCell".Translate(memoryCell.Label), delegate
            {
                CreateInsertJobTargeter(context.FirstSelectedPawn, memoryCell);
            }), context.FirstSelectedPawn, new LocalTargetInfo(memoryCell));

    }

    private static void CreateInsertJobTargeter(Pawn p, MemoryCell memoryCell)
    {
        Find.Targeter.BeginTargeting(targetingParameters, delegate (LocalTargetInfo target)
        {
            target.Thing.TryGetIMemoryCellHolder(out IMemoryCellHolder cellHolder);
            GiveJobToPawn(p, cellHolder, memoryCell);

        }, null, null, null, null, null, playSoundOnAction: true, delegate (LocalTargetInfo target)
        {
            if (target.Thing.TryGetIMemoryCellHolder(out IMemoryCellHolder cellHolder))
            {
                var report = cellHolder.CanInsertCell(memoryCell);
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

    private static void GiveJobToPawn(Pawn p, IMemoryCellHolder targetCellHolder, MemoryCell memoryCell)
    {
        Job job = JobMaker.MakeJob(USH_DefOf.USH_InsertMemoryCell, memoryCell, targetCellHolder.SourceThing, targetCellHolder.InsertPos);
        job.count = 1;
        p.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
    }

}