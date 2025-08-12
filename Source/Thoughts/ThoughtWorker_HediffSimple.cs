using RimWorld;
using Verse;

namespace USH_GE;

public class ThoughtWorker_HediffSimple : ThoughtWorker
{
	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		Hediff firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(def.hediff);

		if (firstHediffOfDef?.def.stages == null)
			return ThoughtState.Inactive;

		return ThoughtState.ActiveDefault;
	}

	public override string PostProcessDescription(Pawn p, string description)
	{
		string text = base.PostProcessDescription(p, description);
		Hediff firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(def.hediff);
		if (firstHediffOfDef == null || !firstHediffOfDef.Visible)
		{
			return text;
		}
		return text + "\n\n" + "CausedBy".Translate() + ": " + firstHediffOfDef.LabelBase.CapitalizeFirst();
	}
}
