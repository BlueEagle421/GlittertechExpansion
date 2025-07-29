using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public static class Patch_DefGenerator_GenerateImpliedDefs_PreResolve
{
    [HarmonyPostfix]
    public static void Postfix(bool hotReload = false)
    {
        foreach (RecipeDef item in NeutroamineRecipeDefGenerator.ImpliedRecipeDefs(hotReload))
            DefGenerator.AddImpliedDef(item, hotReload);
    }
}
