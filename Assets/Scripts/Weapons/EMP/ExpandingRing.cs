using UnityEngine;

/// <summary>Visual for the EMP: a ring sprite that grows to the blast diameter while fading out.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ExpandingRing : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 baseScale;
    private float baseWorldWidth;
    private float targetDiameter;
    private float duration;
    private float t;
    private Color startColor;
    private bool playing;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;

        // Measure the ring as authored in the prefab (before we shrink it to zero)
        baseScale = transform.localScale;
        baseWorldWidth = sr.bounds.size.x;

        transform.localScale = Vector3.zero;
    }
    public void Play(float radius, float time)
    {
        targetDiameter = radius * 2f;
        duration = Mathf.Max(0.05f, time);
        t = 0f;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;

        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float eased = 1f - (1f - k) * (1f - k);

        // Multiplier that makes the ring's final world width equal the blast diameter
        float finalMultiplier = targetDiameter / baseWorldWidth;
        transform.localScale = baseScale * (finalMultiplier * eased);

        Color c = startColor;
        c.a = startColor.a * (1f - k);
        sr.color = c;

        if (k >= 1f) Destroy(gameObject);
    }
}
