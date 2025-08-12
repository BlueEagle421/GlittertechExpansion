using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_GE;

public class JobDriver_InsertMemoryCell : JobDriver
{
    private MemoryCell MemoryCell => job.GetTarget(TargetIndex.A).Thing as MemoryCell;
    private Thing TargetHolder => job.GetTarget(TargetIndex.B).Thing;
    private bool BlockInsert => !TargetCellHolder.CanInsertCell(MemoryCell);
    private const int INSERT_TICKS = 100;

    private IMemoryCellHolder TargetCellHolder
    {
        get
        {
            TargetHolder.TryGetIMemoryCellHolder(out var result);
            return result;
        }
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        bool successfullyReservedItem = pawn.Reserve(MemoryCell, job);
        bool successfullyReservedHolder = pawn.Reserve(TargetHolder, job);

        return successfullyReservedItem && successfullyReservedHolder;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.OnCell).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOn(() => BlockInsert);

        yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true, false, true).FailOn(() => BlockInsert);

        yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C, PathEndMode.ClosestTouch).FailOn(() => BlockInsert);

        Toil insertToil = Toils_General.Wait(INSERT_TICKS, TargetIndex.B);
        insertToil.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
        insertToil.FailOn(() => BlockInsert);
        insertToil.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        insertToil.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
        insertToil.handlingFacing = true;

        yield return insertToil;

        void OnDeposited()
        {
            MemoryCell.def.soundDrop.PlayOneShot(pawn);
            TargetCellHolder.Notify_CellInserted(MemoryCell, pawn);
        }

        yield return DepositHauledThingInContainer(TargetCellHolder, OnDeposited);
    }

    public static Toil DepositHauledThingInContainer(
        IMemoryCellHolder containerHolder,
        Action onDeposited = null)
    {
        Toil toil = ToilMaker.MakeToil("DepositHauledThingInContainer");
        toil.initAction = delegate
        {
            Pawn actor = toil.actor;
            if (actor == null)
                return;

            Job curJob = actor.jobs?.curJob;
            if (curJob == null)
                return;

            Thing carriedThing = actor.carryTracker?.CarriedThing;
            if (carriedThing == null)
                return;

            if (carriedThing is not MemoryCell memoryCell)
                return;

            if (containerHolder == null)
                return;

            Thing containerThing = containerHolder.SourceThing;

            if (containerThing == null)
                return;

            var acceptance = containerHolder.CanInsertCell(memoryCell);
            if (!acceptance.Accepted)
            {
                Log.Warning($"MemoryCell {memoryCell} not accepted by container holder: {acceptance.Reason}");
                return;
            }

            ThingOwner thingOwner = containerHolder.MemoryCellOwner;
            if (thingOwner != null)
            {
                int transferred = actor.carryTracker.innerContainer.TryTransferToContainer(memoryCell, thingOwner, 1);

                if (transferred != 0)
                {
                    NotifyOnDeposited(containerThing, actor, memoryCell, transferred, curJob);

                    containerHolder.Notify_CellInserted(memoryCell, actor);

                    onDeposited?.Invoke();
                }

                return;
            }


            if (containerThing.def?.Minifiable == true)
            {
                actor.carryTracker.innerContainer.ClearAndDestroyContents();
                return;
            }
        };
        return toil;
    }

    private static void NotifyOnDeposited(Thing containerThing, Pawn actor, Thing carriedThing, int transferredCount, Job curJob)
    {
        if (containerThing == null || actor == null || carriedThing == null || transferredCount <= 0)
            return;

        if (containerThing is IHaulEnroute containerEnroute)
        {
            try
            {
                containerThing.Map?.enrouteManager?.ReleaseFor(containerEnroute, actor);
            }
            catch (Exception ex)
            {
                Log.Error($"Error releasing enroute reservation for {containerThing}: {ex}");
            }
        }

        if (containerThing is INotifyHauledTo notifyHauledTo)
        {
            try
            {
                notifyHauledTo.Notify_HauledTo(actor, carriedThing, transferredCount);
            }
            catch (Exception ex)
            {
                Log.Error($"Error calling Notify_HauledTo on {containerThing}: {ex}");
            }
        }

        if (containerThing is ThingWithComps thingWithComps)
        {
            foreach (ThingComp comp in thingWithComps.AllComps)
            {
                if (comp is INotifyHauledTo compNotify)
                {
                    try
                    {
                        compNotify.Notify_HauledTo(actor, carriedThing, transferredCount);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error calling Notify_HauledTo on comp {comp} of {containerThing}: {ex}");
                    }
                }
            }
        }

        if (curJob?.def == JobDefOf.DoBill)
        {
            try
            {
                HaulAIUtility.UpdateJobWithPlacedThings(curJob, carriedThing, transferredCount);
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating job with placed things for job {curJob}: {ex}");
            }
        }
    }
}
