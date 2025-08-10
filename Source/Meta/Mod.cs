using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace USH_GE;

public class GE_Mod : Mod
{
    public static GE_Settings Settings { get; private set; }

    public const string THIRD_PARTY_MOD_MSG = "Most likely caused by other third-party mods, check the exception stacktrace to detect which one";

    public GE_Mod(ModContentPack content) : base(content)
    {
        InitHarmony();

        Settings = GetSettings<GE_Settings>();
    }

    private void InitHarmony()
    {
        Harmony harmony = new("GlittertechExpansion");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Log.Message("The almighty power of Harmony has been initialized by the humble mod creator BlueEagle421".Colorize(Color.cyan));
    }

    public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);

    public override string SettingsCategory() => "Glittertech Expansion";
}