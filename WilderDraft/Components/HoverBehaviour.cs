using System;
using Il2CppInterop.Runtime;
using Reactor.Utilities.Attributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace WilderDraft.Components;

[RegisterInIl2Cpp]
public class HoverBehaviour : MonoBehaviour
{
    public Action<PointerEventData> OnEnter;
    public Action<PointerEventData> OnExit;

    public void Start()
    {
        var trigger = gameObject.AddComponent<EventTrigger>();

        var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener(
            DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>(
                (Action<BaseEventData>)HandlePointerEnter));
        trigger.triggers.Add(pointerEnter);

        var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener(
            DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>(
                (Action<BaseEventData>)HandlePointerExit));
        trigger.triggers.Add(pointerExit);
    }

    private void HandlePointerEnter(BaseEventData e)
    {
        var p = e.Cast<PointerEventData>();
        OnEnter?.Invoke(p);
    }

    private void HandlePointerExit(BaseEventData e)
    {
        var p = e.Cast<PointerEventData>();
        OnExit?.Invoke(p);
    }
}