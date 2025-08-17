using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE;

public class CompProperties_Telepad : CompProperties_Interactable
{
    public int fuelConsumption = 5;
    public HediffDef hediffDef;
    public float hediffAddChance = 0.07f;
    public float mentalStateChance = 0.02f;
    public float machineBreakChance = 0.01f;

    public CompProperties_Telepad() => compClass = typeof(CompTelepad);
}

[StaticConstructorOnStartup]
public class CompTelepad : CompInteractable, ITargetingSource
{
    public CompProperties_Telepad PadProps => (CompProperties_Telepad)props;
    private readonly TargetingParameters TargetingParameters;
    private List<Pawn> _teleportSequence;
    CompRefuelable _refuelableComp;

    private Texture2D _teleportTex;
    public Texture2D TeleportTex
    {
        get
        {
            _teleportTex ??= ContentFinder<Texture2D>.Get("UI/Gizmos/Teleport");
            return _teleportTex;
        }
    }
    private Texture2D _teleportAllTex;
    public Texture2D TeleportAllTex
    {
        get
        {
            _teleportAllTex ??= ContentFinder<Texture2D>.Get("UI/Gizmos/TeleportAll");
            return _teleportAllTex;
        }
    }
    private Texture2D _teleportPlanetTex;
    public Texture2D TeleportPlanetTex
    {
        get
        {
            _teleportPlanetTex ??= ContentFinder<Texture2D>.Get("UI/Gizmos/TeleportPlanet");
            return _teleportPlanetTex;
        }
    }

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

    private void Teleport(Pawn toTel, bool preventNegativeEffects = false)
    {
        if (!CanBeTeleported(toTel))
            return;

        if (!preventNegativeEffects)
            DoTeleportationEffects(toTel);

        Interact(toTel, true);

        _refuelableComp.ConsumeFuel(PadProps.fuelConsumption);

        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(parent);
        SkipUtility.SkipTo(toTel, parent.Position, parent.Map);
        SpawnFleckEffect(parent.Position);
    }

    private void DoTeleportationEffects(Pawn toTel)
    {
        TryGiveMentalState(toTel);

        TryToGiveNausea(toTel);

        TryToBreakDownMachine();
    }

    private void TryGiveComa(Pawn p, float distance)
    {
        int duration = ComaDuration(p, distance);

        if (duration == 0)
            return;

        Hediff added = p.health.AddHediff(USH_DefOf.USH_TelepadComa);

        if (added.TryGetComp(out HediffComp_Disappears compDisappears))
            compDisappears.SetDuration(duration);
    }

    private int ComaDuration(Pawn p, float distance)
    {
        float ticksPerTile = 2500; // 1 hour
        float maxTicks = 60000 * 8; // 8 days
        float safeDistance = 3;

        if (distance < safeDistance)
            return 0;

        if (!p.RaceProps.IsFlesh)
            return 0;

        return Mathf.RoundToInt(Mathf.Min(distance * ticksPerTile, maxTicks));
    }

    private void TryGiveMentalState(Pawn p)
    {
        if (!p.RaceProps.IsFlesh)
            return;

        if (!Rand.Chance(PadProps.mentalStateChance))
            return;

        p.mindState.mentalStateHandler.TryStartMentalState(USH_DefOf.USH_WanderPsychoticTelepad, "USH_GE_TeleportReason".Translate());
    }

    private void TryToGiveNausea(Pawn p)
    {
        if (!p.RaceProps.IsFlesh)
            return;

        if (!Rand.Chance(PadProps.hediffAddChance))
            return;

        p.health.AddHediff(USH_DefOf.USH_TelepadNausea);
        Messages.Message("USH_GE_NauseaMsg".Translate(p.Named("PAWN")), new LookTargets(p), MessageTypeDefOf.NegativeEvent, true);
    }

