using UnityEngine;
using System;
using System.Collections;
public class DestructibleObjects : MonoBehaviour, IDamageable
{
    private Rigidbody rb;
    AudioSource audioSource;
    [SerializeField] GameObject brokenPrefab;
    [SerializeField] AudioClip destructionClip;
    [SerializeField] float explosiveForce = 250;
    [SerializeField] float explosiveRadius = 2f;
    [SerializeField] float pieceFadeSpeed = 0.25f;
    [SerializeField] float pieceDestroyDelay = 2f;
    [SerializeField] float pieceSleepCheckDelay = 0.1f;
    float life = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //audioSource = GetComponent<AudioSource>();
    }
    public void TakeDamage(in DamageInfo info)
    {
        life -= info.damage;
        if(life <= 0)
        {
            Explode();
        }
    }
    public void Explode()
    {
        if(rb != null)
        {
            Destroy(rb);
        }
        if(TryGetComponent<Collider>(out Collider coliider))
        {
            coliider.enabled = false;
        }
        if(TryGetComponent<Renderer>(out Renderer renderer))
        {
            renderer.enabled = false;
        }
        if(destructionClip != null)
        {
            audioSource.PlayOneShot(destructionClip);
        }
        GameObject brokenInstance = Instantiate(brokenPrefab, transform.position, transform.rotation);

        Rigidbody[] rbs = brokenInstance.GetComponentsInChildren<Rigidbody>();
        foreach(Rigidbody body in rbs)
        {
            if(rb != null)
            {
                body.angularVelocity = rb.angularVelocity;
            }

            body.AddExplosionForce(explosiveForce, transform.position, explosiveRadius);
        }

        StartCoroutine(FadeOutRigidbodies(rbs, brokenInstance));
    }
    IEnumerator FadeOutRigidbodies(Rigidbody[] rbs, GameObject brokenInstance)
    {
        WaitForSeconds wait = new WaitForSeconds(pieceSleepCheckDelay);
        int activeRbs = rbs.Length;

        while(activeRbs > 0)
        {
            yield return wait;
            foreach(Rigidbody rb in rbs)
            {
                if (rb.IsSleeping())
                {
                    activeRbs--;
                }
            }
        }
        yield return new WaitForSeconds(pieceDestroyDelay);

        float time = 0;
        Renderer[] renderers = Array.ConvertAll(rbs, GetRendererFromRigidbody);
        foreach(Rigidbody body in rbs)
        {
            Destroy(body.GetComponent<Collider>());
            Destroy(body);
        }
        while (time < 1)
        {
            float step = Time.deltaTime * pieceFadeSpeed;
            foreach (Renderer r in renderers)
            {
                r.transform.Translate(Vector3.down * (step / r.bounds.size.y), Space.World);
            }
            time += step;
            yield return null;
        }
        foreach(Renderer r in renderers)
        {
            Destroy(r.gameObject);
        }
        Destroy(brokenInstance);
        Destroy(gameObject);
    }

    Renderer GetRendererFromRigidbody(Rigidbody rb)
    {
        return rb.GetComponent<Renderer>();
    }
}
