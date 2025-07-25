using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_GE;

public class JobDriver_CarryToBiocoder : JobDriver
{
    protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;
    protected Building_Biocoder Biocoder => (Building_Biocoder)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed))
            return pawn.Reserve(Biocoder, job, 1, -1, null, errorOnFailed);

        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOnAggroMentalState(TargetIndex.A);

        this.FailOn(() => !Biocoder.Accepts(Takee));

        Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOn(() => Biocoder.GetDirectlyHeldThings().Count > 0)
            .FailOn(() => !pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly))
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

        Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);

        Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));

        yield return goToTakee;

        yield return startCarryingTakee;

        yield return goToThing;

        Toil toil = Toils_General.Wait(500, TargetIndex.B);
        toil.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
        toil.WithProgressBarToilDelay(TargetIndex.B);

        yield return toil;

        Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
        toil2.initAction = delegate
        {
            Biocoder.TryAcceptThing(Takee);
        };
        toil2.defaultCompleteMode = ToilCompleteMode.Instant;

        yield return toil2;
    }
}