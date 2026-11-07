using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Crea hitboxes primitivas (capsulas y esferas) colgadas de los huesos de Morgana,
// en fase 1 y fase 2. Son triggers: no empujan nada, solo sirven para que el
// OverlapBox del jugador detecte el cuerpo con la forma real y animada.
//
// Por que no MeshCollider: los dos modelos son skinned, y un MeshCollider usa la
// malla en bind pose, asi que nunca sigue la animacion. Estas primitivas cuelgan
// de los huesos, asi que si.
//
// Los dos rigs NO tienen los mismos nombres de hueso, por eso cada parte se
// resuelve con una lista de candidatos.
public static class MorganaHitboxSetup
{
    private const string BossPrefabPath = "Assets/Martin/Prefabs/Enemy/Boss/Morgana.prefab";

    // Prefijo de todo lo que crea esta herramienta. Borrar usa el mismo prefijo,
    // asi que no toca nada que hayas puesto a mano con otro nombre.
    private const string Prefix = "HB_";

    private const string EnemyTag = "Enemy";

    // Cuantas capsulas se reparten a lo largo de cada cadena. Mas = sigue mejor
    // la curva cuando se dobla, pero mas colliders.
    private const int BodySegments = 6;
    private const int TailSegments = 5;
    private const int NeckSegments = 2;
    private const int ArmSegments = 2;

    // Radio de cada hitbox como fraccion del largo del hueso que cubre.
    // Las cadenas van de 'start' a 'end' estrechandose.
    private const float BodyRadiusStart = 0.45f;
    private const float BodyRadiusEnd = 0.34f;
    private const float TailRadiusStart = 0.30f;
    private const float TailRadiusEnd = 0.16f;
    private const float NeckRadiusScale = 0.40f;
    private const float ArmRadiusScale = 0.28f;
    private const float HandRadiusScale = 0.55f;
    private const float HeadRadiusScale = 0.60f;

    // ---------------------------------------------------------------- Partes

    private enum Kind { Sphere, Capsule, Chain }

    private class Part
    {
        public string name;
        public Kind kind;
        public string[] start;
        public string[] end;         // Capsule y Chain
        public string[] reference;   // solo Sphere: contra que hueso se mide el radio
        public float radius = 0.3f;  // fraccion del largo
        public float radiusEnd = -1; // solo Chain: si es >= 0, estrecha hasta aqui
        public int segments = 4;     // solo Chain
    }

    // Fase 1: spine + spine1..spine13, neck..neck_last, brazos a-b (solo mano en A),
    // cola tail1..tailN
    private static readonly Part[] PhaseOne =
    {
        new Part { name = "Head", kind = Kind.Sphere,
                   start = new[]{"head"}, reference = new[]{"neck_last","neck2","neck1","neck"},
                   radius = HeadRadiusScale },

        new Part { name = "Neck", kind = Kind.Chain, segments = NeckSegments,
                   start = new[]{"neck"}, end = new[]{"neck_last","neck2","head"},
                   radius = NeckRadiusScale },

        new Part { name = "Chest", kind = Kind.Capsule,
                   start = new[]{"chest"}, end = new[]{"neck","neck1"},
                   radius = BodyRadiusStart },

        new Part { name = "Body", kind = Kind.Chain, segments = BodySegments,
                   start = new[]{"spine"}, end = new[]{"chest"},
                   radius = BodyRadiusStart, radiusEnd = BodyRadiusEnd },

        new Part { name = "Tail", kind = Kind.Chain, segments = TailSegments,
                   start = new[]{"tail1","tail"},
                   radius = TailRadiusStart, radiusEnd = TailRadiusEnd },

        new Part { name = "ArmA", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"hombro_a"}, end = new[]{"codo_a"},
                   radius = ArmRadiusScale },

