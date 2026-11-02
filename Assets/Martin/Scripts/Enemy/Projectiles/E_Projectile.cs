using UnityEngine;

public class E_Projectile : MonoBehaviour
{
    [SerializeField] private float speed;

    [Header("Colisión con el mundo")]
    [SerializeField] private float worldArmTime = 0.15f; // ignora paredes/suelo justo al nacer (evita morir al salir pegado al piso)

    [Header("Sfx")]
    [SerializeField] private GameObject sfx; // Reservado: todavía no se reproduce

    // Velocidad configurada en el prefab. Sirve para calcular cuánto tarda en llegar (avisos en el suelo).
    public float Speed => speed;

    private DamageInfo info;
    private Rigidbody rb;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
    }

    public void InitProj(float damage, Vector3 dir)
    {
        info = new DamageInfo
        {
            damage = damage,
            hitDirection = dir,
        };

        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }

        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // El proyectil se destruye al tocar paredes o suelo
        if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            if (Time.time - spawnTime >= worldArmTime) Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player")) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        target.TakeDamage(info);

        Destroy(gameObject);
    }
}
