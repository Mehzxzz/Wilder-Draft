using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace WilderDraft.Options;

public class DraftOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Mode";

    public override Color GroupColor => Color.red;

    public ModdedToggleOption isRoleDraftEnabled { get; } = new ModdedToggleOption("Role Drafting", true);
    public ModdedNumberOption roleDraftDeckSize { get; } = new ModdedNumberOption("Role Card Deck Size", 3, 2, 6, 1 ,MiraNumberSuffixes.None, "0 Cards")
    {
        Visible = () => OptionGroupSingleton<DraftOptions>.Instance.isRoleDraftEnabled.Value
    };

    public ModdedToggleOption isModifierDraftEnabled { get; } = new ModdedToggleOption("Modifier Drafting", true);
    public ModdedNumberOption modifierQuota { get; } = new ModdedNumberOption("Max Modifier Drafting Quota", 3, 2, 6, 1 ,MiraNumberSuffixes.None, "0 Cards")
    {
        Visible = () => OptionGroupSingleton<DraftOptions>.Instance.isModifierDraftEnabled.Value
    };
    public ModdedNumberOption modifierDraftDeckSize { get; } = new ModdedNumberOption("Modifier Card Deck Size", 3, 2, 6, 1 ,MiraNumberSuffixes.None, "0 Cards")
    {
        Visible = () => OptionGroupSingleton<DraftOptions>.Instance.isModifierDraftEnabled.Value
    };

    public ModdedNumberOption selectTimer { get; } =
        new ModdedNumberOption("Selection Time Limit", 10, 5, 20, 2.5f, MiraNumberSuffixes.Seconds);
}