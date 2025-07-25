
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_GE;

[StaticConstructorOnStartup]
public class ITab_ContentsBiocoder : ITab_ContentsBase
{
    private readonly List<Thing> listInt = [];
    public override IList<Thing> container
    {
        get
        {
            listInt.Clear();

            if (SelThing is Building_Biocoder targeter && targeter.ContainedThing != null)
                listInt.Add(targeter.ContainedThing);

            return listInt;
        }
    }

    public ITab_ContentsBiocoder()
    {
        labelKey = "TabCasketContents";
        containedItemsKey = "ContainedItems";
        canRemoveThings = false;
    }
}

[StaticConstructorOnStartup]
public class Building_Biocoder : Building_TurretRocket, IThingHolder, ISearchableContents
{
    public int VerbIndex { get; set; }
    public override Verb AttackVerb => GunCompEq.AllVerbs[VerbIndex];
    public override Material TurretTopMaterial => def.building.turretTopMat;


    protected ThingOwner innerContainer;
    public bool HasAnyContents => innerContainer.Count > 0;
    public Thing ContainedThing
    {
        get
        {
            if (innerContainer.Count != 0)
            {
                return innerContainer[0];
            }
            return null;
        }
    }


    public ThingOwner SearchableContents => innerContainer;

    public Building_Biocoder()
    {
        innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        if (Faction != Faction.OfPlayer || innerContainer.Count <= 0 || !def.building.isPlayerEjectable)
            yield break;

        Command_Action ejectAction = new()
        {
            action = EjectContents,
            defaultLabel = "USH_GE_CommandBiocoderEject".Translate(),
            defaultDesc = "USH_GE_CommandBiocoderEjectDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc8,
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectBiocoder")
        };

        if (innerContainer.Count == 0)
            ejectAction.Disable("USH_GE_CommandBiocoderEjectFailEmpty".Translate());

        yield return ejectAction;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
    }

    public virtual bool Accepts(Thing thing) => innerContainer.CanAcceptAnyOf(thing);

    public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (!Accepts(thing))
            return false;

        bool success;
        if (thing.holdingOwner != null)
        {
            success = innerContainer.TryAddOrTransfer(thing);

            if (allowSpecialEffects && success)
                PlayEnteredSound();
        }
        else
        {
            success = innerContainer.TryAdd(thing);
            if (allowSpecialEffects && success)
                PlayEnteredSound();
        }

        return success;
    }

    private void PlayEnteredSound()
    {
        SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(Position, Map));
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        base.Destroy(mode);
        KillAndDropContent(mode);
    }

    public void KillAndDropContent(DestroyMode mode = DestroyMode.Vanish)
    {
        if (innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
        {
            if (mode != DestroyMode.Deconstruct)
                foreach (Thing t in innerContainer)
                    if (t is Pawn p)
                        HealthUtility.DamageUntilDowned(p);

            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }
        innerContainer.ClearAndDestroyContents();
    }

    public void BurnContainedPawn()
    {
        if (!HasAnyContents)
            return;

        foreach (Thing t in innerContainer)
            if (t is Pawn p)
                HealthUtility.DamageUntilDead(p, DamageDefOf.Burn);

        innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        innerContainer.ClearAndDestroyContents();
    }

    public virtual void EjectContents()
    {
        USH_DefOf.USH_Eject.PlayOneShot(this);
        innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
    }

    protected override void BeginBurst()
    {
        base.BeginBurst();

        ChangeGoodwillNearestFactions(Map, 4, -30);
        BurnContainedPawn();
    }

    public override string GetInspectString()
    {
        string baseStr = base.GetInspectString();
        string str = innerContainer.ContentsString;

        if (!baseStr.NullOrEmpty())
            baseStr += "\n";

        return baseStr + ("Contains".Translate() + ": " + str.CapitalizeFirst());
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
    {
        if (myPawn.IsQuestLodger())
            yield return new FloatMenuOption("CannotUseReason".Translate("CryptosleepCasketGuestsNotAllowed".Translate()), null);

        foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(myPawn))
            yield return floatMenuOption;

        if (innerContainer.Count != 0)
            yield break;

        if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);

        JobDef jobDef = USH_DefOf.USH_EnterBiocoder;
        string label = "USH_GE_EnterBiocoder".Translate();

        void CreateJobAction() => myPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef, this), JobTag.Misc);

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, CreateJobAction), myPawn, this);
    }

    public static Building_Biocoder FindBiocoderFor(Pawn p, Pawn traveler, bool ignoreOtherReservations = false)
    {
        bool queuing = KeyBindingDefOf.QueueOrder.IsDownEvent;

        Building_Biocoder biocoder = (Building_Biocoder)GenClosest.ClosestThingReachable(
            p.PositionHeld,
            p.MapHeld,
            ThingRequest.ForDef(USH_DefOf.USH_GlittertechTargeter),
            PathEndMode.InteractionCell,
            TraverseParms.For(traveler),
            9999f,
            Validator);

        if (biocoder != null)
            return biocoder;

        bool Validator(Thing x)
        {
            if (!((Building_Biocoder)x).HasAnyContents && (!queuing || !traveler.HasReserved(x)))
                return traveler.CanReserve(x, 1, -1, null, ignoreOtherReservations);

            return false;
        }

        return null;
    }

    private void ChangeGoodwillNearestFactions(Map map, int count, int goodwillChange)
    {
        if (map == null)
            return;

        List<Settlement> settlements = [.. Find.WorldObjects.Settlements
            .Where(s => s.Faction != null
                        && !s.Faction.IsPlayer
                        && !s.Faction.def.hidden
                        && !s.Faction.defeated)
            .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(map.Tile, s.Tile))];

        HashSet<Faction> affectedFactions = [];
        int affected = 0;

        foreach (Settlement settlement in settlements)
        {
            Faction faction = settlement.Faction;
            if (affectedFactions.Contains(faction))
                continue;

            if (!faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwillChange, canSendMessage: true, true, USH_DefOf.USH_UsedTargeter))
                continue;

            affectedFactions.Add(faction);
            affected++;

            if (affected >= count)
                break;
        }
    }


    //disable rendering of the turret
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {

    }
}
