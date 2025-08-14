using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_GE;

public class WorldComponent_UpdateHandler(World world) : WorldComponent(world)
{
    private int _passedTicks;
    private bool _didLetter;

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();

        if (_didLetter)
            return;

        RemoveCellsFromPolicies();

        _passedTicks++;

        if (_passedTicks >= 60)
        {
            if (!PlayerHasMemoryCells())
            {
                _didLetter = true;
                return;
            }

            DoUpdateLetter();
        }
    }

    private void DoUpdateLetter()
    {
        string content = "Hi!\n\nThis message can be discarded if you’re not using any memory cells.\n\nThe Glittertech Expansion mod has just received a chunky update.\nUnfortunately, due to a massive overhaul of the memory cell mechanics, you may see a couple of red errors in the console. These should be harmless, but to suppress them entirely, all extracted memories in cells must be erased.\nI’m sorry for the inconvenience, but I hope the new mechanics will be worth it!\n\nEnjoy your playthrough!\n\n~Mod author";
        Find.LetterStack.ReceiveLetter("Glittertech Expansion Update", content, LetterDefOf.NeutralEvent);

        _didLetter = true;
    }

    private bool PlayerHasMemoryCells()
    {
        foreach (Map map in Find.Maps)
            if (map.listerThings.AnyThingWithDef(USH_DefOf.USH_MemoryCellEmpty)
            || map.listerThings.AnyThingWithDef(USH_DefOf.USH_MemoryCellPositive)
            || map.listerThings.AnyThingWithDef(USH_DefOf.USH_MemoryCellNegative))
                return true;

        return false;
    }



    private void RemoveCellsFromPolicies()
    {
        try
        {
            foreach (var policy in Current.Game.drugPolicyDatabase.AllPolicies)
                FixPolicy(policy);
        }
        catch
        {

        }
    }

    private void FixPolicy(DrugPolicy policy)
    {
        FieldInfo fi = AccessTools.Field(typeof(DrugPolicy), "entriesInt");

        var list = fi.GetValue(policy) as List<DrugPolicyEntry>;

        List<ThingDef> toRemove = [USH_DefOf.USH_MemoryCellPositive, USH_DefOf.USH_MemoryCellNegative];

        list.RemoveAll(x => toRemove.Contains(x.drug));
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _didLetter, nameof(_didLetter));
    }
}