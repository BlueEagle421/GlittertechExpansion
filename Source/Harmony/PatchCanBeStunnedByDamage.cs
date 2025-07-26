using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(StunHandler), "CanBeStunnedByDamage")]
static class Patch_StunHandler_CanBeStunnedByDamage
{
    static bool Prefix(DamageDef def, StunHandler __instance, ref bool __result)
    {
        if (def == USH_DefOf.USH_ADP)
        {
            __result = __instance.parent is not Pawn p || !p.RaceProps.IsFlesh;

            return false;
        }

        return true;
    }
}