using System.Reflection;
using Falloutization.Ideologions.Patches;
using HarmonyLib;

namespace Falloutization.Ideologions;

[StaticConstructorOnStartup]
internal static class HarmonyInit
{
    static HarmonyInit()
    {
        Harmony harmony = new Harmony("Falloutization.Ideologions");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        LeaderConversionMemePatch.Apply(harmony);
    }
}
