using MiraAPI.Roles;
using UnityEngine;

namespace WilderDraft.Utilities;

public class RoleHelpers
{
    public static ModdedRoleTeams GetAlignment(RoleBehaviour r, out string teamName, out Color teamColor)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (r is not ICustomRole c)
        {
            var team = r.TeamType == RoleTeamTypes.Crewmate ? ModdedRoleTeams.Crewmate : ModdedRoleTeams.Impostor;
            teamName = r.TeamType == RoleTeamTypes.Crewmate ? TranslationController.Instance.GetString(StringNames.Crewmate) : TranslationController.Instance.GetString(StringNames.Impostor);
            teamColor = r.TeamType == RoleTeamTypes.Crewmate ? Palette.CrewmateBlue : Palette.ImpostorRed;
            return team;
        }

        teamName = c.IntroConfiguration != null ? c.IntroConfiguration.Value.IntroTeamTitle : c.Team.ToString();
        switch (c.Team)
        {
            case  ModdedRoleTeams.Crewmate:
                teamColor = Palette.CrewmateBlue;
                break;
            case ModdedRoleTeams.Impostor:
                teamColor = Palette.ImpostorRed;
                break;
            case ModdedRoleTeams.Custom:
                teamColor = Color.gray;
                if (c.IntroConfiguration != null) teamColor = c.IntroConfiguration.Value.IntroTeamColor;
                break;
            default:
                teamColor = Color.magenta;
                break;
        }
        return c.Team;
    }
    public static bool CompareRoleAlignments(RoleBehaviour r1, RoleBehaviour r2)
    {
        if (GetAlignment(r1, out string teamName1, out _) == GetAlignment(r2, out string teamName2, out _))
        {
            return teamName1 == teamName2;
        }
        return false;
    }
    public static Color GetRoleColor(RoleBehaviour r)
    {
        if (r is not ICustomRole c) return r.TeamColor;
        return c.RoleColor;
    }
}