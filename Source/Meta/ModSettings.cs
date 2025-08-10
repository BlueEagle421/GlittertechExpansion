using RimWorld;
using UnityEngine;
using Verse;

namespace USH_GE;

public class GE_Settings : ModSettings
{
    public Setting<float> FormingSpeedMultiplier = new(1f);
    public Setting<float> PositiveMoodMultiplier = new(0.5f);
    public Setting<float> NegativeMoodMultiplier = new(1f);
    public Setting<float> PylonMoodMultiplier = new(0.25f);
    public Setting<bool> DoubleNeutroamineCost = new(false);
    public Setting<bool> ChangeSkinColor = new(true);

    private static Vector2 _scrollPosition = new(0f, 0f);
    private static float _totalContentHeight = 1000f;
    private const float SCROLL_BAR_WIDTH_MARGIN = 18f;

    public void ResetAll()
    {
        FormingSpeedMultiplier.ToDefault();
        PositiveMoodMultiplier.ToDefault();
        NegativeMoodMultiplier.ToDefault();
        PylonMoodMultiplier.ToDefault();
        DoubleNeutroamineCost.ToDefault();
        ChangeSkinColor.ToDefault();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        FormingSpeedMultiplier.ExposeData(nameof(FormingSpeedMultiplier));
        PositiveMoodMultiplier.ExposeData(nameof(PositiveMoodMultiplier));
        NegativeMoodMultiplier.ExposeData(nameof(NegativeMoodMultiplier));
        PylonMoodMultiplier.ExposeData(nameof(PylonMoodMultiplier));
        DoubleNeutroamineCost.ExposeData(nameof(DoubleNeutroamineCost));
        ChangeSkinColor.ExposeData(nameof(ChangeSkinColor));
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect outerRect = inRect.ContractedBy(10f);

        bool scrollBarVisible = _totalContentHeight > outerRect.height;
        var scrollViewTotal = new Rect(0f, 0f, outerRect.width - (scrollBarVisible ? SCROLL_BAR_WIDTH_MARGIN : 0), _totalContentHeight);
        Widgets.BeginScrollView(outerRect, ref _scrollPosition, scrollViewTotal);

        Listing_Standard listingStandard = new();
        listingStandard.Begin(new Rect(0f, 0f, scrollViewTotal.width, 9999f));

        //FormingSpeedMultiplier
        listingStandard.Label("USH_GE_FormingMultiplierSetting".Translate().Colorize(Color.cyan));
        float formingSliderValue = listingStandard.Slider(FormingSpeedMultiplier.Value, 0.25f, 3f);
        listingStandard.Label("USH_GE_FormingMultiplierSettingDesc".Translate(formingSliderValue.ToStringPercent()));
        FormingSpeedMultiplier.Value = formingSliderValue;

        //PositiveMoodMultiplier
        listingStandard.Label("\n");
        listingStandard.Label("USH_GE_PositiveMultiplierSetting".Translate().Colorize(Color.cyan));
        float positiveSliderValue = listingStandard.Slider(PositiveMoodMultiplier.Value, 0.25f, 1f);
        listingStandard.Label("USH_GE_PositiveMultiplierSettingDesc".Translate(positiveSliderValue.ToStringPercent()));
        PositiveMoodMultiplier.Value = positiveSliderValue;

        //NegativeMoodMultiplier
        listingStandard.Label("\n");
        listingStandard.Label("USH_GE_NegativeMultiplierSetting".Translate().Colorize(Color.cyan));
        float negativeSliderValue = listingStandard.Slider(NegativeMoodMultiplier.Value, 0.5f, 2f);
        listingStandard.Label("USH_GE_NegativeMultiplierSettingDesc".Translate(negativeSliderValue.ToStringPercent()));
        NegativeMoodMultiplier.Value = negativeSliderValue;

        //PylonMoodMultiplier
        listingStandard.Label("\n");
        listingStandard.Label("USH_GE_PylonMultiplierSetting".Translate().Colorize(Color.cyan));
        float pylonSliderValue = listingStandard.Slider(PylonMoodMultiplier.Value, 0.1f, 1f);
        listingStandard.Label("USH_GE_PylonMultiplierSettingDesc".Translate(pylonSliderValue.ToStringPercent()));
        PylonMoodMultiplier.Value = pylonSliderValue;

        //DoubleNeutroamineCost
        listingStandard.Label("\n");
        listingStandard.CheckboxLabeled("USH_GE_NeutroamineSetting".Translate().Colorize(Color.cyan), ref DoubleNeutroamineCost.Value);
        listingStandard.Label("USH_GE_NeutroamineSettingDesc".Translate());

        //ChangeSkinColor
        listingStandard.Label("\n");
        listingStandard.CheckboxLabeled("USH_GE_SkinSetting".Translate().Colorize(Color.cyan), ref ChangeSkinColor.Value);
        listingStandard.Label("USH_GE_SkinSettingDesc".Translate());

        //Reset button
        listingStandard.Label("\n");
        bool shouldReset = listingStandard.ButtonText("USH_GE_ResetSettings".Translate());
        if (shouldReset) ResetAll();
        listingStandard.Label("\n");

        //End
        listingStandard.End();
        _totalContentHeight = listingStandard.CurHeight + 10f;
        Widgets.EndScrollView();
    }

    public class Setting<T>(T defaultValue)
    {
        public T Value = defaultValue;
        public T DefaultValue { get; private set; } = defaultValue;

        public void ToDefault() => Value = DefaultValue;
        public void ExposeData(string key) => Scribe_Values.Look(ref Value, $"USH_{key}", DefaultValue);
    }
}