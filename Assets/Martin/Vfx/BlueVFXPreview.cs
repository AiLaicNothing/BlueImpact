using UnityEngine;

/// <summary>
/// Prueba visual de los shaders BlueImpact/VFX. Anima las propiedades en bucle (edit mode y play mode).
/// Usa MaterialPropertyBlock: no modifica los materiales. Borrar el objeto "VFX_Preview" cuando no se necesite.
/// </summary>
[ExecuteAlways]
public class BlueVFXPreview : MonoBehaviour
{
    public Renderer withered, shockwave, magma, warn, fireball;
    public float loopTime = 3f;

    private MaterialPropertyBlock mpb;
    private static readonly int Reveal = Shader.PropertyToID("_Reveal");
    private static readonly int Dissolve = Shader.PropertyToID("_DissolveAmount");
    private static readonly int Progress = Shader.PropertyToID("_Progress");

    private void OnEnable()
    {
        mpb = new MaterialPropertyBlock();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += Tick;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Tick;
#endif
    }

#if UNITY_EDITOR
    private void Tick()
    {
        if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif

    private void Update()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble;
        float p = Mathf.Repeat(t, Mathf.Max(0.5f, loopTime)) / Mathf.Max(0.5f, loopTime);

        Apply(withered, Reveal, Mathf.Clamp01(p * 3f), Dissolve, Mathf.Clamp01((p - 0.75f) * 4f));
        Apply(magma, Reveal, Mathf.Clamp01(p * 4f), Dissolve, Mathf.Clamp01((p - 0.6f) * 2.5f));
        Apply(shockwave, Progress, p, Dissolve, 0f);
        Apply(warn, Reveal, p, -1, 0f);
        if (fireball != null) fireball.transform.localPosition = new Vector3(fireball.transform.localPosition.x, 1.5f + Mathf.Sin(t * 2f) * 0.3f, fireball.transform.localPosition.z);
    }

    private void Apply(Renderer r, int idA, float a, int idB, float b)
    {
        if (r == null) return;
        r.GetPropertyBlock(mpb);
        mpb.SetFloat(idA, a);
        if (idB >= 0) mpb.SetFloat(idB, b);
        r.SetPropertyBlock(mpb);
    }
}
