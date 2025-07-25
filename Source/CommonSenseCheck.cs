using System;
using System.Reflection;
using HarmonyLib;

public static class CommonSenseCheck
{
    private static bool _assemblyPresent;
    private static Type _settingsType;
    private static FieldInfo _advHaulField;
    public static void CheckForAssemblyPresence()
    {
        Type type = Type.GetType("CommonSense.CommonSense, CommonSense");
        _assemblyPresent = type != null;

        if (!_assemblyPresent)
            return;

        _settingsType = AccessTools.TypeByName("CommonSense.Settings");

        _advHaulField = _settingsType.GetField("adv_haul_all_ings", BindingFlags.Static | BindingFlags.Public);
    }

    public static bool CheckForSetting()
    {
        if (!_assemblyPresent)
            return false;

        if (_advHaulField == null)
            return false;

        return (bool)_advHaulField.GetValue(null);
    }

    public const string MESSAGE_CONTENT
        = "The Glittertech Fabricator is currently incompatible with the Common Sense mod, specifically the setting “Pawns are encouraged to pick up all ingredients before hauling them to the crafting place”. This causes a nasty bug that I'm actively working to fix.\nIn the meantime, please consider disabling that option to enjoy a more balanced experience.Thanks for your patience!\nBest regards,\nmod author";
}