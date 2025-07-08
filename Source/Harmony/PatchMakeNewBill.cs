using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(BillUtility), nameof(BillUtility.MakeNewBill), [typeof(RecipeDef), typeof(Precept_ThingStyle)])]
public static class BillUtility_MakeNewBill_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(RecipeDef recipe, Precept_ThingStyle precept, ref Bill __result)
    {
        if (recipe.HasModExtension<ModExtension_UseGlittertechBill>())
        {
            __result = new Bill_Glittertech(recipe, precept);
            return false;
        }

        return true;
    }
}
