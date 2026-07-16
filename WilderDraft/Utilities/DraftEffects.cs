using System.Collections;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace WilderDraft.Utilities;

public class DraftEffects
{
    public static IEnumerator CoFadeColor(Material mat, Color start, Color end, float duration)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            mat.color = Color.Lerp(start, end, t/duration);
            yield return null;
        }
        mat.color = end;
    }
}