    private void TryToBreakDownMachine()
    {
        if (!Rand.Chance(PadProps.machineBreakChance))
            return;

        if (!parent.TryGetComp(out CompBreakdownable compBreakdownable))
            return;

        compBreakdownable.DoBreakdown();
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

    private AcceptanceReport CanBeTeleported(Thing t, bool distanceCheck = true)
    {
        if (t is not Pawn p)
            return false;

        if (p.RaceProps.IsFlesh && !p.health.hediffSet.HasHediff(USH_DefOf.USH_InstalledTelepadIntegrator))
            return "USH_GE_MissingIntegrator".Translate();

        if (distanceCheck && t.Position.InHorDistOf(parent.Position, parent.def.specialDisplayRadius))
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
            return "NoFuel".Translate();

        var baseReport = base.CanInteract(activateBy, checkOptionalItems);

        if (!baseReport.Accepted && baseReport.Reason == "CannotReach".Translate())
            return true;

        return baseReport;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (HideInteraction)
            yield break;

        if (!parent.SpawnedOrAnyParentSpawned)
            yield break;

        yield return TeleportGizmo();

        yield return TeleportAllGizmo();

        yield return TeleportPlanetGizmo();
    }

    private Command_Action TeleportGizmo()
    {
        Command_Action command_Action = new()
        {
            defaultLabel = "USH_GE_OrderTeleportHere".Translate() + "...",
            defaultDesc = "USH_GE_OrderTeleportHereDesc".Translate(parent.Named("THING")),
            icon = TeleportTex,
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
        List<Pawn> teleportablePawns = [.. TeleportablePawns(true).Where(x => x.Map == parent.Map)];
        string toTeleport = "None".Translate();

        if (!teleportablePawns.NullOrEmpty())
            toTeleport = string.Join(",\n", teleportablePawns.Select(x => x.Name.ToStringFull));

        Command_Action command_Action = new()
        {
            defaultLabel = "USH_GE_OrderTeleportAll".Translate() + "...",
            defaultDesc = "USH_GE_OrderTeleportAllDesc".Translate(toTeleport),
            icon = TeleportAllTex,
            groupable = false,
            action = delegate
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

                _teleportSequence = teleportablePawns;
            },
            onHover = delegate
            {
                foreach (Pawn p in teleportablePawns)
                    GenDraw.DrawLineBetween(parent.DrawPos, p.DrawPos);
            }
        };

        AcceptanceReport acceptanceReport = CanInteract();

        if (!acceptanceReport.Accepted)
            command_Action.Disable(acceptanceReport.Reason.CapitalizeFirst());

        if (teleportablePawns.NullOrEmpty())
            command_Action.Disable("USH_GE_NoTeleportablePawns".Translate());

        return command_Action;
    }

    private Command_Action TeleportPlanetGizmo()
    {
        var teleportablePawns = TeleportablePlanetPawns();

        Command_Action command_Action = new()
        {
            defaultLabel = "USH_GE_OrderTeleportPlanet".Translate() + "...",
            defaultDesc = "USH_GE_OrderTeleportPlanetDesc".Translate(),
            icon = TeleportPlanetTex,
            groupable = false,
            action = delegate
            {
                List<FloatMenuOption> options = [];
                foreach (Pawn p in teleportablePawns)
                {
                    float distance = 0;
                    try
                    {
                        distance = GetTwoMapDistance(parent.Map, p.Map);
                    }
                    catch { }

                    string text = p.Name.ToStringFull;

                    int comaDuration = ComaDuration(p, distance);

                    if (comaDuration > 0)
                        text += $" ({"USH_GE_ComaMsg".Translate(comaDuration.ToStringTicksToPeriod()
                            .Colorize(ColorLibrary.RedReadable))})";

                    options.Add(new FloatMenuOption(text, delegate
                    {
                        Teleport(p);
                        TryGiveComa(p, distance);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };

        AcceptanceReport acceptanceReport = CanInteract();

        if (!acceptanceReport.Accepted)
            command_Action.Disable(acceptanceReport.Reason.CapitalizeFirst());

        if (teleportablePawns.NullOrEmpty())
            command_Action.Disable("USH_GE_NoTeleportablePawns".Translate());

        return command_Action;
    }

    private List<Pawn> TeleportablePawns(bool distanceCheck)
    {
        List<Pawn> result = [];

        foreach (Map map in Find.Maps)
        {
            result.AddRange(map.mapPawns.FreeColonistsSpawned);
            result.AddRange(map.mapPawns.SpawnedColonyMechs);
        }

        return [.. result.Where(x => CanBeTeleported(x, distanceCheck))];
    }

    private float GetTwoMapDistance(Map map1, Map map2)
    {
        int tile1 = map1.Tile;
        if (TryGetPocketMapPlanetTile(map1, out var planetTile1))
            tile1 = planetTile1;

        int tile2 = map2.Tile;
        if (TryGetPocketMapPlanetTile(map2, out var planetTile2))
            tile2 = planetTile2;

        return Find.WorldGrid.TraversalDistanceBetween(tile1, tile2);
    }

    private bool TryGetPocketMapPlanetTile(Map pocketMap, out PlanetTile planetTile)
    {
        planetTile = PlanetTile.Invalid;

        if (!pocketMap.IsPocketMap)
            return false;

        foreach (Map map in Find.Maps)
            if (map.ChildPocketMaps.Contains(pocketMap))
            {
                planetTile = map.Tile;
                return true;
            }

        return false;
    }

    private List<Pawn> TeleportablePlanetPawns()
        => [.. TeleportablePawns(false).Where(x => x.Map != parent.Map)];

    public override string CompInspectStringExtra()
        => "USH_GE_FuelCost".Translate(_refuelableComp.Props.FuelLabel, PadProps.fuelConsumption);

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (HideInteraction)
            yield break;

        AcceptanceReport acceptanceReport = CanBeTeleported(selPawn);
        FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(ActivateOptionLabel, delegate
        {
            Teleport(selPawn, true);
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
