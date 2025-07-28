using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(DefOfHelper), nameof(DefOfHelper.RebindAllDefOfs))]
public static class Patch_DefOfHelper_RebindAllDefOfs
{
    static void Postfix(bool earlyTryMode)
    {
        if (earlyTryMode)
            return;

        try
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {

                try
                {
                    if (def.IsRangedWeapon && def.techLevel >= TechLevel.Spacer)
                        (def.comps ??= []).Add(PropertiesToAdd);
                }
                catch (Exception innerEx)
                {
                    Log.Warning($"[Glittertech Expansion] while adding overclock comp failed to patch ThingDef '{def.defName}': {innerEx}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"[Glittertech Expansion] unexpected error in RebindAllDefOfs postfix. The overclock feature is disabled: {ex}");
        }
    }

    private static CompProperties_Overclock PropertiesToAdd
        => new()
        {
            statFactors = StatFactors,
            statOffsets = StatOffsets
        };

    private static List<StatModifier> StatFactors
        => [
            new StatModifier() { stat = StatDefOf.RangedWeapon_Cooldown, value = 0.9f },
        ];

    private static List<StatModifier> StatOffsets
        => [
            new StatModifier() { stat = StatDefOf.RangedWeapon_DamageMultiplier, value = 0.25f },
            new StatModifier() { stat = StatDefOf.RangedWeapon_ArmorPenetrationMultiplier, value = 0.1f },
            new StatModifier() { stat = StatDefOf.MarketValue, value = 200 },
        ];
}