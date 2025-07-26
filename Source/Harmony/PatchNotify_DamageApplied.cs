using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied))]
[HarmonyPatch(typeof(StunHandler), nameof(StunHandler.Notify_DamageApplied))]
public static class Patch_StunHandler_Notify_DamageApplied
{
    private static readonly AccessTools.FieldRef<StunHandler, bool> stunFromEMPField =
        AccessTools.FieldRefAccess<StunHandler, bool>("stunFromEMP");

    static void Postfix(StunHandler __instance, DamageInfo dinfo)
    {
        if (dinfo.Amount > 0 && dinfo.Def == USH_DefOf.USH_ADP)
            stunFromEMPField(__instance) = true;
    }
}