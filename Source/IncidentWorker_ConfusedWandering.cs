using System.Linq;
using RimWorld;
using Verse;

namespace USH_GE;

public class IncidentWorker_ConfusedWandering : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
        => ConfusionIncidentUtility.GetResearchers((Map)parms.target).Any();

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!ConfusionIncidentUtility.GetResearchers((Map)parms.target).TryRandomElement(out var result))
            return false;

        ConfusionIncidentUtility.DoConfusionIncident(result);
        return true;
    }
}