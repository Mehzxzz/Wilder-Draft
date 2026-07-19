using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TMPro;
using UnityEngine;
using WilderDraft.Options;
using WilderDraft.Utilities;

namespace WilderDraft.Components;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "ReturnValueOfPureMethodIsNotUsed")]
public class CardDeck(IntPtr ptr) : MonoBehaviour(ptr)
{
    public Il2CppReferenceField<TextMeshProUGUI> titleText;
    public Il2CppReferenceField<TextMeshProUGUI> timerText;
    public Il2CppReferenceField<TextMeshProUGUI> infoText;
    public Il2CppReferenceField<CardBehaviour> cardPrefab;
    public Il2CppReferenceField<CardReleaseZone> cardReleaseZonePrefab;
    public List<CardBehaviour> cards = new();
    public Il2CppReferenceField<Transform> cardsParent;
    public Il2CppReferenceField<AudioClip> cardSound;
    public CardReleaseZone releaseZone;
    public int Uses = 1;
    public float timeLeft = OptionGroupSingleton<DraftOptions>.Instance.selectTimer.Value;
    public void BeginRoles()
    {
        titleText.Value.text = "Pick A Role!";
        var canvas = GetComponent<Canvas>();
        releaseZone = Instantiate(cardReleaseZonePrefab.Value, canvas.transform);
        releaseZone.highlightImage.Value.sprite = Assets.Silhouette.LoadAsset();
        releaseZone.highlightImage.Value.preserveAspect = true;
        releaseZone.transform.localPosition = Vector3.zero + new Vector3(0, 100);
        releaseZone.gameObject.AddComponent<CursorFollowUGUI>();
        List<RoleBehaviour> possibleRoles = RoleManager.Instance.AllRoles.ToArray().Where(x => RoleHelpers.CompareRoleAlignments(x, PlayerControl.LocalPlayer.Data.Role) && GameOptionsManager.Instance.currentGameOptions.RoleOptions.GetNumPerGame(x.Role) > 0).ToList();
        var rolePool = new List<RoleBehaviour>();
        foreach (var role in possibleRoles)
        {
            var chance = GameOptionsManager.Instance.currentGameOptions.RoleOptions.GetChancePerGame(role.Role);
            for (int i = 0;
                 i < GameOptionsManager.Instance.currentGameOptions.RoleOptions.GetNumPerGame(role.Role);
                 i++)
            {
                if (Helpers.CheckChance(chance)) rolePool.Add(role);
            }
        }
        releaseZone.GetComponent<RectTransform>().sizeDelta = new Vector2(165, 211);
        for (int i = 0; i < OptionGroupSingleton<DraftOptions>.Instance.roleDraftDeckSize.Value; i++)
        {
            var card = Instantiate(cardPrefab.Value, cardsParent.Value.transform);
            var r = rolePool.Random();
            card.OnDropAccepted = () =>
            {
                Uses--;
                if (Uses > 0) return;
                Coroutines.Start(CoClose());
            };
            rolePool.RemoveAll(x => x.Role == r.Role);
            card.InitializeForRole(r);
            Coroutines.Start(card.CoAnimate(i / 5f, false));
            cards.Add(card);
            
            var hover = card.coloredPart.Value.gameObject.AddComponent<HoverBehaviour>();
            hover.OnEnter = _ =>
            {
                infoText.Value.text = $"<b>{r.NiceName}:</b> {r.Blurb}";
            };
            hover.OnExit = _ => infoText.Value.text = "Hover over a card for more information.";
        }
    }

    private void Start()
    {
        infoText.Value.text = "Hover over a card for more information.";
        SoundManager.Instance.PlaySound(cardSound.Value, false);
    }

    public void BeginModifiers()
    {
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            var modifierComponent = p.GetModifierComponent();
            foreach (var mod in modifierComponent.ActiveModifiers)
            {
                modifierComponent.ActiveModifiers.Remove(mod);
                mod.OnDeactivate();
            }
        }
        var canvas = GetComponent<Canvas>();
        releaseZone = Instantiate(cardReleaseZonePrefab.Value, canvas.transform);
        releaseZone.highlightImage.Value.sprite = Assets.Silhouette.LoadAsset();
        releaseZone.highlightImage.Value.preserveAspect = true;
        releaseZone.transform.localPosition = Vector3.zero + new Vector3(0, 100);
        releaseZone.gameObject.AddComponent<CursorFollowUGUI>();
        
