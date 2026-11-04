using UnityEngine;

// El cristal del Blue Impact. Solo se puede tocar cuando BlueImpactEvent lo habilita,
// es decir despues de que se rompen las cadenas y el cristal termina de descender.
//
// IMPORTANTE: PlayerInteraction busca el interactuable con hit.GetComponent<MonoBehaviour>(),
// que devuelve el PRIMER MonoBehaviour del objeto. Este script debe ser el unico
// MonoBehaviour del GameObject que lleva el Collider, o no se detecta.
[RequireComponent(typeof(Collider))]
public class BlueImpact : MonoBehaviour, IInteractable
{
    [Header("Texto del prompt")]
    [SerializeField] private string interactionText = "Examinar el Blue Impact";

    [Header("Estado")]
    // Arranca apagado: durante la pelea y la cinematica no se puede tocar
    [SerializeField] private bool interactable;

    [Header("Evento")]
    // Si lo dejas vacio, BlueImpactEvent se registra solo con Bind()
    [SerializeField] private BlueImpactEvent eventOwner;

    [Header("Efectos (hijos de este objeto)")]
    [SerializeField] private GameObject idleVfx;    // mientras cuelga de las cadenas
    [SerializeField] private GameObject readyVfx;   // cuando ya se puede usar
    [SerializeField] private Light glowLight;
    [SerializeField] private float readyLightIntensity = 4f;
    [SerializeField] private float lightLerpSpeed = 2f;

    private bool used;
    private float targetIntensity;
    private float baseIntensity;

    private void Awake()
    {
        if (glowLight != null)
        {
            baseIntensity = glowLight.intensity;
            targetIntensity = baseIntensity;
        }

        ApplyVfx();
    }

    private void Update()
    {
        if (glowLight == null) return;

        glowLight.intensity = Mathf.Lerp(glowLight.intensity, targetIntensity, lightLerpSpeed * Time.deltaTime);
    }

    // BlueImpactEvent se registra aqui para recibir la interaccion
    public void Bind(BlueImpactEvent owner)
    {
        eventOwner = owner;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        targetIntensity = value ? readyLightIntensity : baseIntensity;

        ApplyVfx();

        // Si deja de ser interactuable, el prompt no debe quedarse en pantalla
        if (!value) InteractionUI.Instance?.Clear();
    }

    private void ApplyVfx()
    {
        if (idleVfx != null) idleVfx.SetActive(!interactable);
        if (readyVfx != null) readyVfx.SetActive(interactable);
    }

    // ---------- IInteractable ----------

    public void Interact()
    {
        if (!interactable || used) return;

        if (eventOwner == null)
        {
            Debug.LogWarning("[BlueImpact] Sin BlueImpactEvent asignado: la interaccion no hace nada", this);
            return;
        }

        used = true;
        interactable = false;

        InteractionUI.Instance?.Clear();

        eventOwner.OpenChoice();
    }

    // Devolver cadena vacia hace que InteractionUI no muestre nada.
    // Asi el prompt solo aparece cuando el cristal ya esta disponible.
    public string GetInteractionText()
    {
        if (!interactable || used) return "";

        return interactionText;
    }
}
