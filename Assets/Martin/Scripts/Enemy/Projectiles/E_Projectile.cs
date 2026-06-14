using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class E_Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    private Vector3 dir;
    private DamageInfo info;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    private Rigidbody rb;


    public void InitProj(float damage, Vector3 dir)
    {

        info = new DamageInfo
        {
            damage = damage,
            hitDirection = transform.forward,
        };


        this.dir = dir;

        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }

        Destroy(gameObject, 3f);
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        target.TakeDamage(info);

        Destroy(gameObject);
    }
}
