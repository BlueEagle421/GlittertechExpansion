using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE;

[StaticConstructorOnStartup]
public class Building_GlittershipChunk : Building
{
    private Effecter _effecter;
    private Sustainer _sustainer;
    private IntRange _strikeTicksRange = new(2500 * 2, 2500 * 4); //2 hours, 4 hours
    private int _strikeTicks, _strikeAge, _strikeDuration;
    private IntVec3 strikeLoc = IntVec3.Invalid;
    private static readonly Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt");
    private Mesh boltMesh;
    private const int INITIAL_STRIKE_DELAY = 350;

    private const float STRIKE_RADIUS = 12f;

    public override void PostMake()
    {
        base.PostMake();

        _strikeTicks = INITIAL_STRIKE_DELAY;
    }

    protected override void Tick()
    {
        base.Tick();
        _effecter ??= USH_DefOf.USH_GlittershipChunkPulse.SpawnAttached(this, Map);
        _effecter?.EffectTick(this, this);

        if (_sustainer == null || _sustainer.Ended)
            _sustainer = USH_DefOf.USH_GlittershipAmbience.TrySpawnSustainer(SoundInfo.InMap(this));

        _sustainer.Maintain();

        _strikeTicks--;

        _strikeAge++;

        if (_strikeTicks <= 0)
        {
            strikeLoc = GenRadial.RadialCellsAround(Position, STRIKE_RADIUS, true).RandomElement();
            DoStrike(Map, ref boltMesh);
            _strikeTicks = _strikeTicksRange.RandomInRange;
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        USH_DefOf.USH_GlittershipStopped.PlayOneShot(this);
        USH_DefOf.USH_GlittershipChunkDestroyed.Spawn(this, Map);
        _sustainer?.End();
        base.DeSpawn(mode);
    }

    private void DoStrike(Map map, ref Mesh boltMesh)
    {
        _strikeAge = 0;
        _strikeDuration = Rand.Range(15, 60);
        SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(map);

        if (!strikeLoc.IsValid)
            strikeLoc = CellFinderLoose.RandomCellWith(sq => sq.Standable(map) && !map.roofGrid.Roofed(sq), map);

        boltMesh = LightningBoltMeshPool.RandomBoltMesh;

        if (!strikeLoc.Fogged(map))
        {
            GenExplosion.DoExplosion(strikeLoc, map, 1.9f, DamageDefOf.Flame, null);
            Vector3 loc = strikeLoc.ToVector3Shifted();

            for (int num = 0; num < 4; num++)
            {
                FleckMaker.ThrowSmoke(loc, map, 1.5f);
                FleckMaker.ThrowMicroSparks(loc, map);
                FleckMaker.ThrowLightningGlow(loc, map, 1.5f);
            }
        }

        SoundInfo info = SoundInfo.InMap(new TargetInfo(strikeLoc, map));
        SoundDefOf.Thunder_OnMap.PlayOneShot(info);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        Graphics.DrawMesh(
            boltMesh,
            strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather),
            Quaternion.identity,
            FadedMaterialPool.FadedVersionOf(LightningMat, LightningBrightness),
            0);
    }

    protected virtual float LightningBrightness
    {
        get
        {
            if (_strikeAge <= 3)
            {
                return _strikeAge / 3f;
            }
            return 1f - _strikeAge / (float)_strikeDuration;
        }
    }

}
