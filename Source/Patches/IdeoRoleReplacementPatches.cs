using HarmonyLib;

namespace Falloutization.Ideologions.Patches;

/// <summary>
/// Universal Compassion requires Administrator, and that role shares the IdeoRole issue
/// with every other role. Vanilla treats a single-precept requireOne group as a replacement
/// for the whole issue, which would delete the Moral Guide and specialists.
/// </summary>
[HarmonyPatch(typeof(IdeoFoundation), "ConflictsWithNewMemes")]
public static class IdeoFoundation_ConflictsWithNewMemes_Patch
{
    public static void Postfix(Precept precept, List<MemeDef> newMemes, ref bool __result)
    {
        if (!__result || precept?.def == null || newMemes == null)
        {
            return;
        }

        if (ConflictsWithoutAdministratorGroup(precept, newMemes))
        {
            return;
        }

        __result = false;
    }

    private static bool ConflictsWithoutAdministratorGroup(Precept precept, List<MemeDef> newMemes)
    {
        for (int i = 0; i < newMemes.Count; i++)
        {
            MemeDef meme = newMemes[i];
            if (precept.def.conflictingMemes.Contains(meme))
            {
                return true;
            }

            if (precept.def.allowDuplicates || meme.requireOne.NullOrEmpty())
            {
                continue;
            }

            for (int j = 0; j < meme.requireOne.Count; j++)
            {
                List<PreceptDef> group = meme.requireOne[j];
                if (group.Count == 0 || IsAdministratorOnlyGroup(meme, group))
                {
                    continue;
                }

                int sameIssueReplacements = 0;
                for (int k = 0; k < group.Count; k++)
                {
                    if (group[k] != precept.def && group[k].issue == precept.def.issue)
                    {
                        sameIssueReplacements++;
                    }
                }

                if (sameIssueReplacements == group.Count)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsAdministratorOnlyGroup(MemeDef meme, List<PreceptDef> group)
    {
        if (meme.defName != AdministratorRole.MemeDefName || group.Count != 1)
        {
            return false;
        }

        return group[0].defName == AdministratorRole.DefName;
    }
}

/// <summary>
/// Adding a required precept removes one existing precept of the same issue. That is correct
/// for single-choice issues and wrong for roles, which allow several at once.
/// </summary>
[HarmonyPatch(typeof(IdeoFoundation), "AddRequiredPreceptsForMemes")]
public static class IdeoFoundation_AddRequiredPreceptsForMemes_Patch
{
    public static void Prefix()
    {
        RoleRemovalGuard.InAddRequiredPrecepts = true;
    }

    public static void Finalizer()
    {
        RoleRemovalGuard.InAddRequiredPrecepts = false;
    }
}

[HarmonyPatch(typeof(Ideo), nameof(Ideo.RemovePrecept))]
public static class Ideo_RemovePrecept_Patch
{
    public static bool Prefix(Precept precept)
    {
        if (!RoleRemovalGuard.InAddRequiredPrecepts || precept is not Precept_Role)
        {
            return true;
        }

        return precept.def.issue == null || !precept.def.issue.allowMultiplePrecepts;
    }
}

[HarmonyPatch(typeof(IdeoFoundation), nameof(IdeoFoundation.EnsurePreceptsCompatibleWithMemes))]
public static class IdeoFoundation_EnsurePreceptsCompatibleWithMemes_Patch
{
    public static void Postfix(IdeoFoundation __instance)
    {
        SavedIdeoRoleFix.ReplaceLeader(__instance.ideo);
    }
}

[HarmonyPatch(typeof(Ideo), nameof(Ideo.ExposeData))]
public static class Ideo_ExposeData_Patch
{
    public static void Postfix(Ideo __instance)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        SavedIdeoRoleFix.ReplaceLeader(__instance);
    }
}

internal static class RoleRemovalGuard
{
    [ThreadStatic]
    public static bool InAddRequiredPrecepts;
}

internal static class SavedIdeoRoleFix
{
    public static void ReplaceLeader(Ideo ideo)
    {
        if (!AdministratorRole.IdeoHasCompassion(ideo))
        {
            return;
        }

        Pawn previousHolder = null;
        Precept leader = null;
        Precept_RoleSingle administrator = null;
        List<Precept> staleAdministrators = new List<Precept>();

        foreach (Precept precept in ideo.PreceptsListForReading)
        {
            if (precept.def == PreceptDefOf.IdeoRole_Leader)
            {
                leader = precept;
                if (precept is Precept_RoleSingle leaderRole)
                {
                    previousHolder = leaderRole.ChosenPawnSingle();
                }
            }
            else if (precept.def.defName == AdministratorRole.DefName)
            {
                if (precept is Precept_RoleSingle single)
                {
                    administrator = single;
                }
                else
                {
                    staleAdministrators.Add(precept);
                    if (precept is Precept_Role role)
                    {
                        previousHolder ??= role.ChosenPawns().FirstOrDefault();
                    }
                }
            }
        }

        foreach (Precept stale in staleAdministrators)
        {
            ideo.RemovePrecept(stale);
        }

        if (leader != null)
        {
            ideo.RemovePrecept(leader);
        }

        if (administrator == null)
        {
            PreceptDef def = DefDatabase<PreceptDef>.GetNamedSilentFail(AdministratorRole.DefName);
            if (def != null && ideo.foundation.CanAdd(def, true).Accepted)
            {
                ideo.AddPrecept(PreceptMaker.MakePrecept(def), init: true);
                administrator = ideo.RolesListForReading.OfType<Precept_RoleSingle>()
                    .FirstOrDefault(role => role.def.defName == AdministratorRole.DefName);
            }
        }

        if (administrator == null || previousHolder == null || administrator.ChosenPawnSingle() != null || !administrator.ValidatePawn(previousHolder))
        {
            return;
        }

        // Assigning a leader role also sets the player faction leader. Keep that for colonists, and put it back when the holder belongs to someone else.
        Pawn playerLeader = Faction.OfPlayer?.leader;
        administrator.Assign(previousHolder, addThoughts: false);
        if (previousHolder.Faction != Faction.OfPlayer && Faction.OfPlayer != null)
        {
            Faction.OfPlayer.leader = playerLeader;
        }
    }
}
