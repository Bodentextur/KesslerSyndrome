using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private GameObject hitVfxPrefab; // optional small Shuriken spark

    private float damage;
    private float speed;
    private int pierceLeft;

    public void Init(float damage, float speed, float lifetime, int pierce)
    {
        this.damage = damage;
        this.speed = speed;
        pierceLeft = pierce;
        Destroy(gameObject, lifetime);
    }

    private void Awake()
    {
        // Needs a trigger collider; Debris already has a Rigidbody2D, so trigger events fire.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        // Moves along its own right vector (sprite should point right at rotation 0)
        transform.Translate(Vector3.right * (speed * Time.deltaTime), Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Debris debris)) return;

        debris.TakeDamage(damage);

        if (hitVfxPrefab != null)
            Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);

        if (pierceLeft > 0) pierceLeft--;
        else Destroy(gameObject);
    }
}
