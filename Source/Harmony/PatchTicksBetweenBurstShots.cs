using HarmonyLib;
using UnityEngine;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(Verb), nameof(Verb.TicksBetweenBurstShots), MethodType.Getter)]
public static class Verb_TicksBetweenBurstShots_Postfix
{
    static void Postfix(Verb __instance, ref int __result)
    {
        if (__instance?.EquipmentSource == null)
            return;

        if (!__instance.EquipmentSource.TryGetComp<CompOverclock>(out var comp))
            return;

        var lens = comp.UpgradeLens;

        if (lens == null)
            return;

        float m = lens.Props.burstShotSpeedMultiplier;
        if (Mathf.Approximately(m, 1f))
            return;

        float spedUp = __result / m;
        __result = Mathf.RoundToInt(spedUp);
    }


}
