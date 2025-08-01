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
                    if (ShouldBeOverclockable(def))
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

    private static bool ShouldBeOverclockable(ThingDef def)
    {
        if (!def.IsRangedWeapon)
            return false;

        if (def.techLevel < TechLevel.Spacer)
            return false;

        if (ModLister.OdysseyInstalled && def.thingCategories.Contains(ThingCategoryDefOf.WeaponsUnique))
            return false;

        if (def.thingCategories.Contains(USH_DefOf.Grenades))
            return false;

        return true;
    }

    private static CompProperties_Overclock PropertiesToAdd
        => new()
        {
            insertedSoundDefName = "USH_InsertMemoryCell",
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