using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFadeInOut : MonoBehaviour
{
    private Image image;
    private Color baseColor;
    private Color transparent;

    void Start()
    {
        image = GetComponent<Image>();
        if (image == null) return;

        baseColor = image.color;
        transparent = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        image.color = transparent;
    }

    public IEnumerator FadeInOut(float inTime, float sustainTime, float outTime)
    {
        if (image == null) yield break;

        float inDur = Mathf.Max(0f, inTime);
        float hold = Mathf.Max(0f, sustainTime);
        float outDur = Mathf.Max(0f, outTime);
        Color opaque = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        image.color = transparent;

        // fade in
        if (inDur > 0f)
        {
            float t = 0f;
            while (t < inDur)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / inDur);
                image.color = Color.Lerp(transparent, opaque, lerp);
                yield return null;
            }
        }
        image.color = opaque;

        // sustain
        if (hold > 0f) yield return new WaitForSeconds(hold);

        // fade out
        if (outDur > 0f)
        {
            float t = 0f;
            while (t < outDur)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / outDur);
                image.color = Color.Lerp(opaque, transparent, lerp);
                yield return null;
            }
        }
        image.color = transparent;
    }
}
