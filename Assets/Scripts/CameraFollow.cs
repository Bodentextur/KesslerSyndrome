using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothing = 6f;        // higher = snappier, 0 = no smoothing (locked to the player)
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Shake on damage (optional)")]
    [SerializeField] private PlayerController player;     // leave empty to disable
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeMagnitude = 0.3f;

    private float shakeTimer;
    private float currentDuration;
    private float currentMagnitude;
    private Vector2 followPosition;

    private void Start()
    {
        if (target != null)
            followPosition = (Vector2)target.position + offset;

        if (player != null) player.OnDamaged += HandleDamaged;
    }

    private void OnDestroy()
    {
        if (player != null) player.OnDamaged -= HandleDamaged;
    }

    // LateUpdate runs after the player has moved this frame, which avoids camera jitter
    private void LateUpdate()
    {
        if (target == null) return;

        Vector2 desired = (Vector2)target.position + offset;

        // Frame-rate independent smoothing: behaves the same at 30 or 200 FPS
        float k = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        followPosition = Vector2.Lerp(followPosition, desired, k);

        Vector2 shakeOffset = Vector2.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float strength = currentMagnitude * Mathf.Clamp01(shakeTimer / currentDuration); // fades out
            shakeOffset = Random.insideUnitCircle * strength;
        }

        Vector2 final = followPosition + shakeOffset;
        transform.position = new Vector3(final.x, final.y, transform.position.z); // keep the camera's z
    }

    private void HandleDamaged(float damage)
    {
        Shake(shakeDuration, shakeMagnitude);
    }

    /// <summary>Call from anywhere, e.g. an EMP pulse or a big explosion.</summary>
    public void Shake(float duration, float magnitude)
    {
        // Don't let a weak shake cut off a stronger one that is still running
        if (shakeTimer > 0f && magnitude < currentMagnitude * (shakeTimer / currentDuration)) return;

        currentDuration = Mathf.Max(0.01f, duration);
        currentMagnitude = magnitude;
        shakeTimer = currentDuration;
    }
}
