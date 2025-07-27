using RimWorld;
using Verse;
using Verse.AI;

namespace USH_GE;

public sealed class FloatMenuOptionProvider_CarryToBiocoder : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn targetPawn, FloatMenuContext context)
    {
        if (targetPawn.IsPlayerControlled && !targetPawn.Downed)
            return null;

        if (!CanReserveAndReachTarget(context.FirstSelectedPawn, targetPawn))
            return null;

        if (!TryGetBiocoder(targetPawn, context.FirstSelectedPawn, out var biocoder))
            return null;

        string label = "USH_GE_CarryToBiocoder".Translate(targetPawn.LabelShort);
        if (TryGetRestrictedOption(targetPawn, context.FirstSelectedPawn, label, out var restrictedOption))
            return restrictedOption;

        return CreateCarryOption(targetPawn, context, biocoder, label);
    }

    private static bool CanReserveAndReachTarget(Pawn carrier, Pawn target)
    {
        return carrier.CanReserveAndReach(
            target,
            PathEndMode.OnCell,
            Danger.Deadly,
            maxPawns: 1,
            stackCount: -1,
            ignoreOtherReservations: true);
    }

    private static bool TryGetBiocoder(Pawn target, Pawn carrier, out Building_Biocoder biocoder)
    {
        biocoder = Building_Biocoder.FindBiocoderFor(target, carrier, ignoreOtherReservations: true)
                   ?? Building_Biocoder.FindBiocoderFor(target, carrier);

        return biocoder != null;
    }

    private static bool TryGetRestrictedOption(
        Pawn target,
        Pawn carrier,
        string baseLabel,
        out FloatMenuOption option)
    {
        option = null;

        if (target.IsQuestLodger())
        {
            string message = "BiocoderGuestsNotAllowed".Translate();
            option = CreateDisabledOption(baseLabel, message, target, carrier);
            return true;
        }

        if (target.GetExtraHostFaction() != null)
        {
            string message = "BiocoderGuestPrisonersNotAllowed".Translate();
            option = CreateDisabledOption(baseLabel, message, target, carrier);
            return true;
        }

        return false;
    }

    private static FloatMenuOption CreateDisabledOption(
        string baseLabel,
        string restrictionReason,
        Pawn target,
        Pawn carrier)
    {
        string label = $"{baseLabel} ({restrictionReason})";
        var option = new FloatMenuOption(label, null, MenuOptionPriority.Default, null, target);
        return FloatMenuUtility.DecoratePrioritizedTask(option, carrier, target);
    }

    private static FloatMenuOption CreateCarryOption(
        Pawn targetPawn,
        FloatMenuContext context,
        Building_Biocoder biocoder,
        string label)
    {
        void Action()
        {
            if (!TryGetBiocoder(targetPawn, context.FirstSelectedPawn, out biocoder))
            {
                Messages.Message(
                    "USH_GE_CannotCarryToBiocoder".Translate() + ": " + "NoBiocoder".Translate(),
                    targetPawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
                return;
            }

            var job = JobMaker.MakeJob(USH_DefOf.USH_CarryToBiocoder, targetPawn, biocoder);
            job.count = 1;
            context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        var option = new FloatMenuOption(label, Action, MenuOptionPriority.Default, null, targetPawn);
        return FloatMenuUtility.DecoratePrioritizedTask(option, context.FirstSelectedPawn, targetPawn);
    }
}

