using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using Reactor.Utilities.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace WilderDraft.Components;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class CardBehaviour(IntPtr ptr) : MonoBehaviour(ptr)
{
    public Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private Vector2 lastMousePosition;
    public Action OnDropAccepted;
    public Il2CppReferenceField<Image> coloredPart;
    public Il2CppReferenceField<Image> icon;
    public Il2CppReferenceField<TextMeshProUGUI> fallBackText;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        // Wire up drag events through EventTrigger instead of interfaces
        var trigger = gameObject.AddComponent<EventTrigger>();

        var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener((Action<BaseEventData>)((e) => OnBeginDrag(e.Cast<PointerEventData>())));
        trigger.triggers.Add(beginDrag);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener((Action<BaseEventData>)((e) => OnDrag(e.Cast<PointerEventData>())));
        trigger.triggers.Add(drag);

        var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener((Action<BaseEventData>)((e) => OnEndDrag(e.Cast<PointerEventData>())));
        trigger.triggers.Add(endDrag);
    }

    private void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent   = transform.parent;
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        lastMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    }

    private void OnDrag(PointerEventData eventData)
    {
        Vector2 current = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        rectTransform.anchoredPosition += (current - lastMousePosition) / canvas.scaleFactor;
        lastMousePosition = current;
        PlayerControl.LocalPlayer.StartCoroutine(Effects.Rotate2D(transform, 0, Mathf.Clamp(UnityEngine.Random.RandomRangeInt(-40, 40), -10, 10), 0));
    }

    private void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        ReturnToOrigin();
    }

    public void ReturnToOrigin()
    {
        if (originalParent == null) return;
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }

    public void InitializeForRole(RoleBehaviour r)
    {
        icon.Value.transform.localScale *= 2;
        if (r.RoleIconSolid != null) 
        {
            icon.Value.sprite = r.RoleIconSolid;
            fallBackText.Value.enabled = false;
        }
        else
        {
            icon.Value.enabled = false;
            fallBackText.Value.enabled = true;
            fallBackText.Value.text = r.NiceName;
        }
        icon.Value.preserveAspect = true;
        coloredPart.Value.color = r.NameColor == Color.white ? r.TeamColor : r.NameColor;
        OnDropAccepted += () =>
        {
            PlayerControl.LocalPlayer.RpcSetRole(r.Role, true);
            gameObject.Destroy();
        };
    }

    public void InitializeForModifier(GameModifier m)
    {
        fallBackText.Value.enabled = false;
        icon.Value.transform.localScale *= 2;
        try
        {
            // ReSharper disable once PossibleNullReferenceException
            icon.Value.sprite = m.ModifierIcon.LoadAsset();
        }
        catch (Exception)
        {
            fallBackText.Value.enabled = true;
            fallBackText.Value.text = m.ModifierName;
            icon.Value.enabled = false;
        }
        icon.Value.preserveAspect = true;
        coloredPart.Value.color = m.FreeplayFileColor;
        OnDropAccepted += () =>
        {
            PlayerControl.LocalPlayer.RpcAddModifier(m.TypeId);
            gameObject.Destroy();
        };
    }

    public IEnumerator CoAnimate(float delay, bool flipX)
    {
        Vector3 source = new(180f, flipX ? -180f : 180f, 0f); 
        Vector3 dest = new(0f, 0f, 0f);
        transform.localEulerAngles = source;
        yield  return new WaitForSeconds(delay);
        float duration = 0.7f;
        
        Vector3 temp = transform.localEulerAngles;
        for (float time = 0.0f; time < (double) duration; time += Time.deltaTime)
        {
            temp = Vector3.Lerp(source, dest, time / duration);
            transform.localEulerAngles = temp;
            yield return null;
        }
        temp = dest;
        transform.localEulerAngles = temp;
    }
}