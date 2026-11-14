using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Debris : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float baseHealth = 20f;
    [SerializeField] private int sizeTier = 2;               // 2 = large, 1 = medium, 0 = small
    [SerializeField] private float baseDamage = 10f;         // damage at tier 0
    [SerializeField] private float damagePerTier = 5f;       // damage increase per tier
    [SerializeField] private float tumbleSpeed = 90f;        // max random spin in degrees/sec

    [Header("Sprites")]
    [SerializeField] private Sprite[] tierSprites;           // [0] = small, [1] = medium, [2] = large

    [Header("Drops")]
    [SerializeField] private GameObject scrapPrefab;
    [SerializeField] private int scrapPerTier = 2;           // scrap dropped = (tier + 1) * this

    [Header("Splitting")]
    [SerializeField] private int splitCount = 2;
    [SerializeField] private float childScale = 0.65f;
    [SerializeField] private float childHealthFactor = 0.5f;
    [SerializeField] private float splitSpeedBoost = 1.5f;

    [Header("Death effects")]
    [SerializeField] private GameObject explosionVfxPrefab;  
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string dissolveProperty = "_DissolveAmount"; // Shader Graph property reference
    [SerializeField] private float dissolveTime = 0.35f;
    [SerializeField] private AudioClip deathSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.4f;
    [SerializeField] private float vfxScalePerTier = 1f;     // scale multiplier per tier level

    public static event Action<Debris> OnDebrisDestroyed; 

    public int SizeTier => sizeTier;
    public float ContactDamage { get; private set; }

    private GameObject sourcePrefab;
    private Rigidbody2D rb;
    private Collider2D col;
    private float health;
    private float healthMultiplier = 1f;
    private float damageMult = 1f;
    private bool dying;
    public static int ActiveCount { get; private set; }
    public static Transform CullTarget;
    public static float CullDistance = 25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveCount = 0;
        CullTarget = null;
    }
    private void OnEnable() => ActiveCount++;
    private void OnDisable() => ActiveCount--;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.angularDamping = 0f;
        health = baseHealth;
    }

    private void Update()
    {
        if (dying || CullTarget == null) return;

        // Silent removal: no score, scrap or split, the piece just drifted away
        if (((Vector2)transform.position - (Vector2)CullTarget.position).sqrMagnitude > CullDistance * CullDistance)
            Destroy(gameObject);
    }

    /// <summary>Called by the spawner right after Instantiate.</summary>
    public void Init(int tier, float healthMult, float dmgMult, Vector2 velocity, GameObject prefab)
    {
        sourcePrefab = prefab;
        dying = false;
        col.enabled = true;
        if (spriteRenderer != null && spriteRenderer.material.HasProperty(dissolveProperty))
            spriteRenderer.material.SetFloat(dissolveProperty, 0f);

        sizeTier = tier;
        healthMultiplier = healthMult;
        damageMult = dmgMult;
        health = baseHealth * healthMult * (tier + 1) * 0.5f;
        
        // Calculate damage based on tier and difficulty
        ContactDamage = (baseDamage + damagePerTier * tier) * damageMult;
        
        // Set sprite based on tier
        if (tierSprites != null && tierSprites.Length > tier && spriteRenderer != null)
            spriteRenderer.sprite = tierSprites[tier];
        
        SetVelocity(velocity);
        rb.angularVelocity = UnityEngine.Random.Range(-tumbleSpeed, tumbleSpeed);
    }

    public void TakeDamage(float amount)
    {
        if (dying) return;

        health -= amount;
        if (health <= 0f) StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        dying = true;
        col.enabled = false; // no more hits/damage while dissolving

        OnDebrisDestroyed?.Invoke(this);

        if (explosionVfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
            float vfxScale = 1f + vfxScalePerTier * sizeTier;
            vfxInstance.transform.localScale = Vector3.one * vfxScale;
        }

        if (deathSfx != null)
            AudioManager.Play(deathSfx, sfxVolume);

        DropScrap();
        Split();

        // Dissolve shader animation
        if (spriteRenderer != null && spriteRenderer.material.HasProperty(dissolveProperty))
        {
            Material mat = spriteRenderer.material; // instanced copy
            float t = 0f;
            while (t < dissolveTime)
            {
                t += Time.deltaTime;
                mat.SetFloat(dissolveProperty, Mathf.Clamp01(t / dissolveTime));
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private void DropScrap()
    {
        if (scrapPrefab == null) return;

        int count = (sizeTier + 1) * scrapPerTier;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f * (sizeTier + 1);
            Instantiate(scrapPrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }

    private void Split()
    {
        if (sizeTier <= 0 || sourcePrefab == null) return;

        Vector2 parentVel = GetVelocity();
        for (int i = 0; i < splitCount; i++)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            Vector2 pos = (Vector2)transform.position + dir * 0.3f;

            GameObject child = Instantiate(sourcePrefab, pos, Quaternion.identity);
            child.transform.localScale = transform.localScale * childScale;

            if (child.TryGetComponent(out Debris d))
            {
                Vector2 vel = parentVel + dir * splitSpeedBoost;
                d.Init(sizeTier - 1, healthMultiplier * childHealthFactor * 2f, damageMult, vel, sourcePrefab);
            }
        }
    }

    private Vector2 GetVelocity()
    {
        return rb.linearVelocity;
    }

    private void SetVelocity(Vector2 v)
    {
        rb.linearVelocity = v;
    }
}
