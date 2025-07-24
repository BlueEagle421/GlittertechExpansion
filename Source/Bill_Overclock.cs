using Verse;
using RimWorld;
using System.Collections.Generic;

namespace USH_GE;

public class ModExtension_UseOverclockBill : ModExtension_UseGlittertechBill
{

}

public class Bill_Overclock : Bill_Glittertech
{
    public ModExtension_UseOverclockBill OverclockExt;
    public Bill_Overclock() { }
    public Bill_Overclock(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
    {
        OverclockExt = recipe.GetModExtension<ModExtension_UseOverclockBill>();
    }

    protected override Graphic InitialProductGraphic => Fabricator.OverclockingGun.parent.Graphic;

    public override Thing CreateProducts()
    {
        Fabricator.OverclockingGun.IsOverclocked = true;
        return Fabricator.OverclockingGun.parent;
    }

    public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
    {
        base.Notify_IterationCompleted(billDoer, ingredients);

        Thing thing = CreateProducts();
        if (thing != null)
            Fabricator.innerContainer.TryDrop(thing, Fabricator.InteractionCell, Fabricator.Map, ThingPlaceMode.Near, out _);
    }
}