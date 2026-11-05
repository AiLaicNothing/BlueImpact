using System;
using System.Collections;
using UnityEngine;

// Configuracion de un meteorito de Meteor Shower. Se edita en el Inspector de Boss_MedusaMorgana.
[Serializable]
public class MeteorConfig
{
    [Header("Dano")]
    public float damage = 15f;
    public float impactRadius = 1.6f;   // radio del area que dana al caer
    public float bodyRadius = 0.4f;     // radio del cuerpo del jugador
    public float maxHitHeight = 2.5f;   // si el jugador esta mas alto que esto sobre el suelo (saltando), no le pega

    [Header("Caida")]
    public float fallTime = 1.1f;       // segundos entre la marca en el suelo y el impacto
    public float fallHeight = 14f;      // desde que altura cae el modelo del meteorito
    public float horizontalOffset = 3f; // cae un poco inclinado, no perfectamente vertical
    public LayerMask groundMask;        // suelo de la arena. Vacio = usa la altura del jugador

    [Header("Aviso en el suelo")]
    public Color warningColor = new Color(1f, 0.45f, 0.1f, 0.9f);
    public Color fillColor = new Color(1f, 0.15f, 0.05f, 0.9f);
    public float warningLineWidth = 0.08f;

    [Header("Visual (opcional)")]
    public GameObject meteorPrefab;     // modelo/VFX que cae. Vacio = solo el aviso en el suelo
    public GameObject impactVfx;        // PREFAB al impactar
    public float impactVfxLifetime = 2f;
}

// Un meteorito: marca el suelo, cae y hace dano en area al impactar.
// Se crea con BossMeteor.Launch y se destruye solo.
public class BossMeteor : MonoBehaviour
{
    private static Material sharedMaterial;

    private MeteorConfig cfg;
    private Transform target;
    private Vector3 groundPoint;
    private Action<Vector3> onImpact;

    private LineRenderer outerRing;
    private LineRenderer innerRing;
    private GameObject falling;

    private const int RingSegments = 40;

    public static BossMeteor Launch(Vector3 point, MeteorConfig config, Transform player,
                                    Action<Vector3> onFallStart = null, Action<Vector3> onImpact = null)
    {
        if (config == null) return null;

        GameObject go = new GameObject("BossMeteor");
        BossMeteor m = go.AddComponent<BossMeteor>();
        m.Setup(point, config, player, onFallStart, onImpact);

        return m;
    }

    private void Setup(Vector3 point, MeteorConfig config, Transform player, Action<Vector3> onFallStart, Action<Vector3> impactCallback)
    {
        cfg = config;
        target = player;
        onImpact = impactCallback;
        groundPoint = SnapToGround(point);
        transform.position = groundPoint;

        outerRing = CreateRing("Ring_Outer");
        innerRing = CreateRing("Ring_Fill");

        DrawRing(outerRing, cfg.impactRadius, cfg.warningColor);
        DrawRing(innerRing, 0.01f, cfg.fillColor);

        if (cfg.meteorPrefab != null)
        {
            Vector3 side = UnityEngine.Random.insideUnitSphere;
            side.y = 0f;
            falling = Instantiate(cfg.meteorPrefab, StartPos(side), Quaternion.identity);
            fallSide = side;
        }

        onFallStart?.Invoke(groundPoint);

        StartCoroutine(FallRoutine());
    }

    private Vector3 fallSide;

    private Vector3 StartPos(Vector3 side)
    {
        Vector3 offset = side.sqrMagnitude > 0.0001f ? side.normalized * cfg.horizontalOffset : Vector3.zero;
        return groundPoint + Vector3.up * cfg.fallHeight + offset;
    }

    private IEnumerator FallRoutine()
    {
        float t = 0f;
        float duration = Mathf.Max(0.05f, cfg.fallTime);
        Vector3 start = StartPos(fallSide);

        while (t < duration)
        {
            float k = t / duration;

            // El circulo interno crece hasta el borde: muestra cuanto falta para el impacto
            DrawRing(innerRing, Mathf.Max(0.01f, cfg.impactRadius * k), cfg.fillColor);

            if (falling != null)
            {
                // Acelera al caer (k^2) para que se sienta pesado
                falling.transform.position = Vector3.Lerp(start, groundPoint, k * k);

                Vector3 dir = groundPoint - start;
                if (dir.sqrMagnitude > 0.0001f) falling.transform.rotation = Quaternion.LookRotation(dir.normalized);
            }

            t += Time.deltaTime;
            yield return null;
        }

        Impact();
    }

    private void Impact()
    {
        if (falling != null) Destroy(falling);

        if (cfg.impactVfx != null)
        {
            GameObject vfx = Instantiate(cfg.impactVfx, groundPoint, Quaternion.identity);
            Destroy(vfx, cfg.impactVfxLifetime);
        }

        onImpact?.Invoke(groundPoint);

        TryDamage();

        Destroy(gameObject);
    }

    private void TryDamage()
    {
        if (target == null) return;

        Vector3 p = target.position;

        float height = p.y - groundPoint.y;
        if (height > cfg.maxHitHeight) return;

        Vector3 flat = p - groundPoint;
        flat.y = 0f;

        if (flat.magnitude > cfg.impactRadius + cfg.bodyRadius) return;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo
        {
            damage = cfg.damage,
            hitDirection = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.up,
        };

        damageable.TakeDamage(in info);
    }

    private Vector3 SnapToGround(Vector3 point)
    {
        if (cfg.groundMask.value == 0) return point;

        Vector3 from = point + Vector3.up * 20f;

        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 60f, cfg.groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return point;
    }

    private LineRenderer CreateRing(string label)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = RingSegments;
        lr.material = GetMaterial();
        lr.startWidth = cfg.warningLineWidth;
        lr.endWidth = cfg.warningLineWidth;

        return lr;
    }

    private void DrawRing(LineRenderer lr, float radius, Color color)
    {
        if (lr == null) return;

        lr.startColor = color;
        lr.endColor = color;

        Vector3 lift = Vector3.up * 0.05f;

        for (int i = 0; i < RingSegments; i++)
        {
            float a = i / (float)RingSegments * Mathf.PI * 2f;
            lr.SetPosition(i, groundPoint + lift + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }

    private static Material GetMaterial()
    {
        if (sharedMaterial == null)
        {
            sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return sharedMaterial;
    }
}
