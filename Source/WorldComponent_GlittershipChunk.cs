using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace USH_GE;

public class WorldComponent_GlittershipChunk : WorldComponent
{
    private IntRange _tickDelayRange = new(2500, 2500 * 3); //1 hour, 3 hours
    private int _ticksToFire;
    private int _ticksPassed;
    private bool _didEvent;

    private const int TICK_CHECK_INTERVAL = 2000;
    private const int THREAT_POINTS = 600;
    public static WorldComponent_GlittershipChunk Instance { get; private set; }
    public WorldComponent_GlittershipChunk(World world) : base(world)
    {
        if (Instance != null)
        {
            Log.Warning($"Duplicate {nameof(WorldComponent_GlittershipChunk)} detected. Ignoring.");
            return;
        }
        Instance = this;
        _ticksToFire = _tickDelayRange.RandomInRange;
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();

        if (_didEvent)
            return;

        _ticksPassed++;
        if (_ticksPassed % TICK_CHECK_INTERVAL != 0)
            return;

        if (!USH_DefOf.USH_GlittertechFabrication.IsFinished)
            return;

        _ticksToFire -= TICK_CHECK_INTERVAL;

        if (_ticksToFire <= 0)
        {
            try
            {
                FireEvent();
            }
            catch (Exception e)
            {
                Log.Warning("For some reason the glitterworld debris event didn't fire. I know. I'm also disappointed. Here's the exception if your curious: " + e);
            }
        }
    }

    public bool FireEvent()
    {
        Map map = Find.AnyPlayerHomeMap;

        if (map == null)
            return false;

        _didEvent = true;

        IntVec3 spawnPos = IntVec3.Invalid;

        Thing shipChunk = DropGlittershipChunk(map, ref spawnPos);
        Thing crate = DropCrate(map, spawnPos);

        shipChunk.SetFaction(Faction.OfMechanoids);
        crate.SetFaction(Faction.OfMechanoids);

        try
        {
            DropMechs(map, spawnPos, [shipChunk, crate]);
        }
        catch (Exception e)
        {
            Log.Warning("For some reason the mechanoids didn't spawn. I know. I'm also disappointed. Here's the exception if your curious: " + e);
        }

        string label = "USH_GE_LetterLabelGlittershipChunk".Translate();
        string desc = "USH_GE_LetterGlittershipChunk".Translate();
        Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter(label, desc, LetterDefOf.ThreatSmall, new LookTargets(spawnPos, map)));

        return true;
    }

    private Thing DropGlittershipChunk(Map map, ref IntVec3 spawnPos)
    {
        if (!TryFindShipChunkDropCell(map.Center, map, 999999, out var pos))
        {
            List<ThingDef> potentialSpawnDefs =
            [
                ThingDefOf.Bed,
                ThingDefOf.SimpleResearchBench,
                USH_DefOf.HiTechResearchBench,
                USH_DefOf.USH_ResearchProbe,
            ];

            pos = FindThingsOfDef(potentialSpawnDefs).First().Position;
        }

        spawnPos = pos;
        Thing shipChunk = ThingMaker.MakeThing(USH_DefOf.USH_GlittershipChunk);
        SkyfallerMaker.SpawnSkyfaller(USH_DefOf.USH_GlittershipChunkIncoming, shipChunk, pos, map);
        return shipChunk;
    }

    private Thing DropCrate(Map map, IntVec3 chunkPos)
    {
        Thing crate = ThingMaker.MakeThing(USH_DefOf.USH_Glittercrate);

        if (TryFindShipChunkDropCell(chunkPos, map, 8, out var cratePos))
            SkyfallerMaker.SpawnSkyfaller(USH_DefOf.USH_GlittercrateIncoming, crate, cratePos, map);
        else
            DropPodUtility.DropThingsNear(chunkPos, map, [crate]);

        return crate;
    }

    private void DropMechs(Map map, IntVec3 pos, List<Thing> toDefend)
    {
        float points = THREAT_POINTS;
        List<Pawn> mechanoids = [.. PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Combat,
            tile = map.Tile,
            faction = Faction.OfMechanoids,
            points = points
        })];

        mechanoids.ForEach(x => x.TryGetComp<CompCanBeDormant>()?.ToSleep());

        LordMaker.MakeNewLord(Faction.OfMechanoids,
        new LordJob_SleepThenMechanoidsDefend(
            things: toDefend,
            Faction.OfMechanoids,
            defendRadius: 28f,
            pos,
            canAssaultColony: false,
            isMechCluster: false), map, mechanoids);

        DropPodUtility.DropThingsNear(pos, map, mechanoids.Cast<Thing>());
    }

    private List<Thing> FindThingsOfDef(List<ThingDef> defs)
    {
        var listerThings = Find.CurrentMap.listerThings;
        List<Thing> result = [];

        defs.ForEach(x => result.AddRange(listerThings.ThingsOfDef(x)));
        return result;
    }

    private static bool TryFindShipChunkDropCell(IntVec3 nearLoc, Map map, int maxDist, out IntVec3 pos)
    {
        return CellFinderLoose.TryFindSkyfallerCell(
            ThingDefOf.ShipChunkIncoming,
            map,
            ThingDefOf.ShipChunk.terrainAffordanceNeeded,
            out pos,
            10,
            nearLoc,
            maxDist);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _didEvent, nameof(_didEvent));
    }
}