using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI;
using System.Collections.Generic;

namespace USH_GE;

public class ModExtension_UseGlittertechBill : DefModExtension
{
    public int powerNeeded;
    public int fuelNeeded;
    public float fabricatorOffsetY = 0.75f;
    public float fabricatorRotationY = 0;
    public float fabricatorScale = 0.6f;
    public List<ThingDef> requiredFacilities;
}

public class Bill_Glittertech : Bill_Autonomous
{
    private Pawn boundPawn;
    private int gestationCycles;
    public Pawn BoundPawn => boundPawn;
    public int GestationCyclesCompleted => gestationCycles;
    public Building_GlittertechFabricator Fabricator => (Building_GlittertechFabricator)billStack.billGiver;
    public ModExtension_UseGlittertechBill GlittertechExt;

    protected override Color BaseColor
    {
        get
        {
            if (suspended)
                return base.BaseColor;

            return Color.white;
        }
    }
    protected virtual Graphic InitialProductGraphic
    {
        get
        {
            if (recipe.products.NullOrEmpty())
                return null;

            return recipe.products[0].thingDef.graphic;
        }
    }
    public virtual Graphic GetProductGraphic
    {
        get
        {
            Graphic result = InitialProductGraphic;

            if (InitialProductGraphic == null)
                return null;

            if (result is Graphic_StackCount graphic_StackCount)
                result = graphic_StackCount.SubGraphicForStackCount(recipe.products[0].count, recipe.products[0].thingDef);

            if (result is Graphic_RandomRotated graphic_RandomRotated)
                result = graphic_RandomRotated.SubGraphic;

            return result;
        }
    }

    protected override string StatusString
    {
        get
        {
            switch (State)
            {
                case FormingState.Gathering:
                    break;

                case FormingState.Preparing:
                    return "USH_GE_WaitingForMaintenance".Translate();

                case FormingState.Forming:
                    return "USH_GE_Forming".Translate();

                case FormingState.Formed:
                    return "USH_GE_WaitingForCompletion".Translate();
            }
            return null;
        }
    }

    public Bill_Glittertech() { }

    public Bill_Glittertech(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
    {
        GlittertechExt = recipe.GetModExtension<ModExtension_UseGlittertechBill>();
    }

    public override void Notify_DoBillStarted(Pawn billDoer)
    {
        base.Notify_DoBillStarted(billDoer);

        if (boundPawn != billDoer)
            boundPawn = billDoer;
    }


    public override void Reset()
    {
        base.Reset();
        gestationCycles = 0;
        boundPawn = null;
    }

    public void ForceCompleteAllCycles()
    {
        gestationCycles = recipe.gestationCycles;
        formingTicks = 0f;
    }

    public float FormingSpeedMultiplier()
    {
        float statValue = Fabricator.GetStatValue(USH_DefOf.USH_GlittertechDuration);
        float settingValue = GE_Mod.Settings.FormingSpeedMultiplier.Value;
        return 1f / (statValue * settingValue);
    }

    public override void BillTick()
    {
        if (state == FormingState.Preparing && IsAutomated())
        {
            formingTicks = recipe.formingTicks;
            state = FormingState.Forming;
            return;
        }

        if (suspended || state != FormingState.Forming || !Fabricator.PowerTrader.PowerOn)
            return;

        formingTicks -= 1f * FormingSpeedMultiplier();

        if (formingTicks > 0f)
            return;

        gestationCycles++;
        if (gestationCycles >= recipe.gestationCycles)
        {
            state = FormingState.Formed;
            Fabricator.Notify_FormingCompleted();
            return;
        }
        formingTicks = recipe.formingTicks;
        state = FormingState.Preparing;
    }

    private bool IsAutomated()
        => Fabricator.CompFacilities.LinkedFacilitiesListForReading
            .Any(x => x.def == USH_DefOf.USH_AwareGlitterpanel);

    public override bool PawnAllowedToStartAnew(Pawn p)
    {
        if (State == FormingState.Gathering)
        {
            if (!Fabricator.HasStoredPower(GlittertechExt.powerNeeded))
            {
                JobFailReason.Is("USH_GE_NoPowerStoredShort".Translate(Fabricator.PowerNeededWithStat(this), Fabricator.StoredPower()), null);
                return false;
            }

            var facilities = GlittertechExt.requiredFacilities;
            if (facilities != null && !HasFacilities(facilities, out var missingFacility))
            {
                JobFailReason.Is("USH_GE_NoFacility".Translate(missingFacility.label));
                return false;
            }
        }

        return base.PawnAllowedToStartAnew(p);
    }

    private bool HasFacilities(List<ThingDef> toCheck, out ThingDef missingFacility)
    {
        missingFacility = null;
        CompAffectedByFacilities compAffected = Fabricator.TryGetComp<CompAffectedByFacilities>();

        for (int i = 0; i < toCheck.Count; i++)
            if (Fabricator.CompFacilities.LinkedFacilitiesListForReading
                .Find(x => IsRequiredFacility(Fabricator.CompFacilities, x, toCheck[i])) == null)
            {
                missingFacility = toCheck[i];
                return false;
            }

        return true;
    }

    private bool IsRequiredFacility(CompAffectedByFacilities compAffected, Thing thing, ThingDef requiredThingDef)
        => thing.def == requiredThingDef && compAffected.IsFacilityActive(thing);

    public override void AppendInspectionData(StringBuilder sb)
    {
        if (State is FormingState.Gathering)
        {
            sb.AppendLine("USH_GE_GatheredIngredients".Translate() + ":");
            AppendCurrentIngredientCount(sb);
        }

        if (State is FormingState.Forming)
            sb.AppendLine(FormingTimeString());

        if (State is FormingState.Preparing)
            sb.AppendLine("USH_GE_WaitingForMaintenance".Translate());

        if (State is FormingState.Formed)
            sb.AppendLine("USH_GE_WaitingForCompletion".Translate());

        if (State == FormingState.Forming || State == FormingState.Preparing)
            sb.AppendLine(RemainingCyclesString());
    }

    private string FormingTimeString()
    {
        string translated = "USH_GE_CurrentFormingCycle".Translate();

        string timeLeft = ((int)(formingTicks / FormingSpeedMultiplier())).ToStringTicksToPeriod();

        return translated + ": " + timeLeft;
    }

    private string RemainingCyclesString()
    {
        string translated = "USH_GE_RemainingFormingCycles".Translate();

        string cyclesLeft = (recipe.gestationCycles - GestationCyclesCompleted).ToString();

        string allCycles = recipe.gestationCycles.ToString();

        return $"{translated}: {cyclesLeft} ({"OfLower".Translate()} {allCycles})";
    }


    public override Bill Clone()
    {
        Bill_Glittertech obj = (Bill_Glittertech)base.Clone();
        obj.GlittertechExt = GlittertechExt;

        return obj;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref boundPawn, "boundPawn", false);
        Scribe_Values.Look(ref gestationCycles, "gestationCycles", 0, false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            GlittertechExt = recipe.GetModExtension<ModExtension_UseGlittertechBill>();
    }
}