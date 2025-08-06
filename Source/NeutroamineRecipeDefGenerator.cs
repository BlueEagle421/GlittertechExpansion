using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace USH_GE;

public static class NeutroamineRecipeDefGenerator
{
    private static readonly HashSet<string> _omittedDefNames = [];
    private static readonly HashSet<string> _addedRecipesDefNames = [];
    private static Regex _disallowedCharRegex;
    public static IEnumerable<RecipeDef> ImpliedRecipeDefs(bool hotReload = false)
    {
        foreach (RecipeDef item in BeginRecipesGeneration(hotReload))
            yield return item;

        if (!_omittedDefNames.NullOrEmpty())
            Log.Message("[Glittertech Expansion] Recipe defs omitted for neutroamine extraction: " + string.Join(", ", _omittedDefNames));
    }

    private static IEnumerable<RecipeDef> BeginRecipesGeneration(bool hotReload = false)
    {
        GetDisallowedChars();

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

    private static void GetDisallowedChars()
    {
        try
        {
            var fi = AccessTools.Field(typeof(Def), "DisallowedLabelCharsRegex");
            _disallowedCharRegex = (Regex)fi.GetValue(null);
        }
        catch (Exception ex)
        {
            Log.Warning($"[Glittertech Expansion] failed to load DisallowedLabelCharsRegex: {ex}");
        }
    }

    private static List<RecipeDef> DefsFromNeutroamineItems(List<RecipeDef> recipes, bool hotReload = false)
    {
        List<RecipeDef> result = [];

        foreach (var recipe in recipes)
        {
            if (!CanGenerateFromRecipe(recipe, out var product))
            {
                _omittedDefNames.Add(recipe.defName);
                continue;
            }

            try
            {
                var toAdd = CreateRecipeDefFromNeutroamineItem(product.thingDef, recipe, hotReload);

                if (toAdd != null)
                    result.Add(toAdd);
            }
            catch
            {
                _omittedDefNames.Add(recipe.defName);
            }
        }

        return result;
    }

    private static bool CanGenerateFromRecipe(RecipeDef recipe, out ThingDefCountClass product)
    {
        product = null;

        if (recipe.products.NullOrEmpty())
            return false;

        product = recipe.products[0];

        if (product.thingDef == null)
            return false;

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

        int ingredientsCount = GetIngredientsCount(originalCount);

        SetProductsAndIngredients(
            def,
            ingredientsCount,
            originalRecipe,
            ref recipeDef,
            out int countToExtract);

        recipeDef.label = GetRecipeLabel(def, ingredientsCount, countToExtract);

        if (_disallowedCharRegex.IsMatch(recipeDef.label))
            return null;

        if (_addedRecipesDefNames.Contains(recipeDef.defName))
            return null;

        recipeDef.jobString = "USH_GE_NeutroamineJobString".Translate(def.label);
        recipeDef.modContentPack = USH_DefOf.USH_GlittertechFabrication.modContentPack;

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

        recipeDef.description = "USH_GE_NeutroamineRecipeDesc".Translate(countToExtract, originalCount, def.label);
        recipeDef.descriptionHyperlinks = [USH_DefOf.Neutroamine];

        _addedRecipesDefNames.Add(recipeDef.defName);
        return recipeDef;
    }

    private static int GetIngredientsCount(int originalCount)
    {
        int ingredientCount = originalCount;

        if (GE_Mod.Settings.DoubleNeutroamineCost.Value)
            ingredientCount *= 2;

        return ingredientCount;
    }

    private static string GetRecipeLabel(ThingDef def, int ingredientCount, int toExtract)
    {
        string recipeLabel = "USH_GE_NeutroamineRecipeLabel".Translate(def.label, toExtract);

        if (ingredientCount > 1)
            recipeLabel += $" x{ingredientCount}";

        return recipeLabel;
    }

    private static void SetProductsAndIngredients(
        ThingDef def,
        int ingredientCount,
        RecipeDef originalRecipe,
        ref RecipeDef toModify,
        out int countToExtract)
    {
        var originalIngredient = originalRecipe.ingredients.Find(x => FilterContainsNeutroamine(x.filter));
        countToExtract = (int)originalIngredient.GetBaseCount();

        toModify.products = [new ThingDefCountClass() { thingDef = USH_DefOf.Neutroamine, count = countToExtract }];

        toModify.ingredients = [ForThingDef(def, ingredientCount)];
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
