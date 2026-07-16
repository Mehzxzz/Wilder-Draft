using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppSystem.Text;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Patches;
using MiraAPI.Utilities;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WilderDraft.Options;
using WilderDraft.Utilities;

namespace WilderDraft.Components;
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
        releaseZone.highlightImage.Value.sprite = WilderDraft.Assets.Silhouette.LoadAsset();
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
            card.OnDropAccepted = new Action(() =>
            {
                Uses--;
                if (Uses > 0) return;
                Coroutines.Start(CoClose());
            });
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
        releaseZone.highlightImage.Value.sprite = WilderDraft.Assets.Silhouette.LoadAsset();
        releaseZone.highlightImage.Value.preserveAspect = true;
        releaseZone.transform.localPosition = Vector3.zero + new Vector3(0, 100);
        releaseZone.gameObject.AddComponent<CursorFollowUGUI>();
        
        List<GameModifier> possibleModifiers = new();
        foreach (var baseModifier in ModifierManager.Modifiers)
        {
            if (baseModifier is not GameModifier gameModifier) continue;
            if (gameModifier.CanSpawnOnCurrentMode() && gameModifier.IsModifierValidOn(PlayerControl.LocalPlayer.Data.Role) && gameModifier.GetAmountPerGame() > 0 && Helpers.CheckChance(gameModifier.GetAssignmentChance())) possibleModifiers.Add(gameModifier);
        }

        var draftOptions = OptionGroupSingleton<DraftOptions>.Instance;
        int toBeAssignedCount = UnityEngine.Random.RandomRangeInt(0, (int)draftOptions.modifierQuota.Value + 1);
        if (toBeAssignedCount == 0 || possibleModifiers.Count < toBeAssignedCount || possibleModifiers.Count == 0)
        {
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
            card.OnDropAccepted = new Action(() =>
            {
                Uses--;
                titleText.Value.text = $"Pick {Uses} More Modifier" + (Uses == 1 ? "" : "s") + "!";
                if (Uses > 0) return;
                Coroutines.Start(CoClose());
            });
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

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        timerText.Value.text = ((int)timeLeft) + "s";
        if (timeLeft <= 0)
        {
            cardsParent.Value.gameObject.SetActive(false);
            cards.Random().OnDropAccepted.Invoke();
            timerText.Value.text = "TIME'S UP!";
            timerText.Value.color = Color.red;
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