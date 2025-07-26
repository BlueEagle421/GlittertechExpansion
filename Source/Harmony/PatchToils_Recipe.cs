using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace USH_GE;

[HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.Notify_IterationCompleted))]
public static class Patch_Bill_Medical_Notify_IterationCompleted
{
    [HarmonyPostfix]
    static void Postfix(Bill_Medical __instance, Pawn billDoer, List<Thing> ingredients)
    {
        if (__instance == null || billDoer == null)
            return;

        if (__instance.recipe == null || !__instance.recipe.anesthetize)
            return;

        if (__instance.GiverPawn == null)
            return;

        if (__instance.billStack.Bills
            .Any(x => x.PawnAllowedToStartAnew(billDoer)
                && x.CompletableEver
                && x.ShouldDoNow()
                && BillHasAllIngredients(__instance, billDoer, __instance.billStack.billGiver)))
            return;

        var room = billDoer.GetRoom();
        if (room == null)
            return;

        foreach (var cell in room.Cells)
        {
            foreach (var thing in billDoer.Map.thingGrid.ThingsAt(cell))
            {
                if (thing.def != USH_DefOf.USH_NeuroclearConsole)
                    continue;

                var compConsole = thing.TryGetComp<CompNeuroclearConsole>();
                if (compConsole == null || !compConsole.CanInteract(billDoer) || !compConsole.AutoUse)
                    continue;

                var job = JobMaker.MakeJob(JobDefOf.InteractThing, thing);
                job.count = 1;
                billDoer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                return;
            }
        }
    }

    private static bool BillHasAllIngredients(Bill bill, Pawn pawn, IBillGiver giver)
    {
        var chosen = new List<ThingCount>();
        var missing = new List<IngredientCount>();

        var m = typeof(WorkGiver_DoBill)
            .GetMethod("TryFindBestBillIngredients",
                       BindingFlags.Static | BindingFlags.NonPublic);

        bool result = (bool)m.Invoke(null, [bill, pawn, (Thing)giver, chosen, missing]);

        return result;
    }
}