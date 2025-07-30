using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_GE;

public class Recipe_StabilizeCryogenicNexus : Recipe_Surgery
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (!base.AvailableOnNow(thing, part))
            return false;

        if (thing is not Pawn pawn)
            return false;

        if (!pawn.health.hediffSet.HasHediff(USH_DefOf.USH_InstalledCryogenicNexus))
            return false;

        return true;
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (!pawn.health.hediffSet
            .TryGetHediff(USH_DefOf.USH_InstalledCryogenicNexus, out var hediff))
            return;

        if (hediff is not Hediff_CryogenicNexus nexus)
            return;

        nexus.ResetInstability();

        if (billDoer == null)
            return;

        if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            return;

        TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
        {
            string text = (string)recipe.successfullyRemovedHediffMessage.Formatted(billDoer.LabelShort, pawn.LabelShort);
            Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);
        }

    }
}
