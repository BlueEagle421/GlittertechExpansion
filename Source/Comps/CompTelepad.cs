using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_Telepad : CompProperties_Interactable
{
    public int fuelConsumption = 5;
    public HediffDef hediffDef = USH_DefOf.USH_TelepadNausea;
    public float hediffAddChance = 0.07f;

    public CompProperties_Telepad() => compClass = typeof(CompTelepad);
}

[StaticConstructorOnStartup]
public class CompTelepad : CompInteractable, ITargetingSource
{
    public CompProperties_Telepad PadProps => (CompProperties_Telepad)props;
    private readonly TargetingParameters TargetingParameters;
    private List<Pawn> _teleportSequence;
    CompRefuelable _refuelableComp;

    public CompTelepad()
    {
        TargetingParameters = new()
        {
            canTargetPawns = true,
            canTargetSelf = false,
            canTargetBuildings = false,
        };
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        _refuelableComp = parent.GetComp<CompRefuelable>();
    }

    public override void CompTick()
    {
        base.CompTick();

        if (!parent.IsHashIntervalTick(15))
            return;

        if (_teleportSequence.NullOrEmpty())
            return;

        Pawn toTel = _teleportSequence[0];

        if (CanBeTeleported(toTel))
            Teleport(toTel);

        _teleportSequence.Remove(toTel);
    }

    private void Teleport(Pawn toTel, bool draft = false)
    {
        if (!CanBeTeleported(toTel))
            return;

        TryToGiveNausea(toTel);

        Interact(toTel, true);

        _refuelableComp.ConsumeFuel(PadProps.fuelConsumption);

        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(parent);
        SkipUtility.SkipTo(toTel, parent.Position, parent.Map);
        SpawnFleckEffect(parent.Position);
    }

    private void TryToGiveNausea(Pawn p)
    {
        if (!Rand.Chance(0.07f))
            return;

        p.health.AddHediff(USH_DefOf.USH_TelepadNausea);
        Messages.Message("USH_GE_NauseaMsg".Translate(p.Named("PAWN")), new LookTargets(p), MessageTypeDefOf.NegativeEvent, true);
    }

    private void TargetPawnToTeleport()
    {
        Find.Targeter.BeginTargeting(TargetingParameters, delegate (LocalTargetInfo t)
        {
            Teleport(t.Pawn);
        }, null, null, null, null, null, playSoundOnAction: true, delegate (LocalTargetInfo t)
        {
            if (t.Pawn != null)
            {
                var report = CanBeTeleported(t.Pawn);
                if (!report.Accepted)
                {
                    string msg = $"{"USH_GE_CannotTeleport".Translate()}: {report.Reason.CapitalizeFirst()}"
                    .Colorize(ColorLibrary.RedReadable);

                    Widgets.MouseAttachedLabel(msg);
                    return;
                }
            }

            Widgets.MouseAttachedLabel("USH_GE_CommandChoosePawnToTeleport".Translate());
        });
    }

    private AcceptanceReport CanBeTeleported(Thing t)
    {
        if (t is not Pawn p)
            return false;

        if (p.RaceProps.IsFlesh && !p.health.hediffSet.HasHediff(USH_DefOf.USH_InstalledTelepadIntegrator))
            return "USH_GE_MissingIntegrator".Translate();

        if (t.Position.InHorDistOf(parent.Position, parent.def.specialDisplayRadius))
            return "USH_GE_TooClose".Translate();

        if (t.Faction != parent.Faction)
            return false;

        var interactionReport = CanInteract(p);

        if (!interactionReport)
            return interactionReport.Reason;

        return true;
    }

    public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
    {
        if (_refuelableComp.Fuel < PadProps.fuelConsumption)
            return "NoFuel";

        return base.CanInteract(activateBy, checkOptionalItems);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (HideInteraction)
            yield break;

        if (!parent.SpawnedOrAnyParentSpawned)
            yield break;

        yield return TeleportGizmo();

        yield return TeleportAllGizmo();
    }

    private Command_Action TeleportGizmo()
    {
        Command_Action command_Action = new()
        {
            defaultLabel = "OrderActivation".Translate() + "...",
            defaultDesc = "OrderActivationDesc".Translate(parent.Named("THING")),
            icon = UIIcon,
            groupable = false,
            action = delegate
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                TargetPawnToTeleport();
            }
        };

        AcceptanceReport acceptanceReport = CanInteract();

        if (!acceptanceReport.Accepted)
            command_Action.Disable(acceptanceReport.Reason.CapitalizeFirst());

        return command_Action;
    }

    private Command_Action TeleportAllGizmo()
    {
        var teleportablePawns = TeleportablePawns();
        string toTeleport = "None".Translate();

        if (!teleportablePawns.NullOrEmpty())
            toTeleport = string.Join(",\n", teleportablePawns.Select(x => x.Label));

        Command_Action command_Action = new()
        {
            defaultLabel = "USH_GE_OrderTeleportAll".Translate() + "...",
            defaultDesc = "USH_GE_OrderTeleportAllDesc".Translate(toTeleport),
            icon = UIIcon,
            groupable = false,
            action = delegate
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

                _teleportSequence = teleportablePawns;
            }
        };

        AcceptanceReport acceptanceReport = CanInteract();

        if (!acceptanceReport.Accepted)
            command_Action.Disable(acceptanceReport.Reason.CapitalizeFirst());

        if (teleportablePawns.NullOrEmpty())
            command_Action.Disable("USH_GE_NoTeleportablePawns".Translate());

        return command_Action;
    }

    private List<Pawn> TeleportablePawns()
    {
        var pawns = Enumerable.Concat(
            parent.Map.mapPawns.FreeColonistsSpawned,
            parent.Map.mapPawns.SpawnedColonyMechs);

        return [.. pawns.Where(x => CanBeTeleported(x))];
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (HideInteraction)
            yield break;

        AcceptanceReport acceptanceReport = CanBeTeleported(selPawn);
        FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(ActivateOptionLabel, delegate
        {
            Teleport(selPawn);
        }), selPawn, parent);
        if (!acceptanceReport.Accepted)
        {
            floatMenuOption.Disabled = true;
            floatMenuOption.Label = floatMenuOption.Label + " (" + acceptanceReport.Reason.UncapitalizeFirst() + ")";
        }

        yield return floatMenuOption;

    }

    private void SpawnFleckEffect(IntVec3 position) => FleckMaker.Static(position, parent.Map, FleckDefOf.PsycastSkipFlashEntry);
}
