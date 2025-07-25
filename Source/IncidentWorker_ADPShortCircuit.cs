using System.Linq;
using RimWorld;
using Verse;

namespace USH_GE;

public class IncidentWorker_ADPShortCircuit : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
        => ADPShortCircuitUtility.GetShortCircuitableProbes((Map)parms.target).Any();

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!ADPShortCircuitUtility.GetShortCircuitableProbes((Map)parms.target).TryRandomElement(out var result))
            return false;

        ADPShortCircuitUtility.DoADPShortCircuit(result);
        return true;
    }
}
