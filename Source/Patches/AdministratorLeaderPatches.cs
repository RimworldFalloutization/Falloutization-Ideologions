using HarmonyLib;

namespace Falloutization.Ideologions.Patches;

internal static class AdministratorRole
{
    public const string DefName = "FCP_IdeoRole_Followers_Administrator";
    public const string MemeDefName = "FCP_Meme_Followers_Compassion";

    public static bool IdeoHasCompassion(Ideo ideo)
    {
        if (ideo?.memes == null)
        {
            return false;
        }

        for (int i = 0; i < ideo.memes.Count; i++)
        {
            if (ideo.memes[i].defName == MemeDefName)
            {
                return true;
            }
        }

        return false;
    }

    public static Precept_Role LeaderRole(Ideo ideo)
    {
        if (ideo == null)
        {
            return null;
        }

        foreach (Precept_Role role in ideo.RolesListForReading)
        {
            if (role.def.leaderRole)
            {
                return role;
            }
        }

        return null;
    }
}

/// <summary>
/// Ritual slots name the vanilla Leader precept. When that precept is absent, the
/// ideoligion's leaderRole (Administrator) fills the same slot and is preferred for it.
/// </summary>
[HarmonyPatch(typeof(RitualRole), nameof(RitualRole.FindInstance))]
public static class RitualRole_FindInstance_Patch
{
    public static void Postfix(RitualRole __instance, Ideo ideo, ref Precept_Role __result)
    {
        if (__result != null || ideo == null || __instance.precept != PreceptDefOf.IdeoRole_Leader)
        {
            return;
        }

        __result = AdministratorRole.LeaderRole(ideo);
    }
}

[HarmonyPatch(typeof(RitualRole), "PawnDesirability")]
public static class RitualRole_PawnDesirability_Patch
{
    public static void Postfix(RitualRole __instance, Pawn pawn, ref int __result)
    {
        if (__result > 0 || __instance.precept != PreceptDefOf.IdeoRole_Leader)
        {
            return;
        }

        if (pawn?.Ideo?.GetRole(pawn)?.def.leaderRole == true)
        {
            __result = 1;
        }
    }
}

/// <summary>
/// The role card lists rituals whose slot precept is exactly this role. Point Leader
/// slots at Administrator when that is the ideoligion's leader.
/// </summary>
[HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.GetTip))]
public static class Precept_Role_GetTip_Patch
{
    public static void Postfix(Precept_Role __instance, ref string __result)
    {
        if (__instance?.ideo == null || !__instance.def.leaderRole || __instance.def == PreceptDefOf.IdeoRole_Leader)
        {
            return;
        }

        string header = "RoleRitualsLabel".Translate() + ":";
        if (__result != null && __result.Contains(header))
        {
            return;
        }

        List<string> labels = new List<string>();
        foreach (Precept precept in __instance.ideo.PreceptsListForReading)
        {
            if (precept is not Precept_Ritual ritual || !precept.def.listedForRoles)
            {
                continue;
            }

            if (ritual.behavior?.def.roles == null || ritual.behavior.def.stages == null)
            {
                continue;
            }

            if (!ritual.behavior.def.roles.Any(role => role.precept == PreceptDefOf.IdeoRole_Leader))
            {
                continue;
            }

            if (!labels.Contains(ritual.LabelCap))
            {
                labels.Add(ritual.LabelCap);
            }
        }

        if (labels.Count == 0)
        {
            return;
        }

        StringBuilder builder = new StringBuilder(__result);
        builder.AppendLine();
        builder.AppendLine(__instance.ColorizeDescTitle(header));
        builder.Append(labels.ToLineList("  - "));
        __result = builder.ToString();
    }
}

/// <summary>
/// Ritual gizmos mirror the cooldown of the ability on IdeoRole_Leader. Use the
/// leaderRole holder when that precept is not on the ideoligion.
/// </summary>
[HarmonyPatch(typeof(Command_Ritual), "ValidateDisabledState")]
public static class Command_Ritual_ValidateDisabledState_Patch
{
    public static void Postfix(Command_Ritual __instance)
    {
        Precept_Ritual ritual = __instance.ritual;
        if (__instance.disabled || ritual?.ideo == null || ritual.def.sourcePawnRoleDef != PreceptDefOf.IdeoRole_Leader || ritual.def.sourceAbilityDef == null)
        {
            return;
        }

        if (ritual.ideo.RolesListForReading.Any(role => role.def == PreceptDefOf.IdeoRole_Leader))
        {
            return;
        }

        if (AdministratorRole.LeaderRole(ritual.ideo) is not Precept_RoleSingle leader || leader.ChosenPawnSingle() == null)
        {
            return;
        }

        Ability ability = leader.AbilitiesForReading.FirstOrDefault(candidate => candidate.def == ritual.def.sourceAbilityDef);
        if (ability != null && ability.GizmoDisabled(out string reason))
        {
            __instance.disabled = true;
            __instance.disabledReason = reason;
        }
    }
}

/// <summary>
/// Keep the displayed leader title as Administrator instead of a generated culture title.
/// </summary>
[HarmonyPatch(typeof(Precept_Role), nameof(Precept_Role.GenerateNameRaw))]
public static class Precept_Role_GenerateNameRaw_Patch
{
    public static void Postfix(Precept_Role __instance, ref string __result)
    {
        if (__instance.def.defName != AdministratorRole.DefName || __instance.ideo == null)
        {
            return;
        }

        __instance.ideo.leaderTitleMale = __instance.def.label;
        __instance.ideo.leaderTitleFemale = __instance.def.label;
        __result = __instance.def.label;
    }
}
