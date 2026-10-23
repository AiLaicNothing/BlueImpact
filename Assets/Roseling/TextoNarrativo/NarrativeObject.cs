using UnityEngine;

public class NarrativeObject : MonoBehaviour
{
    [Header("Contenido")]
    [TextArea(5, 20)]
    public string texto;

    [Header("imagen del texto")]
    public Sprite imagenDocumento;

    [Header("Texto de interacción")]
    public string textoInteraccion = "Leer";

    private bool playerCerca = false;

    private void Update()
    {
        if (playerCerca && Input.GetKeyDown(KeyCode.E))
        {
            NarrativeUI.instance.AbrirDocumento(texto, imagenDocumento);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCerca = true;

            NarrativeUI.instance.MostrarInteraccion( textoInteraccion);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCerca = false;

            NarrativeUI.instance.OcultarInteraccion();
            NarrativeUI.instance.CerrarDocumento();
        }
    }
}