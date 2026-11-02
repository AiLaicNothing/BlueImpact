using UnityEngine;

/// <summary>
/// Anima los quads de BlueImpact/VFX al instanciarse (sin Animator). Se acuesta en el suelo y
/// mueve _Reveal / _Progress / _DissolveAmount con un MaterialPropertyBlock.
/// El que lo instancia decide cuándo destruirlo (Destroy(obj, tiempo)).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class BlueVFXPlayer : MonoBehaviour
{
    public enum Kind { FieldGrow, Shockwave, Magma, Warning }

    [SerializeField] private Kind kind = Kind.Warning;
    [SerializeField] private float duration = 1f;
    [SerializeField] private bool lieFlat = true;

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private float t;

    private static readonly int Reveal = Shader.PropertyToID("_Reveal");
    private static readonly int Dissolve = Shader.PropertyToID("_DissolveAmount");
    private static readonly int Progress = Shader.PropertyToID("_Progress");

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        if (lieFlat) transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Apply();
    }

    private void Update()
    {
        t += Time.deltaTime;
        Apply();
    }

    private void Apply()
    {
        float d = Mathf.Max(0.05f, duration);
        rend.GetPropertyBlock(mpb);

        switch (kind)
        {
            case Kind.FieldGrow:
                mpb.SetFloat(Reveal, Mathf.Clamp01(t / 0.4f));
                mpb.SetFloat(Dissolve, Mathf.Clamp01((t - (d - 0.6f)) / 0.6f));
                break;
            case Kind.Shockwave:
                mpb.SetFloat(Progress, Mathf.Clamp01(t / d));
                break;
            case Kind.Magma:
                mpb.SetFloat(Reveal, Mathf.Clamp01(t / 0.25f));
                mpb.SetFloat(Dissolve, Mathf.Clamp01((t - d * 0.5f) / (d * 0.5f)));
                break;
            default: // Warning
                mpb.SetFloat(Reveal, Mathf.Clamp01(t / d));
                break;
        }
        rend.SetPropertyBlock(mpb);
    }
}
