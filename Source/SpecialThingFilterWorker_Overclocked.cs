using Verse;

namespace USH_GE;

public class SpecialThingFilterWorker_Overclocked : SpecialThingFilterWorker
{
    public override bool Matches(Thing t)
    {
        if (!t.TryGetComp(out CompOverclock compOverclock))
            return false;

        return compOverclock.IsOverclocked;
    }
}
