using Verse;
using RimWorld;
using UnityEngine;

namespace USH_GE;

public class ModExtension_UseModifyCellBill : DefModExtension
{
    public string label;
    public int maxCount = -1;
    public float multiplyMoodBy = 1f;
    public float addExpireTime = 0;

    public virtual void Notify_InstalledOnCell(MemoryCell memoryCell)
    {
        memoryCell.MemoryCellData.moodOffset =
            (int)Mathf.Round(memoryCell.MemoryCellData.moodOffset * multiplyMoodBy);

        memoryCell.ExpireTicksLeft += addExpireTime;
    }
}

public class Bill_ModifyCell(RecipeDef recipe, Precept_ThingStyle precept = null) : Bill_Production(recipe, precept)
{
    public MemoryCell MemoryCell => (MemoryCell)billStack.billGiver;
    public ModExtension_UseModifyCellBill CellExt = recipe.GetModExtension<ModExtension_UseModifyCellBill>();


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

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            CellExt = recipe.GetModExtension<ModExtension_UseModifyCellBill>();
    }
}