using UnityEngine;
using UnityEngine.UI;
public class BeamCollision : MonoBehaviour
{
    [Header("Beam Configurations")]
    [SerializeField] float maxDistance = 50f;
    [SerializeField] LayerMask collisionLayers;
    [SerializeField] Image startBeam;
    [SerializeField] GameObject vfxBeam;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] float duration;
    [Header("Impact Effect")]
    [SerializeField] GameObject impactEffect;

    
    private ParticleSystem[] impactParticles;
    private float currentOffset = 0f;
    private float timer = 0f;
    private float timer2 = 0f;

    void Start()
    {
        lineRenderer.useWorldSpace = true;
        timer = duration;
        if (impactEffect != null)
        {
            impactParticles = impactEffect.GetComponentsInChildren<ParticleSystem>();

            impactEffect.SetActive(false);
        }
        vfxBeam.SetActive(false);
        startBeam.fillAmount = 0;
    }

    void Update()
    {
        UpdateBeam();
    }
    void AciveBeam()
    {
        vfxBeam.SetActive(true);
    }
    void UpdateBeam()
    {
        if (!vfxBeam.activeSelf) 
        {
            timer2 += Time.deltaTime;
            startBeam.fillClockwise = false;
            startBeam.fillAmount = timer2 / 0.6f;

            Invoke(nameof(AciveBeam), 0.6f); 
            return; 
        }
        timer -= Time.deltaTime;
        startBeam.fillClockwise = true;
        startBeam.fillAmount = timer / duration;
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        Vector3 targetPosition = origin + (direction * maxDistance);

        lineRenderer.SetPosition(0, origin);

        lineRenderer.SetPosition(1, targetPosition);
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, collisionLayers))
        {
            targetPosition = hit.point;
            lineRenderer.SetPosition(1, targetPosition);
            if (impactEffect != null)
            {
                if (!impactEffect.activeSelf) impactEffect.SetActive(true);

                impactEffect.transform.position = hit.point;
                impactEffect.transform.rotation = Quaternion.LookRotation(hit.normal);

                foreach (var ps in impactParticles)
                {
                    if (ps != null && !ps.isPlaying) ps.Play();
                }
            }
        }
        else
        {
            targetPosition = origin + (direction * maxDistance);
            lineRenderer.SetPosition(1, targetPosition);
            if (impactEffect != null && impactEffect.activeSelf)
            {
                foreach (var ps in impactParticles)
                {
                    if (ps != null) ps.Stop();
                }
                impactEffect.SetActive(false);
            }
        }


        float currentDistance = Vector3.Distance(origin, targetPosition);
        foreach (Transform child in vfxBeam.transform)
        {
            if (child.name.Contains("CylinderMagicB") || child.name.Contains("ConeMagicB") || child.name.Contains("CylinderMagicB (1)"))
            {
                Vector3 newScale = child.localScale;
                newScale.z = currentDistance / 2;
                child.localScale = newScale;

            }
        }
    }
    private void OnDestroy()
    {
        Destroy(impactEffect);
    }
    private void OnDisable()
    {
        impactEffect.SetActive(false);
    }
}

