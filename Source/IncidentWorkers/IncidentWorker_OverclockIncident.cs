using System.Linq;
using RimWorld;
using Verse;

namespace USH_GE;

public class IncidentWorker_OverclockIncident : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
        => OverclockIncidentUtility.GetExplosiveGuns((Map)parms.target).Any();

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!OverclockIncidentUtility.GetExplosiveGuns((Map)parms.target).TryRandomElement(out var result))
            return false;

        OverclockIncidentUtility.DoOverclockIncident(result.Item1, result.Item2);
        return true;
    }
}