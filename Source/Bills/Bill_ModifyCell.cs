using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace USH_GE;

public class ModExtension_UseModifyCellBill : DefModExtension
{
    public string label;
    public int maxCount = -1;
    public float multiplyMoodBy = 1f;
    public float addExpireTime = 0;
    public float setExpireTimeMultiplier = -1f;

    public virtual void Notify_InstalledOnCell(MemoryCell memoryCell)
    {
        memoryCell.MemoryCellData.moodOffset =
            (int)Mathf.Round(memoryCell.MemoryCellData.moodOffset * multiplyMoodBy);

        memoryCell.ExpireTicksLeft += addExpireTime;

        if (!Mathf.Approximately(setExpireTimeMultiplier, -1f))
            memoryCell.ExpireTimeMultiplier = setExpireTimeMultiplier;

        memoryCell.Notify_ModInstalled(this);
    }
}

public class Bill_ModifyCell : Bill_Production
{
    public MemoryCell MemoryCell => (MemoryCell)billStack.billGiver;
    public ModExtension_UseModifyCellBill CellExt;

    public Bill_ModifyCell() { }
    public Bill_ModifyCell(RecipeDef recipe, Precept_ThingStyle precept = null) : base(recipe, precept)
        => CellExt = recipe.GetModExtension<ModExtension_UseModifyCellBill>();

    public override void Notify_BillWorkFinished(Pawn billDoer)
    {
        base.Notify_BillWorkFinished(billDoer);

        CellExt.Notify_InstalledOnCell(MemoryCell);
    }

    public override Bill Clone()
    {
        Bill_ModifyCell obj = (Bill_ModifyCell)base.Clone();
        obj.CellExt = CellExt;

        return obj;
    }

    public override bool PawnAllowedToStartAnew(Pawn p)
    {
        if (MemoryCell.GetInstalledModCount(CellExt) >= CellExt.maxCount)
        {
            JobFailReason.Is("Maximum modifiers of this type reached".Translate());
            return false;
        }

        return base.PawnAllowedToStartAnew(p);
    }

    protected override string StatusString
    {
        get
        {
            var installed = MemoryCell.GetInstalledModCount(CellExt);
            var maxSuffix = CellExt.maxCount == -1 ? ")" : $"/{CellExt.maxCount})";

            return $"(present in memory: {installed}{maxSuffix}";
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            CellExt = recipe.GetModExtension<ModExtension_UseModifyCellBill>();
    }
}