using RimWorld;
using Verse;

namespace USH_GE;

public class IngestionOutcomeDoer_GiveHediffMemoryCell : IngestionOutcomeDoer_GiveHediff
{

    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
    {
        Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
        hediff.TryGetComp<HediffCompMemoryCell>().MemoryCellData =
            ((MemoryCell)ingested).MemoryCellData;

        float effect = severity <= 0f ? hediffDef.initialSeverity : severity;

        AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(
            pawn,
            toleranceChemical,
            ref effect,
            multiplyByGeneToleranceFactors,
            divideByBodySize: true);

        hediff.Severity = effect;
        pawn.health.AddHediff(hediff);
    }
}