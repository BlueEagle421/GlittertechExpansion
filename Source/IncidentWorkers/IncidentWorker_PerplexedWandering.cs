using System.Linq;
using RimWorld;
using Verse;

namespace USH_GE;

public class IncidentWorker_PerplexWandering : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
        => PerplexIncidentUtility.GetResearchers((Map)parms.target).Any();

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!PerplexIncidentUtility.GetResearchers((Map)parms.target).TryRandomElement(out var result))
            return false;

        PerplexIncidentUtility.DoConfusionIncident(result);
        return true;
    }
}