using Reactor.Utilities.Attributes;
using UnityEngine;

namespace WilderDraft.Components;

[RegisterInIl2Cpp]
public class CursorFollowUGUI : MonoBehaviour
{
    private Canvas canvas;
    private float maxDistance = 100f;
    
    private float followSpeed = 1f;

    public RectTransform rectTransform;
    private RectTransform canvasRectTransform;
    public Vector2 originalAnchoredPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRectTransform = canvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Remember the resting position so the object always has something to snap back toward.
        originalAnchoredPosition = rectTransform.anchoredPosition;
        if (!Input.mousePresent) enabled = false;
    }

    private void Update()
    {
        if (canvas == null || canvasRectTransform == null) return;
        
        Camera cam = canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform, Input.mousePosition, cam, out Vector2 localMousePos))
            return;

        Vector2 offset = localMousePos - originalAnchoredPosition;
        Vector2 clampedOffset = Vector2.ClampMagnitude(offset, maxDistance);
        Vector2 targetPosition = originalAnchoredPosition + clampedOffset;

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, targetPosition, Time.deltaTime * followSpeed);
    }
}