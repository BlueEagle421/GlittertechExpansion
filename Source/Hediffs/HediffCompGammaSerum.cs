using RimWorld;
using Verse;

namespace USH_GE;

public class HediffCompProperties_GammaSerum : HediffCompProperties
{
    public HediffCompProperties_GammaSerum() => compClass = typeof(HediffCompGammaSerum);
}

public class HediffCompGammaSerum : HediffComp
{
    public HediffCompProperties_GammaSerum Props => (HediffCompProperties_GammaSerum)props;

    private IntRange _doEffectTicksRange = new(4 * 2500, 7 * 2500); // 2 hours ~ 6 hours
    private int _ticksPassed, _ticksToDoEffect;
    private const float CHANCE_TO_FAIL_OFFSET = 0.15f, CHANCE_TO_DAMAGE = 0.35f, CHANCE_TO_BERSERK = 0.35f;
    private float _chanceToFail = 1f;
    private bool _success, _didBerserk;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);

        _ticksPassed += delta;

        if (_ticksPassed >= _ticksToDoEffect)
        {
            DoEffect();
            _ticksPassed = 0;
            _ticksToDoEffect = _doEffectTicksRange.RandomInRange;
        }
    }

    public override void CompPostMake()
    {
        base.CompPostMake();

        _ticksToDoEffect = _doEffectTicksRange.RandomInRange;
    }

    private void DoEffect()
    {
        if (Rand.Chance(_chanceToFail))
        {
            _chanceToFail -= CHANCE_TO_FAIL_OFFSET;

            if (Rand.Chance(CHANCE_TO_BERSERK) && !_didBerserk)
            {
                DoBerserk();
                return;
            }

            if (Rand.Chance(CHANCE_TO_DAMAGE))
            {
                DoDamage();
                return;
            }

            return;
        }

        RemoveWillAndCertainty();
    }

    private void DoBerserk()
    {
        parent.pawn.mindState.mentalStateHandler
            .TryStartMentalState(MentalStateDefOf.Berserk, "Reason" + ": " + parent.Label.UncapitalizeFirst(), true, true);

        _didBerserk = true;
    }

    private void DoDamage()
    {
        var brainPart = parent.pawn.health.hediffSet.GetBrain();
        if (brainPart == null) return;

        float amount = Rand.Range(0.25f, 1f);
        var dinfo = new DamageInfo(DamageDefOf.Burn, amount,
                                   armorPenetration: 0, angle: 0,
                                   instigator: parent.pawn, hitPart: brainPart);

        parent.pawn.TakeDamage(dinfo);

        PushFailMessage("USH_GE_GammaSerumFailHarm".Translate(parent.pawn.Named("PAWN")), MessageTypeDefOf.NegativeHealthEvent);
    }

    private void RemoveWillAndCertainty()
    {
        Pawn target = parent.pawn;

        target.guest.Recruitable = true;

        target.guest.resistance = 0f;
        target.guest.will = 0f;

        if (ModsConfig.IdeologyActive)
            target.ideo.OffsetCertainty(0f - target.ideo.Certainty);

        Find.LetterStack.ReceiveLetter(
            "USH_GE_GammaSerumSuccess".Translate(),
            "USH_GE_GammaSerumSuccessDesc".Translate(parent.pawn.Named("PAWN")),
            LetterDefOf.PositiveEvent,
            new TargetInfo(parent.pawn.Position, parent.pawn.Map)
        );

        _success = true;
        parent.pawn.health.RemoveHediff(parent);
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();

        if (!_success)
            PushFailMessage("USH_GE_GammaSerumFailFinal".Translate(parent.pawn.Named("PAWN")), MessageTypeDefOf.NegativeEvent);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();

        Scribe_Values.Look(ref _success, nameof(_success));
        Scribe_Values.Look(ref _ticksPassed, nameof(_ticksPassed));
        Scribe_Values.Look(ref _ticksToDoEffect, nameof(_ticksToDoEffect));
        Scribe_Values.Look(ref _chanceToFail, nameof(_chanceToFail));
        Scribe_Values.Look(ref _didBerserk, nameof(_didBerserk));
    }

    private void PushFailMessage(string content, MessageTypeDef messageTypeDef)
        => Messages.Message(content, parent.pawn, messageTypeDef, true);
}
