using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;


[HarmonyPatch(typeof(PawnWeaponGenerator), nameof(PawnWeaponGenerator.TryGenerateWeaponFor))]
public static class PawnWeaponGenerator_TryGenerateWeaponFor_Patch
{
    private const float OVERCLOCK_CHANCE = 0.08f;
    public static void Postfix(Pawn pawn, PawnGenerationRequest request)
    {
        var weapon = pawn.equipment?.Primary;
        if (weapon is ThingWithComps thing)
        {
            if (thing.TryGetComp(out CompOverclock compOverclock))
                if (Rand.Chance(OVERCLOCK_CHANCE) || request.KindDef == USH_DefOf.USH_AncientGlittertechSoldier)
                    compOverclock.IsOverclocked = true;
        }
    }
}