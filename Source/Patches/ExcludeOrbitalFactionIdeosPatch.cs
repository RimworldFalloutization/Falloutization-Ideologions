using HarmonyLib;

namespace Falloutization.Ideologions.Patches;

/// <summary>
/// Factionless pawns pick an existing world ideoligion via IdeoChangeToWeight.
/// Traders Guild and Salvagers ideos sit in that pool; exclude them so joiners/guests
/// can still receive any other faction ideo.
/// </summary>
[HarmonyPatch(typeof(IdeoUtility), nameof(IdeoUtility.IdeoChangeToWeight))]
public static class ExcludeOrbitalFactionIdeosPatch
{
    private const string TradersGuildDefName = "TradersGuild";
    private const string SalvagersDefName = "Salvagers";

    private static FactionDef tradersGuildDef;
    private static FactionDef salvagersDef;
    private static bool defsResolved;

    public static void Postfix(Ideo ideo, ref float __result)
    {
        if (__result <= 0f || ideo == null || Current.Game == null)
        {
            return;
        }

        ResolveDefs();
        if (tradersGuildDef == null && salvagersDef == null)
        {
            return;
        }

        List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
        for (int i = 0; i < factions.Count; i++)
        {
            Faction faction = factions[i];
            if (faction.def != tradersGuildDef && faction.def != salvagersDef)
            {
                continue;
            }

            if (faction.ideos != null && faction.ideos.IsPrimary(ideo))
            {
                __result = 0f;
                return;
            }
        }
    }

    private static void ResolveDefs()
    {
        if (defsResolved)
        {
            return;
        }

        tradersGuildDef = DefDatabase<FactionDef>.GetNamedSilentFail(TradersGuildDefName);
        salvagersDef = DefDatabase<FactionDef>.GetNamedSilentFail(SalvagersDefName);
        defsResolved = true;
    }
}
