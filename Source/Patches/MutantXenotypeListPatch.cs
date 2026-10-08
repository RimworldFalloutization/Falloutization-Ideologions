using System.Reflection;
using HarmonyLib;

namespace Falloutization.Ideologions.Patches;

/// <summary>
/// Greenway's mutant precepts only recognize a hardcoded xenotype list.
/// WestTek ships SuperMutantFirst/Second (not SuperMutant), and FCP ghoul
/// variants beyond the base xenotype were omitted.
/// </summary>
internal static class MutantXenotypeListPatch
{
    private static readonly string[] ExtraMutantXenotypes =
    {
        "WestTek_Xenotype_SuperMutantFirst",
        "WestTek_Xenotype_SuperMutantSecond",
        "FCP_Xenotype_Ghoul_Feral",
        "FCP_Xenotype_Ghoul_Glowing_One",
        "FCP_Xenotype_Ghoul_GlowingOne_Feral",
    };

    public static void Apply()
    {
        Type utilityType = AccessTools.TypeByName("FIP.Greenway.MutantPolicyUtility");
        if (utilityType == null)
        {
            return;
        }

        FieldInfo field = AccessTools.Field(utilityType, "MutantXenotypeDefNames");
        if (field?.GetValue(null) is not HashSet<string> names)
        {
            return;
        }

        foreach (string defName in ExtraMutantXenotypes)
        {
            names.Add(defName);
        }
    }
}
