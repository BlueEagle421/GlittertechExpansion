using HarmonyLib;
using Verse;
using RimWorld;

namespace USH_GE;

[HarmonyPatch(typeof(AddictionUtility), nameof(AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize))]
static class Patch_AddictionUtility_ModifyChemicalEffectForToleranceAndBodySize
{
    static void Postfix(Pawn pawn, ref float effect, bool applyGeneToleranceFactor)
    {
        if (pawn == null)
            return;

        try
        {
            if (applyGeneToleranceFactor)
                effect *= pawn.GetStatValue(USH_DefOf.USH_DrugToleranceGainRate);
        }
        catch { }
    }
}