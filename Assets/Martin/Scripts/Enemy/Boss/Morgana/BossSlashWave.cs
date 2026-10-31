using System;
using UnityEngine;

// Configuracion de una onda de corte. Se usa desde Boss_MedusaMorgana (espada y cola tienen una cada una).
[Serializable]
public class SlashWaveConfig
{
    public float damage = 15f;
    public float speed = 14f;           // m/s
    public float maxDistance = 12f;     // distancia maxima antes de desaparecer
    public float hitRadius = 0.6f;      // grosor de la onda para el dano
    public float bodyRadius = 0.4f;     // radio del cuerpo del jugador
    public float visualLength = 1.5f;   // largo de la estela visual
    public float visualWidth = 0.5f;    // ancho de la estela visual
    public Color color = new Color(0.4f, 1f, 0.5f, 1f);
    public LayerMask obstacles;         // paredes que la detienen
    public GameObject visualPrefab;     // opcional: si lo pones, reemplaza la linea
}

// Onda de corte: viaja en linea recta, pega al jugador si lo toca y se destruye al impactar,
// al chocar con un obstaculo o al llegar a la distancia maxima.
public class BossSlashWave : MonoBehaviour
{
    private static Material sharedMaterial;

    private SlashWaveConfig cfg;
    private Transform target;
    private Vector3 dir;
    private float traveled;
    private bool finished;
    private LineRenderer line;

    // flatten = true: viaja a ras del suelo (cola). flatten = false: sigue la direccion 3D (espada, hacia el pecho del jugador)
    public static void Launch(Vector3 origin, Vector3 direction, SlashWaveConfig config, Transform player, bool flatten = true)
    {
        if (config == null) return;

        GameObject go = new GameObject("SlashWave");
        go.transform.position = origin;

        BossSlashWave wave = go.AddComponent<BossSlashWave>();
        wave.Setup(direction, config, player, flatten);
    }

    private void Setup(Vector3 direction, SlashWaveConfig config, Transform player, bool flatten)
    {
        cfg = config;
        target = player;

        if (flatten) direction.y = 0f;
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        transform.rotation = Quaternion.LookRotation(dir);

        if (cfg.visualPrefab != null)
        {
            // Estiramiento por ejes (no escala uniforme): largo en Z, ancho en X e Y.
            // Asume que el prefab mide 1x1x1, tiene el pivote en el centro y apunta en Z.
            // La estela queda detras de la cabeza de la onda, igual que la linea.
            GameObject visual = Instantiate(cfg.visualPrefab, transform);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(cfg.visualWidth, cfg.visualWidth, cfg.visualLength);
            visual.transform.localPosition = Vector3.back * (cfg.visualLength * 0.5f);
        }
        else
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.material = GetMaterial();
            line.startWidth = cfg.visualWidth;
            line.endWidth = cfg.visualWidth;
            line.startColor = cfg.color;
            line.endColor = cfg.color;
        }
    }

    private void Update()
    {
        if (finished) return;

        Vector3 prev = transform.position;
        float step = cfg.speed * Time.deltaTime;

        transform.position += dir * step;
        traveled += step;

        if (line != null)
        {
            line.SetPosition(0, transform.position - dir * cfg.visualLength);
            line.SetPosition(1, transform.position);
        }

        // Choca con una pared
        if (Physics.Linecast(prev, transform.position, cfg.obstacles))
        {
            Finish();
            return;
        }

        // Toca al jugador
        if (target != null && TouchesTarget(prev, transform.position))
        {
            DamageTarget();
            Finish();
            return;
        }

        if (traveled >= cfg.maxDistance)
        {
            Finish();
        }
    }

    private bool TouchesTarget(Vector3 a, Vector3 b)
    {
        Vector3 feet = target.position + Vector3.up * 0.2f;
        Vector3 chest = target.position + Vector3.up * 1f;

        float reach = cfg.hitRadius + cfg.bodyRadius;

        return DistancePointToSegment(feet, a, b) <= reach
            || DistancePointToSegment(chest, a, b) <= reach;
    }

    private void DamageTarget()
    {
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        DamageInfo info = new DamageInfo
        {
            damage = cfg.damage,
            hitDirection = dir,
        };

        damageable.TakeDamage(in info);
    }

    private void Finish()
    {
        finished = true;
        Destroy(gameObject);
    }

    private static Material GetMaterial()
    {
        if (sharedMaterial == null)
        {
            sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return sharedMaterial;
    }

    private static float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float lengthSq = ab.sqrMagnitude;

        float t = lengthSq > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / lengthSq) : 0f;

        return Vector3.Distance(p, a + ab * t);
    }
}