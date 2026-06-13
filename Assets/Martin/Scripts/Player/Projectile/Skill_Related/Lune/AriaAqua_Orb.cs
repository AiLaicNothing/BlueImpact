using UnityEngine;
using System;

public class AriaAqua_Orb : MonoBehaviour
{
    [Header("Spiral")]
    public float spiralStrength = 5f;

    private Rigidbody rb;

    private Vector3 targetPoint;
    private Transform lockTarget;

    private float travelTime;
    private float timer;

    private Action onArrive;

    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 point, Transform target, float time, Action callback)
    {
        targetPoint = point;
        lockTarget = target;

        travelTime = time;
        onArrive = callback;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        initialized = true;
    }

    void FixedUpdate()
    {
        if (!initialized) return;

        timer += Time.fixedDeltaTime;

        float t = timer / travelTime;

        if (t >= 1f)
        {
            ReachCenter();
            return;
        }

        Vector3 currentCenter;

        if (lockTarget != null)
        {
            currentCenter = lockTarget.position;
        }
        else
        {
            currentCenter = targetPoint;
        }

        Vector3 toCenter = (currentCenter - transform.position).normalized;

        Vector3 perpendicular = Vector3.Cross(toCenter, Vector3.up).normalized;

        Vector3 finalDir = (toCenter + perpendicular * spiralStrength * (1f - t)).normalized;

        float distance = Vector3.Distance(transform.position, currentCenter);

        float speed = distance / (travelTime - timer + 0.01f);

        rb.linearVelocity = finalDir * speed;

        // Visual rotation
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    void ReachCenter()
    {
        rb.linearVelocity = Vector3.zero;

        onArrive?.Invoke();

        Destroy(gameObject);
    }
}
