using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NarrativeUI : MonoBehaviour
{
    public static NarrativeUI instance;

    [Header("interacción")]
    public GameObject panelInteraccion;
    public TMP_Text textoInteraccion;

    [Header("objeto")]
    public GameObject panelDocumento;
    public TMP_Text textoDocumento;
    public Image imagenDocumento;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        panelInteraccion.SetActive(false);
        panelDocumento.SetActive(false);
    }

    public void MostrarInteraccion(string texto)
    {
        textoInteraccion.text = "E  " + texto;
        panelInteraccion.SetActive(true);
    }

    public void OcultarInteraccion()
    {
        panelInteraccion.SetActive(false);
    }

    public void AbrirDocumento(string texto, Sprite imagen)
    {
        textoDocumento.text = texto;

        if (imagen != null)
        {
            imagenDocumento.sprite = imagen;
        }

        panelDocumento.SetActive(true);
        panelInteraccion.SetActive(false);
    }

    public void CerrarDocumento()
    {
        panelDocumento.SetActive(false);
    }
}