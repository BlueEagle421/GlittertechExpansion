using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(BillUtility), nameof(BillUtility.MakeNewBill), [typeof(RecipeDef), typeof(Precept_ThingStyle)])]
public static class Patch_BillUtility_MakeNewBill
{
    [HarmonyPrefix]
    public static bool Prefix(RecipeDef recipe, Precept_ThingStyle precept, ref Bill __result)
    {
        if (recipe.HasModExtension<ModExtension_UseOverclockBill>())
        {
            __result = new Bill_Overclock(recipe, precept);
            return false;
        }

        if (recipe.HasModExtension<ModExtension_UseGlittertechBill>())
        {
            __result = new Bill_Glittertech(recipe, precept);
            return false;
        }

        return true;
    }
}
