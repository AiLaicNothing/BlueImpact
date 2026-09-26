using System.Collections;
using UnityEngine;

public class LuneBlinkingEyes : MonoBehaviour
{
    public Renderer cuerpo;

    [Header("Materiales del parpadeo")]
    // [0] Ojos abiertos
    // [1] Ojos entreabiertos
    // [2] Ojos cerrados
    public Material[] materialesParpadeo;

    [Header("Configuración")]
    public float tiempoMinimo = 2.5f;
    public float tiempoMaximo = 6.0f;
    public float velocidadFotograma = 0.05f;

    private void Start()
    {
        StartCoroutine(BucleParpadeo());
    }

    IEnumerator BucleParpadeo()
    {
        while (true)
        {
            float tiempoEspera = Random.Range(tiempoMinimo, tiempoMaximo);
            yield return new WaitForSeconds(tiempoEspera);

            cuerpo.material = materialesParpadeo[1];
            yield return new WaitForSeconds(velocidadFotograma);

            cuerpo.material = materialesParpadeo[2];
            yield return new WaitForSeconds(velocidadFotograma);

            cuerpo.material = materialesParpadeo[1];
            yield return new WaitForSeconds(velocidadFotograma);

            cuerpo.material = materialesParpadeo[0];
        }
    }
}