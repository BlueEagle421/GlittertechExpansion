using HarmonyLib;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class Patch_Pawn_Kill
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance)
    {
        if (!OverclockIncidentUtility.CanAffectPawn(__instance, out ThingWithComps overclockedGun))
            return;

        OverclockIncidentUtility.DoOverclockIncident(__instance, overclockedGun, 6);
    }
}