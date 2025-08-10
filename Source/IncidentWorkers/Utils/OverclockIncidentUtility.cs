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
                if (!CanAffectPawn(pawn, out ThingWithComps overclockedGun))
                    continue;

                yield return (pawn, overclockedGun);
            }
        }
        finally { }
    }

    public static bool CanAffectPawn(Pawn pawn, out ThingWithComps overclockedGun)
    {
        overclockedGun = null;

        if (pawn?.equipment?.Primary is not ThingWithComps thing)
            return false;

        if (Mathf.Approximately(pawn.GetStatValue(StatDefOf.Flammability), 0f))
            return false;

        if (!thing.TryGetComp(out CompOverclock compOverclock))
            return false;

        if (!compOverclock.IsOverclocked)
            return false;

        if (compOverclock.BlocksSelfIgnite)
            return false;

        overclockedGun = thing;

        return true;
    }

    public static void DoOverclockIncident(Pawn pawn, ThingWithComps gun)
    {
        if (Mathf.Approximately(pawn.GetStatValue(StatDefOf.Flammability), 0f))
            return;

        GenExplosion.DoExplosion(pawn.Position, pawn.Map, 3.9f, DamageDefOf.Flame, null);
        GenExplosion.DoExplosion(pawn.Position, pawn.Map, 5.9f, USH_DefOf.USH_ADP, null);

        HealthUtility.DamageUntilDowned(pawn, false, DamageDefOf.Burn);

        Messages.Message("USH_GE_OverclockIncidentShort".Translate(gun.Label), gun, MessageTypeDefOf.NegativeEvent, true);
    }
}
