using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public static class ConfusionIncidentUtility
{
    public static IEnumerable<Pawn> GetResearchers(Map map)
    {
        if (!USH_DefOf.USH_GlittertechFabrication.IsFinished)
            yield break;

        ResearchProjectDef projectDef = Find.ResearchManager.GetProject();

        if (projectDef == null || projectDef.tab != USH_DefOf.USH_GlittertechExpansion)
            yield break;

        var pawns = map.mapPawns.AllPawnsSpawned;

        foreach (Pawn pawn in pawns)
        {
            if (pawn.CurJobDef == null)
                continue;

            if (pawn.CurJobDef != JobDefOf.Research)
                continue;

            yield return pawn;
        }
    }

    public static void DoConfusionIncident(Pawn pawn)
    {
        pawn.mindState.mentalStateHandler.TryStartMentalState(USH_DefOf.USH_ConfusedWandering);
    }
}
