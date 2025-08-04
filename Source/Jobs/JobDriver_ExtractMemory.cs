using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_GE;

public class JobDriver_CloneMemory : JobDriver
{
    private Pawn PawnToSampleFrom => job.GetTarget(TargetIndex.A).Pawn;
    private Thing MemoryCell => job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(PawnToSampleFrom, job))
            return pawn.Reserve(MemoryCell, job);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);

        yield return Toils_Haul.StartCarryThing(TargetIndex.B);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);

        Toil toil = Toils_General.Wait(120);
        toil.WithProgressBarToilDelay(TargetIndex.A);
        toil.FailOnDespawnedOrNull(TargetIndex.A);
        toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

        yield return toil;

        yield return Toils_General.Do(ExtractMemory);
    }

    private void ExtractMemory()
    {
        Thought thought = PawnToSampleFrom.GetThoughtForExtraction();

        if (thought == null)
        {
            Messages.Message("USH_NoDisease".Translate(PawnToSampleFrom.Named("PAWN")), PawnToSampleFrom, MessageTypeDefOf.NeutralEvent);
            return;
        }

        USH_DefOf.USH_ExtractMemory.PlayOneShot(MemoryCell);

        MemoryUtils.CreateNewMemoryCell(PawnToSampleFrom.Map, [.. PawnToSampleFrom.CellsAdjacent8WayAndInside()], thought);
        MemoryCell.SplitOff(1).Destroy(DestroyMode.Vanish);

        MemoryUtils.CreatePostExtractionHediff(PawnToSampleFrom, thought as Thought_Memory);
    }
}
