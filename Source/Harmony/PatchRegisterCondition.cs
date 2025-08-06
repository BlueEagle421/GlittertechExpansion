using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(MapEvents), nameof(MapEvents.Notify_GameConditionAdded))]
public static class Patch_GameConditionManager_RegisterCondition
{
    private const int INTERCEPT_LETTER_DELAY = 60;

    [HarmonyPostfix]
    public static void Postfix(MapEvents __instance, GameCondition condition)
    {
        if (condition?.def == null || __instance?.map == null)
            return;

        if (condition.def != IncidentDefOf.SolarFlare.gameCondition)
            return;

        var component = __instance.map.GetComponent<MapComponent_SolarFlareBank>();
        if (component?.AllAvailableSolarBanks.NullOrEmpty() ?? true)
            return;

        InterceptSolarFlare(component.AllAvailableSolarBanks, condition);
    }

    private static void InterceptSolarFlare(List<CompSolarFlareBank> allBankComps, GameCondition condition)
    {
        allBankComps.ForEach(x => x.Notify_SolarFlareIntercepted());

        string label = "USH_GE_SolarFlareInterceptedLabel".Translate();
        string text = "USH_GE_SolarFlareInterceptedText".Translate(allBankComps.Count);

        condition.End();

        var targets = new LookTargets(allBankComps.Select(x => x.parent));
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, targets, null, null, null, null, INTERCEPT_LETTER_DELAY);
    }
}
