using Verse.AI;

namespace USH_GE;

public class MentalState_ConfusedWandering : MentalState_WanderPsychotic
{
    public override void PostEnd()
    {
        base.PostEnd();

        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(USH_DefOf.USH_Enlightenment);
    }
}
