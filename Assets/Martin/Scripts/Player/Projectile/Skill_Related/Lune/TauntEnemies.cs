using UnityEngine;
using System.Collections;
using System;
public class TauntEnemies : PlayerControl
{
    public static event Action<PlayerControl> OnTauntSpawned;
    [Header("Radio del Taunt")]
    [SerializeField]float radius;
    PlayerControl p;
    void Start()
    {
        p = GameObject.FindWithTag("Player").GetComponent<PlayerControl>();
        StartCoroutine(SearchEnemies());
    }
    IEnumerator SearchEnemies()
    {
        while (true)
        {
            yield return null;

            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (var col in colliders)
            {
                EnemyBase e = col.GetComponent<EnemyBase>();
                if (e != null)
                {
                    OnTauntSpawned?.Invoke(this);
                }
            }
            yield return new WaitForSeconds(1);
        }
    }
    private void Update()
    {
        return;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    private void OnDestroy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var col in colliders)
        {
            EnemyBase e = col.GetComponent<EnemyBase>();
            if (e != null)
            {
                OnTauntSpawned?.Invoke(p);
            }
        }
    }
}
