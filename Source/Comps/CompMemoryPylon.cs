using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class CompProperties_MemoryPylon : CompProperties
{
    public string activeOverlayPositivePath;
    public string activeOverlayNegativePath;

    public CompProperties_MemoryPylon()
    {
        compClass = typeof(CompMemoryPylon);
    }
}

public class CompMemoryPylon : ThingComp
{
    public CompProperties_MemoryPylon PylonProps => (CompProperties_MemoryPylon)props;

    private CompMemoryCellContainer _compContainer;
    private CompGlower _compGlower;
    private CompPowerTrader _compPower;

    private bool _isWorkingNow;

    private float WorkRadius => parent.def.specialDisplayRadius;
    private float _cachedRadiusSquared = -1;
    private float WorkRadiusSquared
    {
        get
        {
            if (_cachedRadiusSquared == -1)
                _cachedRadiusSquared = WorkRadius * WorkRadius;

            return _cachedRadiusSquared;
        }
    }

    private readonly MaterialPropertyBlock _matProps = new();
    private Material _sharedMatCached;
    public Material SharedOverlayMaterial
    {
        get
        {
            _sharedMatCached ??= MaterialPool.MatFrom(PylonProps.activeOverlayPositivePath, ShaderDatabase.Transparent);

            return _sharedMatCached;
        }
    }

    private const int TICK_CHECK_INTERVAL = 60;
    private const float Z_OFFSET = .018f;
    private const float GLOW_MULTIPLIER = 0.45f;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _compContainer = parent.GetComp<CompMemoryCellContainer>();
        _compContainer.OnInserted += StartWorking;
        _compContainer.OnExtracted += StopWorking;

        _compGlower = parent.GetComp<CompGlower>();

        _compPower = parent.GetComp<CompPowerTrader>();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        _isWorkingNow = false;

        _compContainer.OnInserted -= StartWorking;
        _compContainer.OnExtracted -= StopWorking;
        RemoveAllPylonMemories(map);

        base.PostDeSpawn(map, mode);
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.CompInspectStringExtra());

        if (_compContainer.Full)
        {
            sb.AppendLine("USH_GE_ExpiresIn".Translate() + ": " + _compContainer.ContainedCell.ExpireTimeString());
            sb.AppendLine(_compContainer.ContainedCell.MemoryCellData.GetInspectString());
        }

        return sb.ToTaggedString().Trim();
    }

    private void StartWorking()
    {
        if (_isWorkingNow)
            return;

        if (!CanWork())
            return;

        bool isPositive = _compContainer.ContainedCell.MemoryCellData.IsPositive();
        _compGlower.GlowColor = ColorInt.FromHdrColor(
            MemoryUtils.GetThoughtColor(isPositive) * GLOW_MULTIPLIER);

        _compPower.PowerOutput = -_compPower.Props.PowerConsumption;

        CreatePylonMemoriesInRadius();

        string texPath = isPositive ? PylonProps.activeOverlayPositivePath : PylonProps.activeOverlayNegativePath;
        _matProps.SetTexture("_MainTex", ContentFinder<Texture2D>.Get(texPath));

        _isWorkingNow = true;
    }

    private void StopWorking()
    {
        if (!_isWorkingNow)
            return;

        _compGlower.GlowColor = ColorInt.FromHdrColor(Color.clear);

        _compPower.PowerOutput = -_compPower.Props.idlePowerDraw;

        RemoveAllPylonMemories(parent.Map);

        _isWorkingNow = false;
    }

    public override void CompTick()
    {
        base.CompTick();

        if (Find.TickManager.TicksGame % TICK_CHECK_INTERVAL != 0)
            return;

        if (!CanWork())
        {
            if (_isWorkingNow)
                StopWorking();

            return;
        }
        else if (!_isWorkingNow)
            StartWorking();

        CreatePylonMemoriesInRadius();
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        DrawOverlay(drawLoc);
    }

    private void DrawOverlay(Vector3 drawLoc)
    {
        if (!_isWorkingNow)
            return;

        Vector3 loc = drawLoc;
        loc += parent.def.graphicData.drawOffset;
        loc.y += Z_OFFSET;

        Mesh mesh = parent.Graphic.MeshAt(Rot4.North);
        Quaternion quat = parent.Graphic.QuatFromRot(parent.Rotation);

        Graphics.DrawMesh(mesh, loc, quat, SharedOverlayMaterial, 0, null, 0, _matProps);
    }

    private void CreatePylonMemoriesInRadius()
    {
        foreach (var p in parent.Map.mapPawns.FreeColonistsSpawned)
        {
            float dist = p.Position.DistanceToSquared(parent.Position);

            if (dist <= WorkRadiusSquared)
            {
                var mems = p.needs.mood.thoughts.memories;
                if (!mems.Memories.Any(IsMemoryFromHere))
                    CreatePylonMemory(p);
            }
            else
                RemovePylonMemory(p);
        }
    }

    private void CreatePylonMemory(Pawn p)
    {
        Thought_MemoryPylon toGive = ThoughtMaker.MakeThought(USH_DefOf.USH_MemoryPylonThought) as Thought_MemoryPylon;
        toGive.MemoryCellData = _compContainer.ContainedCell.MemoryCellData;
        toGive.SourceThing = parent;

        p.needs.mood.thoughts.memories.TryGainMemory(toGive);
    }

    private void RemovePylonMemory(Pawn p)
    {
        var mems = p.needs.mood.thoughts.memories;
        Thought_Memory toRemove = mems.Memories.Find(IsMemoryFromHere);

        if (toRemove != null)
            mems.RemoveMemory(toRemove);
    }

    private void RemoveAllPylonMemories(Map map)
    {
        map.mapPawns.FreeColonistsSpawned.ForEach(RemovePylonMemory);
    }

    private bool IsMemoryFromHere(Thought_Memory m)
    {
        if (m is not Thought_MemoryPylon thoughtPylon)
            return false;

        if (thoughtPylon.SourceCompPylon != this)
            return false;

        return true;
    }

    private bool CanWork()
    {
        if (_compContainer.ContainedCell == null)
            return false;

        if (!_compPower.PowerOn)
            return false;

        return true;
    }
}