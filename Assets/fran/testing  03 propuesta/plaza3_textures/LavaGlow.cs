using UnityEngine;

public class LavaGlow : MonoBehaviour
{
    public Vector2 scroll = new Vector2(0f, -0.08f); // cae hacia abajo
    public Light glowLight;
    public float baseIntensity = 3f, flicker = 0.8f, flickerSpeed = 2f;

    Renderer r; Vector2 off;
    static readonly int Base = Shader.PropertyToID("_BaseMap");
    static readonly int Main = Shader.PropertyToID("_MainTex");

    void Awake() { r = GetComponent<Renderer>(); }

    void Update()
    {
        if (r != null)
        {
            off += scroll * Time.deltaTime;
            var m = r.material;
            m.SetTextureOffset(m.HasProperty(Base) ? Base : Main, off);
        }
        if (glowLight != null)
            glowLight.intensity = baseIntensity +
                (Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) - 0.5f) * 2f * flicker;
    }
}