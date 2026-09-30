using System.Reflection;
using HarmonyLib;

namespace Falloutization.Ideologions;

[StaticConstructorOnStartup]
internal static class HarmonyInit
{
    static HarmonyInit()
    {
        new Harmony("Falloutization.Ideologions").PatchAll(Assembly.GetExecutingAssembly());
    }
}
