using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_GE;

public class JobDriver_InsertOverclockUpgrade : JobDriver
{
    private Thing TargetItem => job.GetTarget(TargetIndex.A).Thing;
    private ThingWithComps TargetGun => job.GetTarget(TargetIndex.B).Thing as ThingWithComps;
    private bool IsFull => TargetGun.GetComp<CompOverclock>().ContainedThing != null;
    private const int INSERT_TICKS = 100;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        bool successfullyReservedItem = pawn.Reserve(TargetItem, job);
        bool successfullyReservedBuilding = pawn.Reserve(TargetGun, job);

        return successfullyReservedItem && successfullyReservedBuilding;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {

        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.OnCell).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOn(() => IsFull);

        yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false, true).FailOn(() => IsFull);

        yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C, PathEndMode.ClosestTouch).FailOn(() => IsFull);

        Toil insertToil = Toils_General.Wait(INSERT_TICKS, TargetIndex.B);
        insertToil.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
        insertToil.FailOn(() => IsFull);
        insertToil.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        insertToil.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
        insertToil.handlingFacing = true;

        yield return insertToil;

        void OnDeposited()
        {
            TargetItem.def.soundDrop.PlayOneShot(pawn);
            TargetGun.GetComp<CompOverclock>().Notify_UpgradeInserted(pawn);
        }

        yield return Toils_Haul.DepositHauledThingInContainer(TargetIndex.B, TargetIndex.A, OnDeposited);
    }
}
