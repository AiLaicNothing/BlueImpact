using System.Collections;
using UnityEngine;

public class SpikeZone : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float timeBtwHit = 1f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float delayActivation = 0.5f;

    [Header("Hitbox")]
    [SerializeField] private Vector3 offsetHitBox = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 hitBoxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float damage = 10f;

    [Header("Debug")]
    [SerializeField] private bool debug;
    [SerializeField] private GameObject debugBox;

    private bool isActive;
    private bool hasStarted;
    private Coroutine spikeRoutine;
    private GameObject debugBoxInstance;

    private void Update()
    {
        if (!isActive) return;

        if (debug && debugBoxInstance != null)
        {
            DebugHitBox();
        }
    }


    private IEnumerator SpikeRoutine()
    {
        yield return new WaitForSeconds(delayActivation);

        isActive = true;

        float timer = duration;
        float hitTimer = 0f;

        if (debug)
        {
            DebugHitBox();
        }

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            hitTimer += Time.deltaTime;

            if (hitTimer >= timeBtwHit)
            {
                DealDamage();
                hitTimer = 0f;
            }

            yield return null;
        }

        isActive = false;

        if (debugBoxInstance != null)
        {
            Destroy(debugBoxInstance);
            debugBoxInstance = null;
        }

        spikeRoutine = null;
        hasStarted = false;
    }

    private void DealDamage()
    {
        Vector3 center = transform.TransformPoint(offsetHitBox);
        Quaternion rotation = transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, rotation, targetMask);

        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null) continue;

            DamageInfo info = new DamageInfo
            {
                damage = damage,
   
            };

            damageable.TakeDamage(in info);
        }
    }

    private void DebugHitBox()
    {
        if (!debug || debugBox == null) return;

        Vector3 center = transform.TransformPoint(offsetHitBox);
        Quaternion rotation = transform.rotation;

        if (debugBoxInstance == null)
        {
            debugBoxInstance = Instantiate(debugBox);
        }

        debugBoxInstance.transform.SetPositionAndRotation(center, rotation);
        debugBoxInstance.transform.localScale = hitBoxSize;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasStarted) return;

        if (other.gameObject.CompareTag("Player"))
        {
            hasStarted = true;

            if (spikeRoutine != null)
            {
                StopCoroutine(spikeRoutine);
            }

            spikeRoutine = StartCoroutine(SpikeRoutine());
        }
    }
}
