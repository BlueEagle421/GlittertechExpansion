using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace USH_GE;

public class Hediff_MemoryCasing : Hediff_Implant, IThingHolder, ISearchableContents, IMemoryCellHolder
{
    public ThingOwner innerContainer;
    public Thing ContainedThing
    {
        get
        {
            if (!innerContainer.Any)
                return null;

            return innerContainer[0];
        }
    }
    public IThingHolder ParentHolder => pawn;
    public ThingOwner SearchableContents => innerContainer;
    public Thing SourceThing => pawn;
    public IntVec3 InsertPos => pawn.Position;
    public MemoryCell ContainedCell => ContainedThing as MemoryCell;
    public ThingOwner MemoryCellOwner => innerContainer;

    private int _deltaTicksPassed;
    private const int TICK_RARE = 250;

    public Hediff_MemoryCasing()
    {
        innerContainer = new ThingOwner<Thing>(this);
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
        => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public ThingOwner GetDirectlyHeldThings()
        => innerContainer;

    public AcceptanceReport CanInsertCell(MemoryCell memoryCell)
    {
        if (ContainedThing != null)
            return "USH_GE_ContainerFull".Translate(this.Named("BUILDING"));

        return true;
    }

    public void Notify_CellInserted(MemoryCell memoryCell, Pawn doer)
    {
        USH_DefOf.USH_Insert?.PlayOneShot(SoundInfo.InMap(pawn));
    }

    public override string Description
    {
        get
        {
            StringBuilder sb = new();

            sb.AppendLine();
            sb.AppendLine("Contents".Translate() + ": " + (ContainedCell == null ? ((string)"Nothing".Translate()) : ContainedCell.LabelCap));

            if (ContainedCell != null)
            {
                string expireTime = ((int)ContainedCell.ExpireTicksLeft).ToStringTicksToPeriod();

                sb.AppendLine("USH_GE_ExpiresIn".Translate() + ": " + expireTime);
                sb.AppendLine(ContainedCell.MemoryCellData.GetInspectString());
            }

            return base.Description + '\n' + sb.ToString();
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        _deltaTicksPassed += delta;

        if (_deltaTicksPassed >= TICK_RARE)
        {
            ContainedCell?.TickRare();
            _deltaTicksPassed = 0;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {


        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;

        Command_Action ejectCommand = new()
        {
            action = EjectContent,
            defaultLabel = "USH_GE_CommandMemoryContainerEject".Translate(),
            defaultDesc = "USH_GE_CommandMemoryContainerEjectDesc".Translate(),
            hotKey = KeyBindingDefOf.Misc8,
            icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectMemoryCell")
        };

        if (ContainedThing == null)
            ejectCommand.Disable("USH_GE_CommandMemoryContainerEjectFailEmpty".Translate());

        yield return ejectCommand;
    }

    public void EjectContent()
    {
        USH_DefOf.USH_Eject?.PlayOneShot(SoundInfo.InMap(pawn));
        innerContainer.TryDropAll(pawn.Position, pawn.Map, ThingPlaceMode.Near);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
    }
}
