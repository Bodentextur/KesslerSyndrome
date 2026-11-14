using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DroneHitbox : MonoBehaviour
{
    [SerializeField] private GameObject hitVfxPrefab; 
    [SerializeField] private AudioClip hitSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.4f;

    private OrbitalDrones owner;
    private readonly Dictionary<Debris, float> nextHitTime = new Dictionary<Debris, float>();

    public void Init(OrbitalDrones weapon)
    {
        owner = weapon;
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (owner == null) return;
        if (!other.TryGetComponent(out Debris debris)) return;

        // Time.time stops while paused, so intervals pause too
        if (nextHitTime.TryGetValue(debris, out float t) && Time.time < t) return;

        nextHitTime[debris] = Time.time + owner.Cooldown;
        debris.TakeDamage(owner.Damage);

        if (hitVfxPrefab != null)
            Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);

        if (hitSfx != null)
            AudioManager.Play(hitSfx, sfxVolume);

        if (nextHitTime.Count > 50) Prune();
    }

    // Remove entries for debris that no longer exists, so the dictionary doesn't grow forever
    private void Prune()
    {
        var dead = new List<Debris>();
        foreach (var kv in nextHitTime)
            if (kv.Key == null) dead.Add(kv.Key);

        foreach (Debris d in dead)
            nextHitTime.Remove(d);
    }
}
