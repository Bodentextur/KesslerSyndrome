using UnityEngine;

public class Scrap : MonoBehaviour
{
    [Header("Value")]
    [SerializeField] private int xpValue = 1;

    [Header("Scatter (when dropped)")]
    [SerializeField] private float scatterSpeed = 2.5f;
    [SerializeField] private float scatterDrag = 4f;

    [Header("Magnet")]
    [SerializeField] private float magnetAcceleration = 30f;
    [SerializeField] private float maxMagnetSpeed = 14f;
    [SerializeField] private float pickupDistance = 0.4f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 20f;
    [SerializeField] private float blinkTime = 3f; // blinks during the last few seconds

    [Header("Feedback")]
    [SerializeField] private GameObject pickupVfxPrefab;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.3f;

    private SpriteRenderer sr;
    private Vector2 scatterVelocity;
    private float magnetSpeed;
    private bool attracted;
    private float age;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        scatterVelocity = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * scatterSpeed;
    }

    private void Update()
    {
        // Time.deltaTime is 0 while paused, so scrap freezes with the game
        float dt = Time.deltaTime;

        age += dt;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        HandleBlink();

        PlayerProgression player = PlayerProgression.Instance;
        if (player == null)
        {
            Scatter(dt);
            return;
        }

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        if (dist <= pickupDistance)
        {
            Collect(player);
            return;
        }

        // Once inside the magnet radius, scrap locks on and keeps accelerating
        if (!attracted && dist <= player.MagnetRadius)
            attracted = true;

        if (attracted)
        {
            magnetSpeed = Mathf.MoveTowards(magnetSpeed, maxMagnetSpeed, magnetAcceleration * dt);
            transform.position += (Vector3)(toPlayer / dist * (magnetSpeed * dt));
        }
        else
        {
            Scatter(dt);
        }
    }

    private void Scatter(float dt)
    {
        transform.position += (Vector3)(scatterVelocity * dt);
        scatterVelocity = Vector2.Lerp(scatterVelocity, Vector2.zero, scatterDrag * dt);
    }

    private void HandleBlink()
    {
        if (sr == null) return;
        float remaining = lifetime - age;
        sr.enabled = remaining > blinkTime || Mathf.FloorToInt(remaining * 8f) % 2 == 0;
    }

    private void Collect(PlayerProgression player)
    {
        player.AddXP(xpValue);

        if (pickupVfxPrefab != null)
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);

        if (pickupSfx != null)
            AudioManager.Play(pickupSfx, sfxVolume);

        Destroy(gameObject);
    }
}
