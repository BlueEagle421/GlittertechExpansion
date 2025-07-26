using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(StunHandler), "CanAdaptToDamage")]
static class Patch_StunHandler_CanAdaptToDamage
{
    static bool Prefix(DamageDef def, ref bool __result)
    {
        if (def != null && def == USH_DefOf.USH_ADP)
        {
            __result = false;
            return false;
        }

        return true;
    }
}