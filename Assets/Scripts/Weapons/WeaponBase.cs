using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Info (for the level-up UI)")]
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField, TextArea] private string description = "";
    [SerializeField] private Sprite icon;

    [Header("Base stats (level 1)")]
    [SerializeField] protected float baseDamage = 10f;
    [SerializeField] protected float baseCooldown = 1f;
    [SerializeField] protected float baseRange = 10f;

    [Header("Scaling per level")]
    [SerializeField] protected float damageGainPerLevel = 0.25f;     // +25% of base damage each level
    [SerializeField] protected float cooldownReductionPerLevel = 0.08f; // cooldown *= (1 - this) each level
    [SerializeField] protected float minCooldown = 0.1f;

    [Header("Levels")]
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 8;

    [Header("Targeting")]
    [SerializeField] protected LayerMask targetLayers = ~0; // set to your Debris layer

    public string WeaponName => weaponName;
    public string Description => description;
    public Sprite Icon => icon;
    public int Level => level;
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => level >= maxLevel;

    public float Damage => baseDamage * (1f + damageGainPerLevel * (level - 1));
    public float Cooldown => Mathf.Max(minCooldown, baseCooldown * Mathf.Pow(1f - cooldownReductionPerLevel, level - 1));
    public float Range => baseRange;

    private float cooldownTimer;

    protected virtual void Update()
    {
        // Time.deltaTime is 0 while paused, so weapons pause automatically
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (TryFire())
            cooldownTimer = Cooldown;
    }

    /// <summary>Return true if the weapon actually fired (so the cooldown only starts when it did).</summary>
    protected abstract bool TryFire();

    /// <summary>Called when the player picks this weapon again in the level-up screen.</summary>
    public void LevelUp()
    {
        if (IsMaxLevel) return;
        level++;
        OnLevelUp();
    }

    /// <summary>Override for weapon-specific upgrades (extra projectiles, pierce, etc).</summary>
    protected virtual void OnLevelUp() { }

    /// <summary>One-line summary of what the next level gives, for the upgrade card.</summary>
    public virtual string GetUpgradeText() => $"Damage and fire rate up (Lv {level + 1})";

    // ---------- Targeting helpers ----------

    protected Debris FindNearestDebris()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Range, targetLayers);

        Debris nearest = null;
        float best = float.MaxValue;

        foreach (Collider2D h in hits)
        {
            if (!h.TryGetComponent(out Debris d)) continue;

            float dist = ((Vector2)h.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (dist < best)
            {
                best = dist;
                nearest = d;
            }
        }
        return nearest;
    }

    protected static Vector2 DirectionTo(Vector2 from, Vector2 to) => (to - from).normalized;
}
