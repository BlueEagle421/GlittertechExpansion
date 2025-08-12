using Verse;

namespace USH_GE;

public interface IMemoryCellHolder
{
    public AcceptanceReport CanInsertCell(MemoryCell memoryCell);
    public void Notify_CellInserted(MemoryCell memoryCell, Pawn doer);
    public Thing SourceThing { get; }
    public IntVec3 InsertPos { get; }
    public MemoryCell ContainedCell { get; }
    public ThingOwner MemoryCellOwner { get; }
}