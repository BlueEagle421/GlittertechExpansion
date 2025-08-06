using UnityEngine;
using Verse;
namespace USH_GE;

public class Hediff_CryogenicNexus : Hediff_AddedPart
{
    private float _instability;
    public float Instability
    {
        get => _instability;

        set => _instability = Mathf.Clamp(value, 0, 1);
    }

    public const float MAX_INSTABILITY = 1f, INSTABILITY_UP_RATE = 0.04f, INSTABILITY_DOWN_RATE = 0.0005f;
    public const int TICK_CHECK_INTERVAL = 250;
    private int _ticksPassed;


    public override void Tick()
    {
        base.Tick();

        _ticksPassed++;

        if (_ticksPassed < TICK_CHECK_INTERVAL)
            return;

        float ambientTemperature = pawn.AmbientTemperature;
        FloatRange floatRange = pawn.ComfortableTemperatureRange();

        if (ambientTemperature > floatRange.max)
            Instability += INSTABILITY_UP_RATE;
        else
            Instability -= INSTABILITY_DOWN_RATE;

        _ticksPassed = 0;
    }

    public override string LabelInBrackets
        => $"{CurStage.label} - {((MAX_INSTABILITY - Instability) * 100f).ToString("F0") + "%"}";

    public override int CurStageIndex =>
        Instability switch
        {
            >= 0.8f => 3,
            > 0.4f => 2,
            > 0 => 1,
            _ => 0
        };

    public void ResetInstability()
    {
        _instability = 0;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _instability, nameof(_instability));
    }
}
