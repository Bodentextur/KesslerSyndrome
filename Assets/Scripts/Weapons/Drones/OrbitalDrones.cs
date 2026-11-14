using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Continuous weapon: drones orbit the satellite and damage debris they touch.
/// Cooldown is reused as the "hit interval" per drone per debris, so upgrades make drones hit faster.
/// </summary>
public class OrbitalDrones : WeaponBase
{
    [Header("Orbital Drones")]
    [SerializeField] private DroneHitbox dronePrefab;
    [SerializeField] private int startDrones = 2;
    [SerializeField] private int maxDrones = 5;
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float radiusGainPerLevel = 0.15f;
    [SerializeField] private float rotationSpeed = 120f;     // degrees per second
    [SerializeField] private float speedGainPerLevel = 12f;

    private readonly List<DroneHitbox> drones = new List<DroneHitbox>();
    private float angle;

    private void Reset()
    {
        baseDamage = 6f;
        baseCooldown = 0.5f; // per-target hit interval
        baseRange = 2f;      // unused, orbitRadius is used instead
    }

    private void Start()
    {
        SetDroneCount(startDrones);
    }

    // Replaces the base cooldown loop: this weapon never "fires", it just orbits.
    protected override void Update()
    {
        float speed = rotationSpeed + speedGainPerLevel * (Level - 1);
        float radius = orbitRadius + radiusGainPerLevel * (Level - 1);

        angle = (angle + speed * Time.deltaTime) % 360f; // Time.deltaTime = 0 while paused

        int n = drones.Count;
        for (int i = 0; i < n; i++)
        {
            if (drones[i] == null) continue;

            float a = (angle + i * 360f / n) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;

            // World-space positioning, so the player's rotation doesn't distort the orbit speed
            drones[i].transform.position = (Vector2)transform.position + offset;
        }
    }

    protected override bool TryFire() => false;

    protected override void OnLevelUp()
    {
        // Extra drone at levels 3, 5 and 7
        if (Level == 3 || Level == 5 || Level == 7)
            SetDroneCount(Mathf.Min(maxDrones, drones.Count + 1));
    }

    public override string GetUpgradeText()
    {
        int next = Level + 1;
        if (next == 3 || next == 5 || next == 7) return "+1 drone, more damage";
        return "More damage, faster orbit, wider radius";
    }

    private void SetDroneCount(int count)
    {
        while (drones.Count < count)
        {
            DroneHitbox d = Instantiate(dronePrefab, transform);
            d.Init(this);
            drones.Add(d);
        }
    }

    // Hide the drones when the weapon is disabled (player death) and show them again if re-enabled
    private void OnDisable() => SetDronesActive(false);
    private void OnEnable() => SetDronesActive(true);

    private void SetDronesActive(bool active)
    {
        foreach (DroneHitbox d in drones)
            if (d != null) d.gameObject.SetActive(active);
    }
}
