using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public static class OverclockIncidentUtility
{
    public static IEnumerable<(Pawn, ThingWithComps)> GetExplosiveGuns(Map map)
    {
        try
        {
            var pawns = map.mapPawns.AllPawnsSpawned;

            foreach (Pawn pawn in pawns)
            {
                if (pawn?.equipment?.Primary is not ThingWithComps thing)
                    continue;

                if (!thing.TryGetComp(out CompOverclock compOverclock))
                    continue;

                if (!compOverclock.IsOverclocked)
                    continue;

                yield return (pawn, thing);
            }
        }
        finally { }
    }

    public static void DoOverclockIncident(Pawn pawn, ThingWithComps gun)
    {
        GenExplosion.DoExplosion(pawn.Position, pawn.Map, 5.9f, DamageDefOf.Flame, null);

        if (!Mathf.Approximately(pawn.GetStatValue(StatDefOf.Flammability), 0f))
            HealthUtility.DamageUntilDowned(pawn, false, DamageDefOf.Burn);

        Find.LetterStack.ReceiveLetter(
            "USH_GE_LetterLabelOverclockIncident".Translate(),
            "USH_GE_OverclockIncident".Translate(gun.Label.UncapitalizeFirst()),
            LetterDefOf.NegativeEvent,
            new TargetInfo(pawn.Position, pawn.Map)
        );
    }
}
