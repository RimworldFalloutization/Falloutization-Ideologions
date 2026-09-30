using System.Reflection;
using HarmonyLib;

namespace Falloutization.Ideologions.Patches;

/// <summary>
/// Vanilla Ideology Expanded adds a hidden "conversion ritual (leader)" to every ideoligion.
/// Its start check only requires Exalted Priesthood when the vanilla Leader precept exists.
/// With that precept removed, the Administrator's Leader tag lets them start it anyway.
/// </summary>
internal static class LeaderConversionMemePatch
{
    private const string WorkerTypeName = "VanillaMemesExpanded.RitualBehaviorWorker_LeaderConversion";
    private const string MemeDefName = "VME_ExaltedPriesthood";

    public static void Apply(Harmony harmony)
    {
        System.Type workerType = AccessTools.TypeByName(WorkerTypeName);
        if (workerType == null)
        {
            return;
        }

        MethodInfo method = AccessTools.Method(workerType, "CanStartRitualNow");
        if (method == null)
        {
            return;
        }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(LeaderConversionMemePatch), nameof(Postfix)));
    }

    public static void Postfix(Precept_Ritual ritual, ref string __result)
    {
        if (!__result.NullOrEmpty() || ritual?.ideo == null)
        {
            return;
        }

        MemeDef meme = DefDatabase<MemeDef>.GetNamedSilentFail(MemeDefName);
        if (meme == null || ritual.ideo.HasMeme(meme))
        {
            return;
        }

        __result = "VME_NeedsExaltedPriesthood".Translate(ritual.ideo.name);
    }
}
