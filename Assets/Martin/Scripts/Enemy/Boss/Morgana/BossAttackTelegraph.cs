using System;
using System.Collections;
using UnityEngine;

// Muestra en el suelo la zona donde caera el golpe y activa los trails de espada/cola.
// Va en el mismo objeto que Boss_MedusaMorgana.
public class BossAttackTelegraph : MonoBehaviour
{
    [Header("Aviso de dano (linea en el suelo)")]
    [SerializeField] private Material lineMaterial;                      // opcional, si no usa Sprites/Default
    [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float hitLineWidth = 0.2f;
    [SerializeField] private float startAlpha = 0.2f;                    // transparencia inicial, sube a 1
    [SerializeField] private float groundOffset = 0.05f;                 // altura sobre el suelo para que no se esconda
    [SerializeField] private float holdAfterWarning = 0.3f;              // cuanto queda visible tras el aviso si no hay golpe
    [SerializeField] private float hitFlashTime = 0.25f;                 // destello al golpear

    [Header("Trail de espada y cola")]
    [SerializeField] private TrailRenderer swordTrail;                   // hijo de la punta de la espada (modelo fase 1)
    [SerializeField] private TrailRenderer tailTrail;                    // hijo de la punta de la cola (modelo fase 1)
    [SerializeField] private float trailTime = 0.3f;                     // duracion de la estela

    private LineRenderer line;
    private Coroutine routine;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.loop = false;
        line.enabled = false;
        line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));

        if (swordTrail != null)
        {
            swordTrail.time = trailTime;
            swordTrail.emitting = false;
        }

        if (tailTrail != null)
        {
            tailTrail.time = trailTime;
            tailTrail.emitting = false;
        }
    }

    // getPoints se llama cada frame, asi la zona sigue al boss si se mueve
    public void StartTelegraph(Func<Vector3[]> getPoints, float duration)
    {
        StopRoutine();
        routine = StartCoroutine(WarningRoutine(getPoints, duration));
    }

    public void OnHit()
    {
        StopRoutine();
        routine = StartCoroutine(HitFlashRoutine());
    }

    public void CancelAll()
    {
        StopRoutine();
        Hide();
        SetTrail(true, false);
        SetTrail(false, false);
    }

    public void SetTrail(bool isSword, bool on)
    {
        TrailRenderer t = isSword ? swordTrail : tailTrail;
        if (t != null) t.emitting = on;
    }

    public void Hide()
    {
        if (line != null) line.enabled = false;
    }

    // Sector circular: arranca en 'startDeg' desde forward y abarca 'sweepDeg' grados
    public static Vector3[] Pie(Vector3 center, Vector3 forward, float radius, float startDeg, float sweepDeg, int segments = 48)
    {
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        segments = Mathf.Max(4, segments);
        Vector3[] pts = new Vector3[segments + 2];
        pts[0] = center;

        for (int i = 0; i <= segments; i++)
        {
            float a = startDeg + sweepDeg * (i / (float)segments);
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * forward;
            pts[i + 1] = center + dir * radius;
        }

        return pts;
    }

    // Cono frontal: empieza en el origen y abarca 'angleDeg' grados centrados en forward
    public static Vector3[] Sector(Vector3 origin, Vector3 forward, float radius, float angleDeg, int segments = 24)
    {
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        segments = Mathf.Max(2, segments);
        float half = angleDeg * 0.5f;

        Vector3[] pts = new Vector3[segments + 2];
        pts[0] = origin;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * forward;
            pts[i + 1] = origin + dir * radius;
        }

        return pts;
    }

    // Circulo alrededor de un centro, en el plano horizontal
    public static Vector3[] Circle(Vector3 center, float radius, int segments = 48)
    {
        segments = Mathf.Max(8, segments);
        Vector3[] pts = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            pts[i] = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
        }

        return pts;
    }

    // Rectangulo que empieza a 'offset' delante del origen y se extiende 'depth' hacia adelante
    public static Vector3[] Rect(Vector3 origin, Vector3 forward, float offset, float width, float depth)
    {
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 start = origin + forward * offset;

        Vector3 a = start - right * width * 0.5f;
        Vector3 b = start + right * width * 0.5f;

        return new[]
        {
            a,
            b,
            b + forward * depth,
            a + forward * depth,
        };
    }

    private void StopRoutine()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator WarningRoutine(Func<Vector3[]> getPoints, float duration)
    {
        line.enabled = true;

        float t = 0f;
        while (t < duration)
        {
            float k = duration > 0f ? t / duration : 1f;

            SetLook(warningColor, Mathf.Lerp(startAlpha, 1f, k), lineWidth);
            SetPoints(getPoints());

            t += Time.deltaTime;
            yield return null;
        }

        SetPoints(getPoints());
        yield return new WaitForSeconds(holdAfterWarning);

        Hide();
        routine = null;
    }

    private IEnumerator HitFlashRoutine()
    {
        line.enabled = true;
        SetLook(hitColor, 1f, hitLineWidth);

        yield return new WaitForSeconds(hitFlashTime);

        Hide();
        routine = null;
    }

    private void SetLook(Color c, float alpha, float width)
    {
        c.a = alpha;
        line.startColor = c;
        line.endColor = c;
        line.startWidth = width;
        line.endWidth = width;
    }

    private void SetPoints(Vector3[] pts)
    {
        if (pts == null || pts.Length == 0) return;

        line.positionCount = pts.Length + 1;

        Vector3 lift = Vector3.up * groundOffset;

        for (int i = 0; i < pts.Length; i++)
        {
            line.SetPosition(i, pts[i] + lift);
        }

        // Cierra la figura volviendo al primer punto
        line.SetPosition(pts.Length, pts[0] + lift);
    }
}