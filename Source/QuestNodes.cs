using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;

namespace USH_GE;

public class QuestNode_MakeLord : QuestNode
{
    public enum LordJobType
    {
        Assault, Defend, Assist
    }

    public SlateRef<bool> canFlee;
    public SlateRef<bool> canSteal;
    public SlateRef<Faction> faction;
    public SlateRef<LordJobType> lordJob;
    public SlateRef<List<Pawn>> pawns;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var fac = faction.GetValue(slate);
        var map = slate.Get<Map>("map");
        LordMaker.MakeNewLord(fac, lordJob.GetValue(slate) switch
        {
            LordJobType.Assault => new LordJob_AssaultColony(fac, false, canFlee.GetValue(slate), false, true, canSteal.GetValue(slate)),
            LordJobType.Defend => new LordJob_DefendBase(fac, map.Center, 180000, true),
            LordJobType.Assist => new LordJob_AssistColony(fac, CellFinder.RandomSpawnCellForPawnNear(CellFinder.RandomEdgeCell(map), map)),
            _ => throw new ArgumentOutOfRangeException()
        }, map, pawns.GetValue(slate));
    }

    protected override bool TestRunInt(Slate slate) => true;
}

public class QuestNode_AncientForces : QuestNode
{
    public SlateRef<string> inSignal;
    public SlateRef<QuestNode_MakeLord.LordJobType?> lord;
    public SlateRef<MapParent> mapParent;
    public SlateRef<float> points;
    public SlateRef<float?> pointsFactor;

    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        var site = mapParent.GetValue(slate);
        var tile = site.Tile;
        var parms = new PawnGroupMakerParms_Saveable
        {
            groupKind = PawnGroupKindDefOf.Combat,
            points = points.GetValue(slate) * (pointsFactor.GetValue(slate) ?? 1f),
            faction = Faction.OfAncientsHostile,
            generateFightersOnly = true,
            dontUseSingleUseRocketLaunchers = true,
            inhabitants = true,
            tile = tile,
            seed = Gen.HashCombineInt(Find.World.info.Seed, tile)
        };

        QuestGen.quest.AddPart(new QuestPart_SpawnForces
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            mapParent = site,
            parms = parms,
            lord = lord.GetValue(slate) ?? QuestNode_MakeLord.LordJobType.Defend
        });

        QuestGen.AddQuestDescriptionRules(new List<Rule>
        {
            new Rule_String("forces_description",
                PawnUtility.PawnKindsToLineList(PawnGroupMakerUtility.GeneratePawnKindsExample(parms), "  - ", ColoredText.ThreatColor))
        });
    }

    protected override bool TestRunInt(Slate slate) => points.GetValue(slate) > 0f;
}

public class QuestPart_SpawnForces : QuestPart
{
    public string inSignal;
    public QuestNode_MakeLord.LordJobType lord;
    public MapParent mapParent;
    public PawnGroupMakerParms_Saveable parms;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal && mapParent.HasMap)
        {
            var map = mapParent.Map;
            var forces = PawnGroupMakerUtility.GeneratePawns(parms).ToList();
            Rand.PushState(Gen.HashCombineInt(Find.World.info.Seed, mapParent.Tile));
            foreach (var pawn in forces)
                if (CellFinder.TryFindRandomCellNear(map.Center, map, 16, x => x.Standable(map), out var cell))
                    GenSpawn.Spawn(pawn, cell, map);
            LordMaker.MakeNewLord(Faction.OfAncientsHostile, lord switch
            {
                QuestNode_MakeLord.LordJobType.Assault => new LordJob_AssaultColony(Faction.OfAncientsHostile, false, false, false, true, false),
                QuestNode_MakeLord.LordJobType.Defend => new LordJob_DefendBase(Faction.OfAncientsHostile, map.Center, 180000),
                QuestNode_MakeLord.LordJobType.Assist => new LordJob_AssistColony(Faction.OfAncientsHostile, forces.RandomElement().Position),
                _ => throw new ArgumentOutOfRangeException()
            }, map, forces);
            Rand.PopState();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_References.Look(ref mapParent, nameof(mapParent));
        Scribe_Deep.Look(ref parms, nameof(parms));
    }
}

public class PawnGroupMakerParms_Saveable : PawnGroupMakerParms, IExposable
{
    public void ExposeData()
    {
        Scribe_Defs.Look(ref groupKind, nameof(groupKind));
        Scribe_Values.Look(ref tile, nameof(tile));
        Scribe_Values.Look(ref inhabitants, nameof(inhabitants));
        Scribe_Values.Look(ref points, nameof(points));
        Scribe_References.Look(ref faction, nameof(faction));
        Scribe_References.Look(ref ideo, nameof(ideo));
        Scribe_Defs.Look(ref traderKind, nameof(traderKind));
        Scribe_Values.Look(ref generateFightersOnly, nameof(generateFightersOnly));
        Scribe_Values.Look(ref dontUseSingleUseRocketLaunchers, nameof(dontUseSingleUseRocketLaunchers));
        Scribe_Defs.Look(ref raidStrategy, nameof(raidStrategy));
        Scribe_Values.Look(ref forceOneDowned, nameof(forceOneDowned));
        Scribe_Values.Look(ref seed, nameof(seed));
        Scribe_Defs.Look(ref raidAgeRestriction, nameof(raidAgeRestriction));
    }
}