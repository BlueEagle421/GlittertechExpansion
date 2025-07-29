using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

public static class NeutroamineRecipeDefGenerator
{
    public static IEnumerable<RecipeDef> ImpliedRecipeDefs(bool hotReload = false)
    {
        foreach (RecipeDef item in BeginRecipesGeneration(hotReload))
            yield return item;
    }

    private static IEnumerable<RecipeDef> BeginRecipesGeneration(bool hotReload = false)
    {
        List<RecipeDef> result = [];

        try
        {
            result = DefsFromNeutroamineItems(NeutroamineRecipes, hotReload);
        }
        catch (Exception ex)
        {
            Log.Warning($"[Glittertech Expansion] unexpected error in NeutroamineRecipeDefGenerator: {ex}");
        }

        foreach (var recipeDef in result)
            yield return recipeDef;
    }

    private static List<RecipeDef> DefsFromNeutroamineItems(List<RecipeDef> recipes, bool hotReload = false)
    {
        List<RecipeDef> result = [];

        foreach (var recipe in recipes)
        {
            var report = CanGenerateFromRecipe(recipe, out var product);

            if (!report)
            {
                Log.Warning($"[Glittertech Expansion] Recipe '{recipe.defName}' {report.Reason}");
                continue;
            }

            try
            {
                result.Add(CreateRecipeDefFromNeutroamineItem(product.thingDef, recipe, hotReload));
            }
            catch (Exception e)
            {
                Log.Warning($"[Glittertech Expansion] Failed to patch ThingDef '{product.thingDef.defName}': {e}");
            }
        }

        return result;
    }

    private static AcceptanceReport CanGenerateFromRecipe(RecipeDef recipe, out ThingDefCountClass product)
    {
        product = null;

        if (recipe.products.NullOrEmpty())
            return "has no products";

        product = recipe.products[0];

        if (product.thingDef == null)
            return "has a null product or thingDef";

        return true;
    }

    private static RecipeDef CreateRecipeDefFromNeutroamineItem(ThingDef def, RecipeDef originalRecipe, bool hotReload = false)
    {
        int originalCount = originalRecipe.products[0].count;

        string defName = "USH_ExtractFrom_" + def.defName + originalCount;
        if (originalRecipe.adjustedCount > 1)
            defName += $"{originalRecipe.adjustedCount}";

        RecipeDef recipeDef = hotReload ? (DefDatabase<RecipeDef>.GetNamed(defName, errorOnFail: false) ?? new RecipeDef()) : new RecipeDef();
        recipeDef.defName = defName;

        recipeDef.label = GetRecipeLabel(def, originalCount);

        recipeDef.jobString = $"Extracting neutroamine from {def.label}";
        recipeDef.modContentPack = USH_DefOf.USH_GlittertechFabrication.modContentPack;

        SetProductsAndIngredients(
            def,
            originalCount,
            originalRecipe,
            ref recipeDef,
            out int countToExtract);

        int workPerNeutroamine = 720;
        recipeDef.workAmount = countToExtract * workPerNeutroamine;
        recipeDef.workSpeedStat = USH_DefOf.DrugSynthesisSpeed;

        recipeDef.skillRequirements =
        [
            new SkillRequirement() { skill = SkillDefOf.Intellectual, minLevel = 4 },
            new SkillRequirement() { skill = SkillDefOf.Crafting, minLevel = 4 },
        ];

        recipeDef.workSkill = SkillDefOf.Intellectual;

        recipeDef.recipeUsers = [USH_DefOf.USH_NeutroamineExtractor];

        recipeDef.effectWorking = USH_DefOf.Cook;
        recipeDef.soundWorking = USH_DefOf.Recipe_Drug;

        recipeDef.researchPrerequisites = [USH_DefOf.USH_GlittertechUtilitiesRes];

        recipeDef.description = $"Extract x{countToExtract} neutroamine from {def.label} x{originalCount}";
        recipeDef.descriptionHyperlinks = [USH_DefOf.Neutroamine];

        return recipeDef;
    }

    private static string GetRecipeLabel(ThingDef def, int originalCount)
    {
        string recipeLabel = $"Extract neutroamine from {def.label}";
        if (originalCount > 1)
            recipeLabel += $" x{originalCount}";

        return recipeLabel;
    }

    private static void SetProductsAndIngredients(
        ThingDef def,
        int originalCount,
        RecipeDef originalRecipe,
        ref RecipeDef toModify,
        out int countToExtract)
    {
        var originalIngredient = originalRecipe.ingredients.Find(x => FilterContainsNeutroamine(x.filter));
        countToExtract = (int)originalIngredient.GetBaseCount();

        toModify.products = [new ThingDefCountClass() { thingDef = USH_DefOf.Neutroamine, count = countToExtract }];

        toModify.ingredients = [ForThingDef(def, originalCount)];
    }

    private static readonly ConstructorInfo _ctor = AccessTools.Constructor(typeof(IngredientCount));
    private static readonly MethodInfo _setBaseCount = AccessTools.Method(typeof(IngredientCount), "SetBaseCount");

    public static IngredientCount ForThingDef(ThingDef thingDef, int count)
    {
        if (thingDef == null)
            throw new ArgumentNullException(nameof(thingDef));

        var ingredientCount = (IngredientCount)_ctor.Invoke([]);

        _setBaseCount.Invoke(ingredientCount, [count]);

        ingredientCount.filter.SetAllow(thingDef, true);

        ingredientCount.ResolveReferences();

        return ingredientCount;
    }

    private static List<RecipeDef> NeutroamineRecipes
    => [.. DefDatabase<RecipeDef>.AllDefs
            .Where(x =>
                !x.products.NullOrEmpty() &&
                x.ingredients.Any(x => FilterContainsNeutroamine(x.filter)))];

    private static bool FilterContainsNeutroamine(ThingFilter filter)
    {
        if (filter.AllowedThingDefs.Contains(USH_DefOf.Neutroamine))
            return true;

        if (filter.ToString() == "neutroamine")
            return true;

        return false;
    }
}
