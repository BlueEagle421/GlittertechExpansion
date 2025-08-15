using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public static class ADPShortCircuitUtility
{
    private static readonly Dictionary<PowerNet, bool> _tmpPowerNetHasActivePowerSource = [];

    public static IEnumerable<Building> GetShortCircuitableProbes(Map map)
    {
        _tmpPowerNetHasActivePowerSource.Clear();
        try
        {
            var probes = map.listerThings.ThingsOfDef(USH_DefOf.USH_ResearchProbe);
            foreach (Thing thing in probes)
            {
                if (thing is not Building building) continue;

                if (building.TryGetComp(out CompPowerTrader powerTrader) && !powerTrader.PowerOn) continue;

                if (building.TryGetComp(out CompFlickable compFlickable) && !compFlickable.SwitchIsOn) continue;

                yield return building;
            }
        }
        finally
        {
            _tmpPowerNetHasActivePowerSource.Clear();
        }
    }

    public static void DoADPShortCircuit(Building culprit)
    {
        PowerNet powerNet = culprit.PowerComp.PowerNet;
        Map map = culprit.Map;

        if (!powerNet.batteryComps.Any(b => b.StoredEnergy > 20f))
            return;

        DrainBatteriesAndCauseExplosion(powerNet, culprit, out float totalEnergy, out float explosionRadius);

        string culpritLabel = Find.ActiveLanguageWorker.WithIndefiniteArticlePostProcessed(culprit.Label);

        var sb = new StringBuilder();

        sb.Append("USH_GE_ADPShortCircuit".Translate(culpritLabel));

        if (totalEnergy > 0f)
        {
            sb.AppendLine().AppendLine();
            sb.Append("USH_GE_ADPShortCircuitDischargedEnergy".Translate(totalEnergy.ToString("F0")));
        }

        if (explosionRadius > 5f)
        {
            sb.AppendLine().AppendLine();
            sb.Append("USH_GE_ADPShortCircuitWasLarge".Translate());
        }

        Find.LetterStack.ReceiveLetter(
            "USH_GE_LetterLabelADPShortCircuit".Translate(),
            sb.ToString(),
            LetterDefOf.NegativeEvent,
            new TargetInfo(culprit.Position, map)
        );
    }

    private static void DrainBatteriesAndCauseExplosion(PowerNet net, Building culprit, out float totalEnergy, out float explosionRadius)
    {
        totalEnergy = 0f;

        foreach (var battery in net.batteryComps)
        {
            totalEnergy += battery.StoredEnergy;
            battery.DrawPower(battery.StoredEnergy);
        }

        explosionRadius = Mathf.Clamp(Mathf.Sqrt(totalEnergy) * 0.05f, 1.5f, 14.9f) * 3f;

        GenExplosion.DoExplosion(culprit.Position, net.Map, 2.9f, DamageDefOf.Flame, null);

        if (explosionRadius > 3.5f)
        {
            GenExplosion.DoExplosion(culprit.Position, net.Map, explosionRadius * 0.85f, USH_DefOf.USH_ADP, null);
        }
    }
}