        List<GameModifier> possibleModifiers = new();
        Logger<WilderDraftPlugin>.Error($"Total modifiers: {ModifierManager.Modifiers.Count} | Current role: {PlayerControl.LocalPlayer.Data.Role?.NiceName ?? "idk this missing some reason lol"}");
        foreach (var baseModifier in ModifierManager.Modifiers)
        {
            if (baseModifier is not GameModifier gameModifier)
            {
                Logger<WilderDraftPlugin>.Error($"Skipped non GameModifier: {baseModifier.GetType().Name}");
                continue;
            }

            bool canSpawn = gameModifier.CanSpawnOnCurrentMode();
            bool validOnRole = gameModifier.IsModifierValidOn(PlayerControl.LocalPlayer.Data.Role);
            int amountPerGame = gameModifier.GetAmountPerGame();
            int assignChance = gameModifier.GetAssignmentChance();
            bool wonChance = Helpers.CheckChance(assignChance);
            bool included = canSpawn && validOnRole && amountPerGame > 0 && wonChance;

            Logger<WilderDraftPlugin>.Error(
                $"Modifier: {gameModifier.ModifierName} | Can Spawn: {canSpawn} | Valid On Role: {validOnRole} | Amount Per Game: {amountPerGame} | Assignment Chance: {assignChance}");

            if (included) possibleModifiers.Add(gameModifier);
        }
        Logger<WilderDraftPlugin>.Error($"Total modifiers: {possibleModifiers.Count}");

        var draftOptions = OptionGroupSingleton<DraftOptions>.Instance;
        int toBeAssignedCount = UnityEngine.Random.RandomRangeInt((int)draftOptions.minModQuota.Value, (int)draftOptions.maxModQuota.Value + 1);
        Logger<WilderDraftPlugin>.Instance.LogError($"Modifiers to be assigned: {toBeAssignedCount} | min {draftOptions.minModQuota.Value}/max {draftOptions.maxModQuota.Value})");
        if (toBeAssignedCount == 0 || possibleModifiers.Count < toBeAssignedCount || possibleModifiers.Count == 0)
        {
            Logger<WilderDraftPlugin>.Instance.LogError(
                $"Modifiers to be assigned: {toBeAssignedCount} | Possible modifiers: {possibleModifiers.Count}");
            gameObject.DestroyImmediate();
            return;
        }

        Uses = toBeAssignedCount;
        titleText.Value.text = $"Pick {Uses} Modifiers!";
        
        releaseZone.GetComponent<RectTransform>().sizeDelta = new Vector2(165, 211);
        for (int i = 0; i < draftOptions.modifierDraftDeckSize.Value; i++)
        {
            if (possibleModifiers.Count == 0) return;
            var card = Instantiate(cardPrefab.Value, cardsParent.Value.transform);
            Coroutines.Start(card.CoAnimate(i / 5f, true));
            var m = possibleModifiers.Random();
            possibleModifiers.Remove(m);
            card.OnDropAccepted = () =>
            {
                Uses--;
                titleText.Value.text = $"Pick {Uses} More Modifier" + (Uses == 1 ? "" : "s") + "!";
                if (Uses > 0) return;
                Coroutines.Start(CoClose());
            };
            card.InitializeForModifier(m);
            cards.Add(card);
            var hover = card.gameObject.AddComponent<HoverBehaviour>();
            hover.OnEnter = _ =>
            {
                infoText.Value.text = $"<b>{m.ModifierName}:</b> {m.GetDescription()}";
                infoText.Value.color = m.FreeplayFileColor.LightenColor();
            };
            hover.OnExit = _ =>
            {
                infoText.Value.text = "Hover over a card for more information.";
                infoText.Value.color = Color.white;
            };
        }
    }

    private bool _timeUp;

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        timerText.Value.text = ((int)timeLeft) + "s";
        if (timeLeft <= 0 && !_timeUp)
        {
            _timeUp = true;
            cardsParent.Value.gameObject.SetActive(false);
            timerText.Value.text = "TIME'S UP!";
            timerText.Value.color = Color.red;

            var remainingPool = new List<CardBehaviour>(cards);
            while (Uses > 0 && remainingPool.Count > 0)
            {
                var pick = remainingPool.Random();
                remainingPool.Remove(pick);
                pick.OnDropAccepted.Invoke();
            }
        }
        if (timeLeft <= -3)
        {
            gameObject.Destroy();
        }
    }

    private IEnumerator CoClose()
    {
        cardsParent.Value.gameObject.SetActive(false);
        var cursorFollowUGUI = releaseZone.GetComponent<CursorFollowUGUI>();
        cursorFollowUGUI.enabled = false;
        HudManager.Instance.StartCoroutine(Effects.ScaleIn(titleText.Value.transform.parent, 1, 0, 0.4f));
        HudManager.Instance.StartCoroutine(Effects.Slide2D(titleText.Value.transform.parent, titleText.Value.transform.parent.localPosition, titleText.Value.transform.parent.localPosition + new Vector3(0, 500, 0), 0.4f));
        HudManager.Instance.StartCoroutine(Effects.Slide2D(timerText.Value.transform.parent, timerText.Value.transform.parent.localPosition, timerText.Value.transform.parent.localPosition + new Vector3(0, 500, 0), 0.4f));
        HudManager.Instance.StartCoroutine(Effects.Slide2D(infoText.Value.transform.parent, infoText.Value.transform.parent.localPosition, infoText.Value.transform.parent.localPosition - new Vector3(0, 500, 0), 0.4f));
        yield return new WaitForSeconds(0.5f);
        gameObject.Destroy();
    }
}