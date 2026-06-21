using UnityEngine;

public class CuboDestructible : MonoBehaviour, IDamageable
{
    public float vidaMaxima = 100f; 
    public float vidaActual;

    public GameObject cuboDanio1;
    public GameObject cuboRestos;

    void Start()
    {
        vidaActual = vidaMaxima;
        ActualizarEstadoVisual();
    }

    void ActualizarEstadoVisual()
    {
        float porcentajeVida = (vidaActual / vidaMaxima) * 100f;

        if (porcentajeVida > 30f)
        {
            cuboDanio1.SetActive(true);
            cuboRestos.SetActive(false);
        }
        else if (porcentajeVida > 0f)
        {
            cuboDanio1.SetActive(false);
            cuboRestos.SetActive(true);
        }
        else
        {
            cuboDanio1.SetActive(false);
            cuboRestos.SetActive(false);
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            ActualizarEstadoVisual();
    }

    void Destruir()
    {
        Destroy(gameObject, 1f); 
    }

    public void TakeDamage(in DamageInfo info)
    {
        vidaActual -= info.damage;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);

        Debug.Log($"🔴 Cubo recibió {info.damage} daño. Vida actual: {vidaActual}/{vidaMaxima}");

        ActualizarEstadoVisual();

        if (vidaActual <= 0)
        {
            Debug.Log("💥 Cubo destruido");
            Destruir();
        }
    }
}