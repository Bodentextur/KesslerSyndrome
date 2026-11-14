using System.Collections.Generic;
using UnityEngine;

public class EMPPulse : WeaponBase
{
    [Header("EMP Pulse")]
    [SerializeField] private ExpandingRing ringPrefab;      // visual only
    [SerializeField] private float ringDuration = 0.4f;
    [SerializeField] private float knockback = 8f;
    [SerializeField] private float knockbackGainPerLevel = 1f;
    [SerializeField] private float radiusGainPerLevel = 0.8f;
    [SerializeField] private AudioClip pulseSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.6f;

    private void Reset()
    {
        baseDamage = 25f;
        baseCooldown = 4f;
        baseRange = 4f;
    }

    protected override bool TryFire()
    {
        Vector2 origin = transform.position;

        // Physics query: everything in the blast radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, Range, targetLayers);

        var targets = new List<Debris>();
        foreach (Collider2D h in hits)
            if (h.TryGetComponent(out Debris d) && !targets.Contains(d))
                targets.Add(d);

        if (targets.Count == 0) return false; // don't waste the pulse on empty space

        float force = knockback + knockbackGainPerLevel * (Level - 1);

        foreach (Debris d in targets)
        {
            if (d == null) continue;

            // Push first, so split children spawned by a kill inherit the outward velocity
            if (d.TryGetComponent(out Rigidbody2D rb))
            {
                Vector2 dir = ((Vector2)d.transform.position - origin).normalized;
                rb.AddForce(dir * force, ForceMode2D.Impulse);
            }

            d.TakeDamage(Damage);
        }

        if (ringPrefab != null)
        {
            ExpandingRing ring = Instantiate(ringPrefab, origin, Quaternion.identity);
            ring.Play(Range, ringDuration);
        }

        if (pulseSfx != null)
            AudioManager.Play(pulseSfx, sfxVolume);

        return true;
    }

    protected override void OnLevelUp()
    {
        baseRange += radiusGainPerLevel;
    }

    public override string GetUpgradeText() => "Bigger blast radius, more damage, faster recharge";
}
