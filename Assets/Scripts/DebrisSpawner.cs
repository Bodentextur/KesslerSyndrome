using UnityEngine;

public class DebrisSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private Camera cam;

    [Header("Spawning")]
    [SerializeField] private float spawnMargin = 3f;         // how far outside the camera view to spawn
    [SerializeField] private float startSpawnInterval = 1.5f;
    [SerializeField] private float minSpawnInterval = 0.2f;
    [SerializeField] private int maxActiveDebris = 150;      // performance safety cap
    [SerializeField] private float cullPadding = 12f; // how far beyond the spawn ring debris survives

    [Header("Difficulty scaling")]
    [SerializeField] private float secondsPerDifficultyLevel = 30f;
    [SerializeField] private float intervalReductionPerLevel = 0.85f; // spawn interval multiplied each level
    [SerializeField] private float speedPerLevel = 0.25f;             // added to base speed per level
    [SerializeField] private float healthPerLevel = 0.15f;            // added to health multiplier per level
    [SerializeField] private float damagePerLevel = 1f;               // added to damage per difficulty level
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float speedVariance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float aimAtPlayerChance = 0.7f; // otherwise drifts at a random angle

    public int DifficultyLevel { get; private set; }
    public float ElapsedTime { get; private set; }

    private float spawnTimer;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float spawnRadius = Mathf.Sqrt(halfW * halfW + halfH * halfH) + spawnMargin;

        Debris.CullTarget = player;
        Debris.CullDistance = spawnRadius + cullPadding;
    }

    private void Update()
    {
        if(player == null || GameManager.Instance.State != GameState.Playing) return;

        // Time.deltaTime is 0 while paused, so the spawner pauses automatically
        ElapsedTime += Time.deltaTime;
        DifficultyLevel = Mathf.FloorToInt(ElapsedTime / secondsPerDifficultyLevel);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnDebris();
            spawnTimer = CurrentSpawnInterval();
        }
    }

    private float CurrentSpawnInterval()
    {
        float interval = startSpawnInterval * Mathf.Pow(intervalReductionPerLevel, DifficultyLevel);
        return Mathf.Max(minSpawnInterval, interval);
    }

    private void SpawnDebris()
    {
        if (Debris.ActiveCount >= maxActiveDebris) return;

        Vector2 spawnPos = GetSpawnPositionOutsideCamera();
        int tier = PickTier();

        GameObject go = Instantiate(debrisPrefab, spawnPos, Quaternion.identity);
        go.transform.localScale = Vector3.one * (0.6f + tier * 0.4f); // small 0.6, medium 1.0, large 1.4

        // Direction: usually toward the player with some spread, sometimes random
        Vector2 dir;
        if (Random.value < aimAtPlayerChance)
        {
            dir = ((Vector2)player.position - spawnPos).normalized;
            dir = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f)) * dir;
        }
        else
        {
            dir = Random.insideUnitCircle.normalized;
        }

        float speed = baseSpeed + DifficultyLevel * speedPerLevel + Random.Range(-speedVariance, speedVariance);
        float healthMult = 1f + DifficultyLevel * healthPerLevel;
        float damageMult = 1f + DifficultyLevel * damagePerLevel;

        if (go.TryGetComponent(out Debris debris))
            debris.Init(tier, healthMult, damageMult, dir * Mathf.Max(0.5f, speed), debrisPrefab);

    }

    // Bigger debris becomes more common as difficulty rises
    private int PickTier()
    {
        float largeChance = Mathf.Clamp01(0.15f + DifficultyLevel * 0.05f);
        float mediumChance = 0.35f;
        float r = Random.value;
        if (r < largeChance) return 2;
        if (r < largeChance + mediumChance) return 1;
        return 0;
    }

    private Vector2 GetSpawnPositionOutsideCamera()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float radius = Mathf.Sqrt(halfW * halfW + halfH * halfH) + spawnMargin; // circumscribes the view

        Vector2 center = cam.transform.position;
        float angle = Random.value * Mathf.PI * 2f;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