        new Part { name = "ForearmA", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"codo_a"}, end = new[]{"origin_hand_a","palma_a"},
                   radius = ArmRadiusScale },

        new Part { name = "HandA", kind = Kind.Sphere,
                   start = new[]{"origin_hand_a","palma_a"}, reference = new[]{"indice_a","medio_a","pulgar_a"},
                   radius = HandRadiusScale },

        new Part { name = "ArmB", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"hombro_b"}, end = new[]{"codo_b"},
                   radius = ArmRadiusScale },

        new Part { name = "ForearmB", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"codo_b"}, end = new[]{"origin_hand_b","palma_b"},
                   radius = ArmRadiusScale },
    };

    // Fase 2: spine_lower + spine_lower1..11, neck1..neck6, brazos con clavicle y
    // palma en los dos lados, cola tail..tail14_end
    private static readonly Part[] PhaseTwo =
    {
        new Part { name = "Head", kind = Kind.Sphere,
                   start = new[]{"head"}, reference = new[]{"neck6","neck5","neck1"},
                   radius = HeadRadiusScale },

        new Part { name = "Neck", kind = Kind.Chain, segments = NeckSegments + 1,
                   start = new[]{"neck1"}, end = new[]{"neck6","head"},
                   radius = NeckRadiusScale },

        new Part { name = "Chest", kind = Kind.Capsule,
                   start = new[]{"chest"}, end = new[]{"neck1"},
                   radius = BodyRadiusStart },

        new Part { name = "Body", kind = Kind.Chain, segments = BodySegments,
                   start = new[]{"spine_lower"}, end = new[]{"chest"},
                   radius = BodyRadiusStart, radiusEnd = BodyRadiusEnd },

        new Part { name = "Tail", kind = Kind.Chain, segments = TailSegments,
                   start = new[]{"tail","tail1"}, end = new[]{"tail14_end"},
                   radius = TailRadiusStart, radiusEnd = TailRadiusEnd },

        new Part { name = "ClavicleA", kind = Kind.Capsule,
                   start = new[]{"clavicle_a"}, end = new[]{"hombro_a"},
                   radius = ArmRadiusScale },

        new Part { name = "ArmA", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"hombro_a"}, end = new[]{"codo_a"},
                   radius = ArmRadiusScale },

        new Part { name = "ForearmA", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"codo_a"}, end = new[]{"palma_a"},
                   radius = ArmRadiusScale },

        new Part { name = "HandA", kind = Kind.Sphere,
                   start = new[]{"palma_a"}, reference = new[]{"indice_a","medui_a","pulgar_a"},
                   radius = HandRadiusScale },

        new Part { name = "ClavicleB", kind = Kind.Capsule,
                   start = new[]{"clavicle_b"}, end = new[]{"hombro_b"},
                   radius = ArmRadiusScale },

        new Part { name = "ArmB", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"hombro_b"}, end = new[]{"codo_b"},
                   radius = ArmRadiusScale },

        new Part { name = "ForearmB", kind = Kind.Chain, segments = ArmSegments,
                   start = new[]{"codo_b"}, end = new[]{"palma_b"},
                   radius = ArmRadiusScale },

        new Part { name = "HandB", kind = Kind.Sphere,
                   start = new[]{"palma_b"}, reference = new[]{"indice_b","medui_b","pulgar_b"},
                   radius = HandRadiusScale },
    };

    // ---------------------------------------------------------------- Menus

    [MenuItem("Tools/Morgana/4. Crear hitboxes de fase 1 y 2")]
    public static void CreateHitboxes()
    {
        Run((root, phase, parts, log) =>
        {
            RemoveIn(phase);

            int made = 0;

            foreach (Part part in parts)
                made += Build(root, phase, part, log);

            log.Add($"  == {made} hitboxes creadas en {phase.name}");
        });

        if (!Physics.queriesHitTriggers)
            Debug.LogWarning("[Morgana Hitbox] Physics.queriesHitTriggers esta en false en Project Settings > Physics. " +
                             "Con esa opcion apagada el OverlapBox del jugador NO detecta triggers y estas hitboxes no serviran.");
    }

    [MenuItem("Tools/Morgana/5. Revisar hitboxes")]
    public static void Validate()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);

        if (root == null)
        {
            Debug.LogError($"[Morgana Hitbox] No se encontro el prefab en {BossPrefabPath}");
            return;
        }

        try
        {
            foreach (Transform phase in FindPhases(root))
            {
                Collider[] boxes = phase.GetComponentsInChildren<Collider>(true)
                                        .Where(c => c.name.StartsWith(Prefix))
                                        .ToArray();

                Debug.Log($"[Morgana Hitbox] {phase.name}: {boxes.Length} hitboxes\n" +
                          string.Join("\n", boxes.Select(Describe)));

                foreach (Collider c in boxes)
                {
                    if (!c.isTrigger)
                        Debug.LogWarning($"[Morgana Hitbox] {c.name} no es trigger. Va a empujar al jugador.", c);

                    if (!c.CompareTag(EnemyTag))
                        Debug.LogWarning($"[Morgana Hitbox] {c.name} no tiene el tag {EnemyTag}. El golpe del jugador lo ignora.", c);
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Morgana/6. Borrar hitboxes")]
    public static void DeleteHitboxes()
    {
        Run((root, phase, parts, log) =>
        {
            int removed = RemoveIn(phase);
            log.Add($"  == {removed} hitboxes borradas en {phase.name}");
        });
    }

    // ---------------------------------------------------------------- Motor

    private delegate void PhaseAction(GameObject root, Transform phase, Part[] parts, List<string> log);

    private static void Run(PhaseAction action)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);

        if (root == null)
        {
            Debug.LogError($"[Morgana Hitbox] No se encontro el prefab en {BossPrefabPath}");
            return;
        }

        var log = new List<string>();
        bool saved = false;

        try
        {
            Transform[] phases = FindPhases(root);

            if (phases.Length == 0)
            {
                Debug.LogError("[Morgana Hitbox] No se encontraron los hijos 'Fase 1' / 'Fase 2' dentro de 'Model'. " +
                               "Revisa los nombres en la hierarchy del prefab.");
                return;
            }

            foreach (Transform phase in phases)
            {
                Part[] parts = phase.name.Contains("2") ? PhaseTwo : PhaseOne;
                log.Add($"{phase.name}:");
                action(root, phase, parts, log);
            }

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            saved = true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        if (saved)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[Morgana Hitbox]\n" + string.Join("\n", log));
        }
    }

    private static Transform[] FindPhases(GameObject root)
    {
        return root.GetComponentsInChildren<Transform>(true)
                   .Where(t => t.name.StartsWith("Fase"))
                   .ToArray();
    }

    private static int RemoveIn(Transform phase)
    {
        Transform[] doomed = phase.GetComponentsInChildren<Transform>(true)
                                  .Where(t => t.name.StartsWith(Prefix))
                                  .ToArray();

        int removed = 0;

        foreach (Transform t in doomed)
        {
            if (t == null) continue;   // pudo morir al borrarse un padre

            Object.DestroyImmediate(t.gameObject);
            removed++;
        }

        return removed;
    }

    private static int Build(GameObject root, Transform phase, Part part, List<string> log)
    {
        Transform start = FindBone(phase, part.start);

        if (start == null)
        {
            log.Add($"  [-] {part.name}: no se encontro ningun hueso de [{string.Join(", ", part.start)}]");
            return 0;
        }

        WarnNonUniform(start, part.name);

        switch (part.kind)
        {
            case Kind.Sphere: return BuildSphere(root, phase, part, start, log);
            case Kind.Chain: return BuildChainPart(root, phase, part, start, log);
            default: return BuildCapsulePart(root, phase, part, start, log);
        }
    }

    private static int BuildSphere(GameObject root, Transform phase, Part part, Transform start, List<string> log)
    {
        Transform reference = FindBone(phase, part.reference);

        float span = reference != null
            ? Vector3.Distance(start.position, reference.position)
            : 0f;

        if (span <= 0.0001f)
        {
            log.Add($"  [-] {part.name}: no hay referencia para medir el radio en '{start.name}', se salta");
            return 0;
        }

        GameObject go = NewHitbox(root, start, part.name);

        SphereCollider sphere = go.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = ToLocal(start, span) * part.radius;

        log.Add($"  [+] {part.name}: esfera en '{start.name}' (r={sphere.radius:0.###})");
        return 1;
    }

    private static int BuildCapsulePart(GameObject root, Transform phase, Part part, Transform start, List<string> log)
    {
        Transform end = FindBone(phase, part.end);

        if (end == null)
        {
            log.Add($"  [-] {part.name}: no se encontro el hueso final de [{string.Join(", ", part.end)}]");
            return 0;
        }

        return Capsule(root, start, end, part.name, part.radius, log) ? 1 : 0;
    }

    private static int BuildChainPart(GameObject root, Transform phase, Part part, Transform start, List<string> log)
    {
        Transform end = FindBone(phase, part.end);
        List<Transform> chain = ChainBetween(start, end);

        if (chain.Count < 2)
        {
            log.Add($"  [-] {part.name}: la cadena desde '{start.name}' tiene {chain.Count} hueso(s), se salta");
            return 0;
        }

        List<Transform> sampled = Sample(chain, part.segments + 1);
        int pairs = sampled.Count - 1;
        int made = 0;

        for (int i = 0; i < pairs; i++)
        {
            // Si la parte estrecha, el radio baja parejo del primer al ultimo tramo.
            float radius = part.radiusEnd >= 0f && pairs > 1
                ? Mathf.Lerp(part.radius, part.radiusEnd, i / (pairs - 1f))
                : part.radius;

            if (Capsule(root, sampled[i], sampled[i + 1], $"{part.name}_{i + 1}", radius, log))
                made++;
        }

        log.Add($"  [i] {part.name}: cadena de {chain.Count} huesos ('{chain[0].name}' -> '{chain[chain.Count - 1].name}'), {made} tramos");
        return made;
    }

    private static bool Capsule(GameObject root, Transform start, Transform end, string name, float radiusScale, List<string> log)
    {
        // Todo se calcula en el espacio local del hueso: asi la capsula acompana
        // al hueso sin importar como este rotado en la pose.
        Vector3 localEnd = start.InverseTransformPoint(end.position);
        float length = localEnd.magnitude;

        if (length <= 0.0001f)
        {
            log.Add($"  [-] {name}: '{start.name}' y '{end.name}' estan en el mismo punto, se salta");
            return false;
        }

        GameObject go = NewHitbox(root, start, name);

        // Se orienta el hijo hacia el hueso final y la capsula va sobre su eje Z.
        // Asi queda exacta aunque la cadena no sea paralela a ningun eje del hueso.
        go.transform.localRotation = Quaternion.LookRotation(localEnd, PickUp(localEnd));

        CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.direction = 2;                       // Z
        capsule.height = length;
        capsule.radius = length * radiusScale;
        capsule.center = new Vector3(0f, 0f, length * 0.5f);

        log.Add($"  [+] {name}: capsula '{start.name}' -> '{end.name}' (h={length:0.###}, r={capsule.radius:0.###})");
        return true;
    }

    private static GameObject NewHitbox(GameObject root, Transform bone, string name)
    {
        var go = new GameObject(Prefix + name);

        go.transform.SetParent(bone, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // Sin el tag el OverlapBox del jugador lo descarta; sin la capa correcta
        // puede que ni entre en la query.
        go.tag = EnemyTag;
        go.layer = root.layer;

        return go;
    }

    // ---------------------------------------------------------------- Huesos

    // Descarta los controles de animacion del rig de Maya (_Ctrl, _Ctrl_Grp):
    // no deforman nada y meterles colliders no sirve.
    private static bool IsDeformBone(Transform t) =>
        !t.name.EndsWith("_Ctrl") && !t.name.Contains("_Ctrl_Grp") && !t.name.StartsWith(Prefix);

    private static Transform FindBone(Transform phase, string[] candidates)
    {
        if (candidates == null) return null;

        Transform[] all = phase.GetComponentsInChildren<Transform>(true)
                               .Where(IsDeformBone)
                               .ToArray();

        foreach (string wanted in candidates)
        {
            Transform exact = all.FirstOrDefault(t => t.name == wanted);
            if (exact != null) return exact;
        }

        // Segunda pasada sin distinguir mayusculas: los dos rigs mezclan
        // 'snake_J' con 'snake_j', asi que conviene.
        foreach (string wanted in candidates)
        {
            Transform loose = all.FirstOrDefault(t => string.Equals(t.name, wanted, System.StringComparison.OrdinalIgnoreCase));
            if (loose != null) return loose;
        }

        return null;
    }

    // Devuelve los huesos que van de 'start' a 'end'. Si no hay 'end', o si no son
    // parientes, cae a seguir la rama mas larga que sale de 'start'.
    private static List<Transform> ChainBetween(Transform start, Transform end)
    {
        if (end != null && end != start)
        {
            List<Transform> down = WalkUp(end, start);
            if (down != null) return down;

            List<Transform> up = WalkUp(start, end);
            if (up != null) return up;
        }

        return LongestBranch(start);
    }

    // Sube por los padres desde 'from' buscando 'ancestor'. Devuelve la ruta
    // ordenada de ancestro a descendiente, o null si no es pariente.
    private static List<Transform> WalkUp(Transform from, Transform ancestor)
    {
        var path = new List<Transform>();
        Transform current = from;

        while (current != null)
        {
            path.Add(current);

            if (current == ancestor)
            {
                path.Reverse();
                return path;
            }

            current = current.parent;
        }

        return null;
    }

    // Baja por la jerarquia siguiendo siempre al hijo que mas lejos llega.
    // Sirve para la cola de fase 1, que no tiene un hueso final con nombre fijo.
    private static List<Transform> LongestBranch(Transform start)
    {
        var chain = new List<Transform> { start };
        Transform current = start;

        while (true)
        {
            Transform next = null;
            int best = -1;

            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                if (!IsDeformBone(child)) continue;

                int depth = Depth(child);

                if (depth > best)
                {
                    best = depth;
                    next = child;
                }
            }

            if (next == null) break;

            chain.Add(next);
            current = next;
        }

        return chain;
    }

    private static int Depth(Transform t)
    {
        int deepest = 0;

        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            if (!IsDeformBone(child)) continue;

            int depth = Depth(child);
            if (depth > deepest) deepest = depth;
        }

        return deepest + 1;
    }

    // Reduce la cadena a 'wanted' nodos repartidos parejo, siempre incluyendo
    // el primero y el ultimo.
    private static List<Transform> Sample(List<Transform> chain, int wanted)
    {
        if (wanted < 2) wanted = 2;
        if (chain.Count <= wanted) return new List<Transform>(chain);

        var result = new List<Transform>();

        for (int i = 0; i < wanted; i++)
        {
            int index = Mathf.RoundToInt(i * (chain.Count - 1f) / (wanted - 1f));
            Transform pick = chain[index];

            if (!result.Contains(pick)) result.Add(pick);
        }

        return result;
    }

    // ---------------------------------------------------------------- Varios

    // SphereCollider.radius va en unidades locales del hueso.
    private static float ToLocal(Transform bone, float worldDistance)
    {
        Vector3 scale = bone.lossyScale;
        float factor = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))));

        return worldDistance / factor;
    }

    private static void WarnNonUniform(Transform bone, string partName)
    {
        Vector3 s = bone.lossyScale;
        float max = Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        float min = Mathf.Min(Mathf.Abs(s.x), Mathf.Min(Mathf.Abs(s.y), Mathf.Abs(s.z)));

        if (min > 0.0001f && max / min > 1.05f)
            Debug.LogWarning($"[Morgana Hitbox] El hueso '{bone.name}' ({partName}) tiene escala no uniforme {s}. " +
                             "Unity deforma las primitivas con la escala mayor, asi que ese hitbox puede quedar mas gordo de lo esperado.");
    }

    // Evita que LookRotation reviente cuando la cadena cae justo sobre el eje Y local.
    private static Vector3 PickUp(Vector3 forward) =>
        Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;

    private static string Describe(Collider c)
    {
        if (c is CapsuleCollider cap)
            return $"    {c.name} @ {c.transform.parent.name}  capsula h={cap.height:0.###} r={cap.radius:0.###}";

        if (c is SphereCollider sph)
            return $"    {c.name} @ {c.transform.parent.name}  esfera r={sph.radius:0.###}";

        return $"    {c.name} @ {c.transform.parent.name}  {c.GetType().Name}";
    }
}
