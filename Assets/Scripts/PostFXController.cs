using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostFXController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume volume;              // your existing global volume
    [SerializeField] private PlayerController player;

    [Header("Damage vignette")]
    [SerializeField] private Color hitColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField, Range(0f, 1f)] private float peakIntensity = 0.55f;
    [SerializeField, Range(0f, 1f)] private float smoothness = 0.4f;

    [Header("Damage chromatic aberration")]
    [SerializeField, Range(0f, 1f)] private float peakAberration = 0.8f;
    [SerializeField] private float aberrationFalloff = 2f; // higher = the colour split fades faster than the vignette

    [Header("Timing")]
    [SerializeField] private float fadeTime = 0.6f;          // seconds to return to normal
    [SerializeField] private float fullStrengthDamage = 15f; // damage at which the flash reaches full strength
    [SerializeField, Range(0f, 1f)] private float minStrength = 0.4f; // even tiny hits flash at least this much

    private Vignette vignette;
    private ChromaticAberration aberration;

    private float baseIntensity;
    private Color baseColor;
    private float baseSmoothness;
    private float baseAberration;

    private float hit;        // 0 = normal, 1 = full flash
    private float hitPeak;    // strength of the current flash (scales both effects)
    private bool holdPeak;    // stay at the flash after death

    private void Start()
    {
        // volume.profile returns a runtime copy, so the profile asset itself is never modified
        if (!volume.profile.TryGet(out vignette))
            vignette = volume.profile.Add<Vignette>(true);

        if (!volume.profile.TryGet(out aberration))
            aberration = volume.profile.Add<ChromaticAberration>(true);

        // Remember whatever settings you already have as the "calm" look
        baseIntensity = vignette.intensity.overrideState ? vignette.intensity.value : 0f;
        baseColor = vignette.color.overrideState ? vignette.color.value : Color.black;
        baseSmoothness = vignette.smoothness.overrideState ? vignette.smoothness.value : smoothness;
        baseAberration = aberration.intensity.overrideState ? aberration.intensity.value : 0f;

        vignette.active = true;
        aberration.active = true;
        Apply();

        player.OnDamaged += HandleDamaged;
        player.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.OnDamaged -= HandleDamaged;
        player.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (hit <= 0f || holdPeak) return;

        hit = Mathf.MoveTowards(hit, 0f, Time.deltaTime / Mathf.Max(0.01f, fadeTime));
        Apply();
    }

    private void HandleDamaged(float damage)
    {
        // Bigger hits flash harder. Never weaken a flash that is still fading.
        float strength = Mathf.Clamp01(Mathf.Max(minStrength, damage / fullStrengthDamage));
        hitPeak = Mathf.Max(hitPeak * hit, strength);
        hit = 1f; // snap to full for a punchy hit, then fade out
        Apply();
    }

    private void HandleDied()
    {
        holdPeak = true; // keep the red frame on the game-over screen
        hitPeak = 1f;
        hit = 1f;
        Apply();
    }

    private void Apply()
    {
        // Vignette
        float peak = Mathf.Lerp(baseIntensity, peakIntensity, hitPeak);
        vignette.intensity.Override(Mathf.Lerp(baseIntensity, peak, hit));
        vignette.color.Override(Color.Lerp(baseColor, hitColor, hit));
        vignette.smoothness.Override(Mathf.Lerp(baseSmoothness, smoothness, hit));

        // Chromatic aberration: raised to a power so the colour split snaps back faster than the red fades
        float caHit = holdPeak ? hit : Mathf.Pow(hit, aberrationFalloff);
        float caPeak = Mathf.Lerp(baseAberration, peakAberration, hitPeak);
        aberration.intensity.Override(Mathf.Lerp(baseAberration, caPeak, caHit));
    }
}
