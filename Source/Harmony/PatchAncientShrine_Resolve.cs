using HarmonyLib;
using RimWorld.BaseGen;

namespace USH_GE;

[HarmonyPatch(typeof(SymbolResolver_Interior_AncientTemple), "Resolve")]
public static class Patch_SymbolResolver_Interior_AncientTemple_Resolve
{
    public static void Postfix(ResolveParams rp)
    {
        if (rp.rect == null)
            return;

        ResolveParams crateParams = rp;
        crateParams.singleThingDef = USH_DefOf.USH_Glittercrate;
        BaseGen.symbolStack.Push("thing", crateParams);
    }
}