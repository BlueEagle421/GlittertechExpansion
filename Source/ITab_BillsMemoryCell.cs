using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_GE
{
	public class ITab_MemoryCellMods : ITab
	{
		private float viewHeight = 1000f;
		private Vector2 scrollPosition;
		private Bill mouseoverBill;

		private static readonly Vector2 WinSize = new(420f, 480f);

		[TweakValue("Interface", 0f, 128f)]
		private static float PasteX = 48f;

		[TweakValue("Interface", 0f, 128f)]
		private static float PasteY = 3f;

		[TweakValue("Interface", 0f, 32f)]
		private static float PasteSize = 24f;

		protected MemoryCell SelCell => (MemoryCell)SelThing;

		public ITab_MemoryCellMods()
		{
			size = WinSize;
			labelKey = "USH_GE_ModsTab";
			tutorTag = "Bills";
		}

		protected override void FillTab()
		{
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);

			DrawPasteButtonIfNeeded();

			var contentRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

			mouseoverBill = SelCell.BillStack.DoListing(contentRect, BuildRecipeOptions, ref scrollPosition, ref viewHeight);
		}

		public override void TabUpdate()
		{
			if (mouseoverBill == null)
				return;

			mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelCell.Position);
			mouseoverBill = null;
		}


		private void DrawPasteButtonIfNeeded()
		{
			var pasteRect = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
			var clipboard = BillUtility.Clipboard;
			if (clipboard == null)
				return;

			if (!IsClipboardRecipeAvailableHere(clipboard))
			{
				DrawDisabledPaste(pasteRect);

				if (Mouse.IsOver(pasteRect))
					TooltipHandler.TipRegion(pasteRect, "ClipboardBillNotAvailableHere".Translate() + ": " + clipboard.LabelCap);

				return;
			}

			if (SelCell.BillStack.Count >= MaxBillsInStack)
			{
				DrawDisabledPaste(pasteRect);
				if (Mouse.IsOver(pasteRect))
				{
					TooltipHandler.TipRegion(
						pasteRect,
						"PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + "): " + clipboard.LabelCap);
				}

				return;
			}


			if (Widgets.ButtonImageFitted(pasteRect, TexButton.Paste, Color.white))
				PasteClipboardBill(clipboard);

			if (Mouse.IsOver(pasteRect))
				TooltipHandler.TipRegion(pasteRect, "PasteBillTip".Translate() + ": " + clipboard.LabelCap);
		}

		private const int MaxBillsInStack = 15;

		private bool IsClipboardRecipeAvailableHere(Bill clipboard)
		{
			if (clipboard?.recipe == null) return false;

			var recipe = clipboard.recipe;

			return SelCell.def.AllRecipes.Contains(recipe)
				&& recipe.AvailableNow
				&& recipe.AvailableOnNow(SelCell);
		}

		private void DrawDisabledPaste(Rect rect)
		{
			GUI.color = Color.gray;
			Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
			GUI.color = Color.white;
		}

		private void PasteClipboardBill(Bill clipboard)
		{
			var bill = clipboard.Clone();
			bill.InitializeAfterClone();
			SelCell.BillStack.AddBill(bill);
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}


		private List<FloatMenuOption> BuildRecipeOptions()
		{
			var options = new List<FloatMenuOption>();

			var recipes = SelCell.def.AllRecipes;
			for (int i = 0; i < recipes.Count; i++)
			{
				var recipe = recipes[i];
				if (!IsRecipeAvailable(recipe))
					continue;

				AddOptionsForRecipe(recipe, i, options);
			}

			if (!options.Any())
				options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));

			return options;
		}

		private bool IsRecipeAvailable(RecipeDef recipe)
			=> recipe.AvailableNow && recipe.AvailableOnNow(SelCell);

		private void AddOptionsForRecipe(RecipeDef recipe, int recipeIndex, List<FloatMenuOption> options)
		{
			CreateAndAddOptionForRecipe(recipe, recipeIndex, null, options);

			foreach (var ideo in Faction.OfPlayer.ideos.AllIdeos)
				foreach (var cached in ideo.cachedPossibleBuildings)
					if (cached.ThingDef == recipe.ProducedThingDef)
						CreateAndAddOptionForRecipe(recipe, recipeIndex, cached, options);

		}

		private void CreateAndAddOptionForRecipe(RecipeDef recipe, int recipeIndex, Precept_ThingStyle preceptThingStyle, List<FloatMenuOption> options)
		{
			string label = preceptThingStyle != null
				? "RecipeMake".Translate(preceptThingStyle.LabelCap).CapitalizeFirst()
				: recipe.LabelCap;

			int capturedIndex = recipeIndex;
			var capturedRecipe = recipe;
			var capturedPrecept = preceptThingStyle;
			string capturedLabel = label;

			void onChosen()
			{
				if (ModsConfig.BiotechActive && capturedRecipe.mechanitorOnlyRecipe && !SelCell.Map.mapPawns.FreeColonists.Any(MechanitorUtility.IsMechanitor))
				{
					Find.WindowStack.Add(new Dialog_MessageBox("RecipeRequiresMechanitor".Translate(capturedRecipe.LabelCap)));
					return;
				}

				if (!SelCell.Map.mapPawns.FreeColonists.Any(col => capturedRecipe.PawnSatisfiesSkillRequirements(col)))
				{
					Bill.CreateNoPawnsWithSkillDialog(capturedRecipe);
					return;
				}

				Bill newBill = capturedRecipe.MakeNewBill(capturedPrecept);
				SelCell.BillStack.AddBill(newBill);

				if (capturedRecipe.conceptLearned != null)
					PlayerKnowledgeDatabase.KnowledgeDemonstrated(capturedRecipe.conceptLearned, KnowledgeAmount.Total);

				if (TutorSystem.TutorialMode)
					TutorSystem.Notify_Event("AddBill-" + capturedRecipe.LabelCap.Resolve());
			}

			void mouseoverGuiAction(Rect rect)
				=> BillUtility.DoBillInfoWindow(capturedIndex, capturedLabel, rect, capturedRecipe);


			var opt = new FloatMenuOption(
				label: capturedLabel,
				action: onChosen,
				iconTex: recipe.UIIcon,
				shownItemForIcon: recipe.UIIconThing,
				thingStyle: null,
				forceBasicStyle: false,
				priority: MenuOptionPriority.Default,
				mouseoverGuiAction: mouseoverGuiAction,
				revalidateClickTarget: null,
				extraPartWidth: 29f,
				extraPartOnGUI: r => Widgets.InfoCardButton(r.x + 5f, r.y + (r.height - 24f) / 2f, recipe, preceptThingStyle),
				revalidateWorldClickTarget: null,
				playSelectionSound: true,
				orderInPriority: -recipe.displayPriority
			);

			options.Add(opt);
		}

	}
}
