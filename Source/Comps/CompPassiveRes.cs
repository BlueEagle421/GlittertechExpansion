using RimWorld;
using Verse;

namespace USH_GE;

public class CompProperties_PassiveRes : CompProperties
{
    public float ticksPerResearch;
    public float researchAmount;
    public CompProperties_PassiveRes() => compClass = typeof(CompPassiveRes);
}

public class CompPassiveRes : ThingComp
{
    private int _ticksPassed;
    private float _researchPerformed;

    CompPowerTrader _powerTraderComp;
    CompFacility _facilityComp;

    public CompPowerTrader PowerTraderComp => _powerTraderComp;

    public CompProperties_PassiveRes Props => (CompProperties_PassiveRes)props;

    private EffecterHandler electricEffecterHandler;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _powerTraderComp = parent.GetComp<CompPowerTrader>();
        _facilityComp = parent.GetComp<CompFacility>();

        electricEffecterHandler = new EffecterHandler(parent, USH_DefOf.USH_ElectricResearchProbe);
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);

        _ticksPassed = 0;
        _researchPerformed = 0;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref _ticksPassed, "USH_ResearchTicksPassed", 0);
        Scribe_Values.Look(ref _researchPerformed, "USH_ResearchPerformed", 0);
    }

    public override void CompTick()
    {
        base.CompTick();

        ResearchTickRare();

        electricEffecterHandler.Tick();
    }

    private void ResearchTickRare()
    {
        if (!parent.IsHashIntervalTick(250))
            return;

        if (!ResearchReport().Accepted)
            return;

        _ticksPassed += 250;

        if (_ticksPassed >= Props.ticksPerResearch)
        {
            ConductResearch(Props.researchAmount);
            _ticksPassed = 0;
        }
    }

    private void ConductResearch(float amount)
    {
        ResearchManager researchManager = Find.ResearchManager;

        if (researchManager.GetProject() == null)
            return;

        if (!ResearchReport().Accepted)
            return;

        researchManager.ResearchPerformed(amount / ResearchManager.ResearchPointsPerWorkTick, null);
        _researchPerformed += amount;

        electricEffecterHandler.StartMaintaining();
    }

    private AcceptanceReport ResearchReport()
    {
        if (parent is MinifiedThing)
            return false;

        if (_powerTraderComp != null && !_powerTraderComp.PowerOn)
            return false;

        if (_facilityComp != null && _facilityComp.LinkedBuildings.Count == 0)
            return false;

        return true;
    }

    public override string CompInspectStringExtra() => "USH_GE_ResPerformed".Translate(_researchPerformed.ToString());
}
