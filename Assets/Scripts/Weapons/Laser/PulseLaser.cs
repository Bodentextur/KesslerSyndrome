using UnityEngine;

public class PulseLaser : WeaponBase
{
    [Header("Pulse Laser")]
    [SerializeField] private LaserProjectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileLifetime = 1.5f;
    [SerializeField] private float spreadAngle = 12f;     // total spread when firing several shots
    [SerializeField] private AudioClip shootSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.4f;

    private int projectileCount = 1;
    private int pierce = 0;

    private void Reset()
    {
        // Sensible defaults when you first add the component
        baseDamage = 10f;
        baseCooldown = 0.6f;
        baseRange = 9f;
    }

    protected override bool TryFire()
    {
        Debris target = FindNearestDebris();
        if (target == null) return false;

        Vector2 origin = transform.position;
        Vector2 dir = DirectionTo(origin, target.transform.position);
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projectileCount; i++)
        {
            // Spread shots evenly around the aim direction
            float offset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, i / (float)(projectileCount - 1));

            float angle = baseAngle + offset;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            LaserProjectile p = Instantiate(projectilePrefab, origin, rot);
            p.Init(Damage, projectileSpeed, projectileLifetime, pierce);
        }

        if (shootSfx != null)
            AudioManager.Play(shootSfx, sfxVolume);

        return true;
    }

    protected override void OnLevelUp()
    {
        // Extra projectile at levels 3 and 6, extra pierce at levels 4 and 7
        if (Level == 3 || Level == 6) projectileCount++;
        if (Level == 4 || Level == 7) pierce++;
    }

    public override string GetUpgradeText()
    {
        int next = Level + 1;
        if (next == 3 || next == 6) return "+1 projectile, more damage";
        if (next == 4 || next == 7) return "Lasers pierce +1 target";
        return "More damage, faster firing";
    }
}
