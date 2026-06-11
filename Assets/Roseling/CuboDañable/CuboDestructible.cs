using UnityEngine;

public class CuboDestructible : MonoBehaviour
{
    public float vidaMaxima = 300f;
    public float vidaActual;

    public GameObject cuboEntero;
    public GameObject cuboDanio1;
    public GameObject cuboDanio2;
    public GameObject cuboRestos;

    void Start()
    {
        vidaActual = vidaMaxima;
        ActualizarEstadoVisual();
    }

    public void RecibirDanio(float cantidad)
    {
        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);

        ActualizarEstadoVisual();

        if (vidaActual <= 0)
        {
            Destruir();
        }
    }

    void ActualizarEstadoVisual()
    {
        // apagar todo
        cuboEntero.SetActive(false);
        cuboDanio1.SetActive(false);
        cuboDanio2.SetActive(false);
        cuboRestos.SetActive(false);

        if (vidaActual > 200)
        {
            cuboEntero.SetActive(true);
        }
        else if (vidaActual > 100)
        {
            cuboDanio1.SetActive(true);
        }
        else if (vidaActual > 0)
        {
            cuboDanio2.SetActive(true);
        }
        else
        {
            cuboRestos.SetActive(true);
        }
    }
    //solo pa pobrar luego lo comento
    void OnValidate()
    {
        ActualizarEstadoVisual();
    }

    void Destruir()
    {
        Destroy(gameObject, 2f);
    }
}