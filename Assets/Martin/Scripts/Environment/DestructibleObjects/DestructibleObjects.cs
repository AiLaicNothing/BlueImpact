using UnityEngine;
using System;
using System.Collections;
public class DestructibleObjects : MonoBehaviour, IDamageable
{
    private Rigidbody rb;
    AudioSource audioSource;
    Material thisMaterial;
    private Collider col;
    private Renderer rend;
    [SerializeField] GameObject brokenPrefab;
    [SerializeField] AudioClip destructionClip;
    [Header("Explosion Settings")]
    [SerializeField] float explosiveForce = 250;
    [SerializeField] float explosiveRadius = 2f;
    [SerializeField] float pieceFadeSpeed = 0.25f;
    [SerializeField] float pieceDestroyDelay = 2f;
    [SerializeField] float pieceSleepCheckDelay = 0.1f;

    [Header("Options")]
    [SerializeField] bool thisMaterialIsToon;
    [SerializeField] bool forPuzzles;

    private Vector3 startPos;
    private Quaternion startRot;
    private bool wasKinematic;
    private float life = 1f;
    private bool isBroken = false;
    private void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        rb = GetComponent<Rigidbody>(); 
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
        thisMaterial = GetComponent<Renderer>().sharedMaterial;
        //audioSource = GetComponent<AudioSource>();
    }
    public void TakeDamage(in DamageInfo info)
    {
        if (forPuzzles || isBroken) return;

        life -= info.damage;
        if(life <= 0)
        {
            Explode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!forPuzzles || isBroken) return;

        if (collision.gameObject.CompareTag("DestroyBox"))
        {
            Explode();
        }

    }
    public void Explode()
    {
        isBroken = true;
        //Vector3 currentAngularVel = rb != null ? rb.angularVelocity : Vector3.zero;
        if (rb != null)
        {
            if (forPuzzles)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else
            {
                Destroy(rb);
            }
        }
        Vector3 spawnPoition = transform.position;
        Vector3 explosionCenter = transform.position;
        if(rend != null)
        {
            explosionCenter = rend.bounds.center;

            spawnPoition = rend.bounds.center - (transform.up * rend.bounds.extents.y);
        }
        if (col != null) col.enabled = false;
        if (rend != null) rend.enabled = false;
        if (destructionClip != null)
        {
            audioSource.PlayOneShot(destructionClip);
        }

        GameObject brokenInstance = Instantiate(brokenPrefab, spawnPoition, transform.rotation);
        brokenInstance.transform.localScale = gameObject.transform.localScale;
        if (!thisMaterialIsToon && thisMaterial != null)
        {
            Renderer[] mats = brokenInstance.transform.GetComponentsInChildren<Renderer>();
            foreach(Renderer r in mats)
            {
                r.sharedMaterial = thisMaterial;
            }
            /**for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = thisMaterial;
            }*/
        }

        Rigidbody[] rbs = brokenInstance.GetComponentsInChildren<Rigidbody>();
        foreach(Rigidbody body in rbs)
        {
            
           // body.angularVelocity = currentAngularVel;            
            body.AddExplosionForce(explosiveForce, explosionCenter, explosiveRadius);
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

        if (forPuzzles)
        {
            ResetPuzzle();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void ResetPuzzle()
    {
        transform.position = startPos;
        transform.rotation = startRot;

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            if (!wasKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }


        if (col != null) col.enabled = true;
        if (rend != null) rend.enabled = true;

        isBroken = false;
    }
    Renderer GetRendererFromRigidbody(Rigidbody rb)
    {
        return rb.GetComponent<Renderer>();
    }
}
