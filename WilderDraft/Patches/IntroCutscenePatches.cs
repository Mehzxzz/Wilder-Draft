using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using PowerTools;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using UnityEngine;
using WilderDraft.Components;
using WilderDraft.Options;
using WilderDraft.Utilities;
using Object = UnityEngine.Object;

namespace WilderDraft.Patches;

[HarmonyPatch]
public static class IntroCutscenePatches
{
    private static readonly int Rad = Shader.PropertyToID("_Rad");

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    [HarmonyPrefix]
    public static bool IntroCutscene_CoBegin_Prefix(IntroCutscene __instance)
    {
        Coroutines.Start(CoBeginDraft(__instance));
        return false;
    }

    private static IEnumerator CoBeginDraft(IntroCutscene introCutscene)
    {
        PlayerControl.LocalPlayer.moveable = false;
        Logger.GlobalInstance.Info("IntroCutscene :: CoBegin() :: Starting intro cutscene");
        SoundManager.Instance.PlaySound(introCutscene.IntroStinger, false);
        if (GameManager.Instance.IsNormal())
        {
            Logger.GlobalInstance.Info("IntroCutscene :: CoBegin() :: Game Mode: Normal");
            introCutscene.LogPlayerRoleData();
            introCutscene.HideAndSeekPanels.SetActive(false);
            introCutscene.CrewmateRules.SetActive(false);
            introCutscene.ImpostorRules.SetActive(false);
            introCutscene.ImpostorName.gameObject.SetActive(false);
            introCutscene.ImpostorTitle.gameObject.SetActive(false);
            var show = IntroCutscene.SelectTeamToShow((Func<NetworkedPlayerInfo, bool>) (pcd => !PlayerControl.LocalPlayer.Data.Role.IsImpostor || pcd.Role.TeamType == PlayerControl.LocalPlayer.Data.Role.TeamType));
            if (show == null || show.Count < 1)
                Logger.GlobalInstance.Error("IntroCutscene :: CoBegin() :: teamToShow is EMPTY or NULL");
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                introCutscene.ImpostorText.gameObject.SetActive(false);
            }
            else
            {
                int adjustedNumImpostors = GameManager.Instance.LogicOptions.GetAdjustedNumImpostors(GameData.Instance.PlayerCount);
                if (adjustedNumImpostors == 1)
                    introCutscene.ImpostorText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsS);
                else
                    introCutscene.ImpostorText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.NumImpostorsP, adjustedNumImpostors);
                introCutscene.ImpostorText.text = introCutscene.ImpostorText.text.Replace("[FF1919FF]", "<color=#FF1919FF>");
                introCutscene.ImpostorText.text = introCutscene.ImpostorText.text.Replace("[]", "</color>");
            }
            
            RoleHelpers.GetAlignment(PlayerControl.LocalPlayer.Data.Role, out var teamName, out Color teamColor);
            introCutscene.TeamTitle.text = teamName;
            introCutscene.BackgroundBar.material.SetColor(ShaderID.Color, teamColor);
            var fullScreen = Object.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
            fullScreen.transform.localPosition = new Vector3(0, 0, -250);
            fullScreen.gameObject.SetActive(true);
            fullScreen.color = Color.black;
            introCutscene.StartCoroutine(Effects.ActionAfterDelay(0.05f,
                new Action(() => introCutscene.BackgroundBar.material.color = teamColor)));
            yield return introCutscene.ShowTeam(show, 3);
            introCutscene.BackgroundBar.material.SetColor(ShaderID.Color, teamColor);
            var audioSource = new GameObject().AddComponent<AudioSource>();
            audioSource.transform.position = Camera.main.transform.position;
            audioSource.clip = introCutscene.IntroStinger;
            audioSource.loop = false;
            audioSource.volume = 0.5f;
            audioSource.pitch = 2;
            
            var meshRenderer = introCutscene.transform.FindChild("BackgroundLayer").GetComponent<MeshRenderer>();
            if (OptionGroupSingleton<DraftOptions>.Instance.isRoleDraftEnabled.Value)
            {
                Coroutines.Start(DraftEffects.CoFadeColor(meshRenderer.material, Color.black,
                    teamColor.DarkenColor(0.7f), 0.4f));
                meshRenderer.transform.localScale = new Vector3(meshRenderer.transform.localScale.x, meshRenderer.transform.localScale.y * 2f, 1f);
                meshRenderer.transform.localPosition = new Vector3(0, 0, -16);
                audioSource.Play();
                var menu = Object.Instantiate(WilderDraftPlugin.bundle.LoadAsset<GameObject>("SelectCanvas.prefab"))
                    .GetComponent<CardDeck>();
                menu.BeginRoles();
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                while (menu != null)
                    yield return null;
                yield return new WaitForSeconds(0.7f);
                meshRenderer.transform.localScale = new Vector3(meshRenderer.transform.localScale.x, meshRenderer.transform.localScale.y / 2f, 1f);
                meshRenderer.transform.localPosition = new Vector3(0, 0, -13);
                yield return Coroutines.Start(DraftEffects.CoFadeColor(meshRenderer.material, meshRenderer.material.color,Color.black, 0.2f));
            }
            yield return introCutscene.StartCoroutine(introCutscene.ShowRole());
            if (OptionGroupSingleton<DraftOptions>.Instance.isModifierDraftEnabled.Value)
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    var modifierComponent = p.GetModifierComponent();
                    foreach (var mod in modifierComponent.ActiveModifiers)
                    {
                        modifierComponent.RemoveModifier(mod);
                    }
                }

                yield return new WaitForFixedUpdate();
                Coroutines.Start(DraftEffects.CoFadeColor(meshRenderer.material, Color.black,
                    RoleHelpers.GetRoleColor(PlayerControl.LocalPlayer.Data.Role).DarkenColor(0.7f), 0.4f));
                meshRenderer.transform.localScale = new Vector3(meshRenderer.transform.localScale.x, meshRenderer.transform.localScale.y * 2f, 1f);
                meshRenderer.transform.localPosition = new Vector3(0, 0, -16);
                audioSource.pitch = 0.5f;
                audioSource.Play();
                var menu2 = Object.Instantiate(WilderDraftPlugin.bundle.LoadAsset<GameObject>("SelectCanvas.prefab"))
                    .GetComponent<CardDeck>();
                menu2.BeginModifiers();
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                while (menu2 != null)
                    yield return null;

                meshRenderer.material.color = Color.black;
            }
            audioSource.gameObject.Destroy();
            PlayerControl.LocalPlayer.moveable = true;
            fullScreen.gameObject.Destroy();
            yield return HudManager.Instance.CoFadeFullScreen(Color.black, Color.clear);
        }
        else
        {
            Logger.GlobalInstance.Info("IntroCutscene :: CoBegin() :: Game Mode: Hide and Seek");
            introCutscene.LogPlayerRoleData();
            introCutscene.HideAndSeekPanels.SetActive(true);
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                introCutscene.CrewmateRules.SetActive(false);
                introCutscene.ImpostorRules.SetActive(true);
            }
            else
            {
                introCutscene.CrewmateRules.SetActive(true);
                introCutscene.ImpostorRules.SetActive(false);
            }
            var show = IntroCutscene.SelectTeamToShow((Func<NetworkedPlayerInfo, bool>) (pcd => PlayerControl.LocalPlayer.Data.Role.IsImpostor != pcd.Role.IsImpostor));
            if (show == null || show.Count < 1)
                    Logger.GlobalInstance.Error("IntroCutscene :: CoBegin() :: teamToShow is EMPTY or NULL");
            PlayerControl impostor = PlayerControl.AllPlayerControls.ToArray().ToList().Find((Predicate<PlayerControl>) (pc => pc.Data.Role.IsImpostor));
            if (impostor == null)
                    Logger.GlobalInstance.Error("IntroCutscene :: CoBegin() :: impostor is NULL");
            GameManager.Instance.SetSpecialCosmetics(impostor);
            introCutscene.ImpostorName.gameObject.SetActive(true);
            introCutscene.ImpostorTitle.gameObject.SetActive(true);
            introCutscene.BackgroundBar.enabled = false;
            introCutscene.TeamTitle.gameObject.SetActive(false);
            if (impostor != null)
                introCutscene.ImpostorName.text = impostor.Data.PlayerName;
            else
                introCutscene.ImpostorName.text = "???";
            yield return new WaitForSecondsRealtime(0.1f);
            PoolablePlayer playerSlot = null;
            if (impostor != null)
            {
                  playerSlot = introCutscene.CreatePlayer(1, 1, impostor.Data, false);
                  playerSlot.SetBodyType(PlayerBodyTypes.Normal);
                  playerSlot.SetFlipX(false);
                  playerSlot.transform.localPosition = introCutscene.impostorPos;
                  playerSlot.transform.localScale = Vector3.one * introCutscene.impostorScale;
            }
            yield return ShipStatus.Instance.CosmeticsCache.PopulateFromPlayers();
            yield return new WaitForSecondsRealtime(6f);
            if (playerSlot != null)
                playerSlot.gameObject.SetActive(false);
            introCutscene.HideAndSeekPanels.SetActive(false);
            introCutscene.CrewmateRules.SetActive(false);
            introCutscene.ImpostorRules.SetActive(false);
            LogicOptionsHnS logicOptions = GameManager.Instance.LogicOptions as LogicOptionsHnS;
            if (GameManager.Instance.GetLogicComponent<LogicHnSMusic>() is LogicHnSMusic logicComponent)
                logicComponent.StartMusicWithIntro();
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
              float crewmateLeadTime = logicOptions.GetCrewmateLeadTime();
              introCutscene.HideAndSeekTimerText.gameObject.SetActive(true);
              PoolablePlayer poolablePlayer;
              AnimationClip anim;
              if (AprilFoolsMode.ShouldHorseAround())
              {
                    poolablePlayer = introCutscene.HorseWrangleVisualSuit;
                    poolablePlayer.gameObject.SetActive(true);
                    poolablePlayer.SetBodyType(PlayerBodyTypes.Seeker);
                    anim = introCutscene.HnSSeekerSpawnHorseAnim;
                    introCutscene.HorseWrangleVisualPlayer.SetBodyType(PlayerBodyTypes.Normal);
                    introCutscene.HorseWrangleVisualPlayer.UpdateFromPlayerData(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.CurrentOutfitType, PlayerMaterial.MaskType.None, false);
              }
              else if (AprilFoolsMode.ShouldLongAround())
              {
                    poolablePlayer = introCutscene.HideAndSeekPlayerVisual;
                    poolablePlayer.gameObject.SetActive(true);
                    poolablePlayer.SetBodyType(PlayerBodyTypes.LongSeeker);
                    anim = introCutscene.HnSSeekerSpawnLongAnim;
              }
              else
              {
                    poolablePlayer = introCutscene.HideAndSeekPlayerVisual;
                    poolablePlayer.gameObject.SetActive(true);
                    poolablePlayer.SetBodyType(PlayerBodyTypes.Seeker);
                    anim = introCutscene.HnSSeekerSpawnAnim;
              }
              poolablePlayer.SetBodyCosmeticsVisible(false);
              poolablePlayer.UpdateFromPlayerData(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.CurrentOutfitType, PlayerMaterial.MaskType.None, false);
              SpriteAnim component = poolablePlayer.GetComponent<SpriteAnim>();
              poolablePlayer.gameObject.SetActive(true);
              poolablePlayer.ToggleName(false);
              component.Play(anim);
              while (crewmateLeadTime > 0.0)
              {
                    introCutscene.HideAndSeekTimerText.text = Mathf.RoundToInt(crewmateLeadTime).ToString();
                    crewmateLeadTime -= Time.deltaTime;
                    yield return null;
              }
            }
            else
            {
              ShipStatus.Instance.HideCountdown = logicOptions.GetCrewmateLeadTime();
              if (AprilFoolsMode.ShouldHorseAround())
              {
                if (impostor != null)
                  impostor.AnimateCustom(introCutscene.HnSSeekerSpawnHorseInGameAnim);
              }
              else if (AprilFoolsMode.ShouldLongAround())
              {
                if (impostor != null)
                  impostor.AnimateCustom(introCutscene.HnSSeekerSpawnLongInGameAnim);
              }
              else if (impostor != null)
              {
                impostor.AnimateCustom(introCutscene.HnSSeekerSpawnAnim);
                impostor.cosmetics.SetBodyCosmeticsVisible(false);
              }
            }
            impostor = null;
            playerSlot = null;
        }
        ShipStatus.Instance.StartSFX();
        Object.Destroy(introCutscene.gameObject);
    }
}