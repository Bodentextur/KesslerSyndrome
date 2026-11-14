using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 10f;   
    [SerializeField] private float deceleration = 4f;    
    [SerializeField] private float turnBoost = 2f;   

    [Header("Visuals")]
    [SerializeField] private bool rotateTowardVelocity = true;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float rotationOffset = -90f;

    [Header("Boost Particles")]
    [SerializeField] private ParticleSystem boostParticles;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invulnerableTime = 0.5f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

    public event Action<float> OnDamaged;      // passes damage amount
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDied;

    private Rigidbody2D rb;
    private Vector2 input;
    private float invulnerableUntil;
    public bool isDead;
    private float targetAngle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (boostParticles != null)
            boostParticles.Stop();
    }

    private void Update()
    {
        if (isDead) return;
        if (Time.timeScale == 0f) { input = Vector2.zero; return; }

        input = ReadInput();

        if (rotateTowardVelocity && GetVelocity().sqrMagnitude > 0.5f)
        {
            Vector2 v = GetVelocity();
            targetAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + rotationOffset;
        }

        UpdateBoostParticles();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Vector2 velocity = GetVelocity();

        if (input.sqrMagnitude > 0.01f)
        {
            Vector2 targetVelocity = input * maxSpeed;
            float accel = acceleration;

            // Reversing direction gets a boost so control stays responsive
            if (Vector2.Dot(velocity, input) < 0f) accel *= turnBoost;

            velocity = Vector2.MoveTowards(velocity, targetVelocity, accel * Time.fixedDeltaTime);
        }
        else
        {
            // No input: drift to a stop
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }

        if (rotateTowardVelocity)
        {
            float angle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(angle);
        }

        SetVelocity(velocity);
    }

    private void UpdateBoostParticles()
    {
        if (boostParticles == null) return;

        if (input.sqrMagnitude > 0.01f)
        {
            if (!boostParticles.isPlaying)
                boostParticles.Play();
        }
        else
        {
            if (boostParticles.isPlaying)
                boostParticles.Stop();
        }
    }

    // ---------- Health ----------

    public void TakeDamage(float amount)
    {
        if (isDead || Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + invulnerableTime;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        SetVelocity(Vector2.zero);

        foreach (WeaponBase w in GetComponentsInChildren<WeaponBase>())
            w.enabled = false;

        if (boostParticles != null)
            boostParticles.Stop();

        OnDied?.Invoke();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent(out Debris debris))
        {
            TakeDamage(debris.ContactDamage);
        }
    }

    // ---------- Helpers ----------

    private Vector2 ReadInput()
    {
        Vector2 raw = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) raw.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) raw.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) raw.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) raw.x -= 1f;
        }
#else
        raw.x = Input.GetAxisRaw("Horizontal");
        raw.y = Input.GetAxisRaw("Vertical");
#endif
        return Vector2.ClampMagnitude(raw, 1f); 
    }

    private Vector2 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVelocity(Vector2 v)
    {
        rb.linearVelocity = v;
    }
}