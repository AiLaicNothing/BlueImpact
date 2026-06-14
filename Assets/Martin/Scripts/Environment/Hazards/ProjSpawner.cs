using UnityEngine;

public class ProjSpawner : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float damage;
    [SerializeField] private float timeBtwShoot = 2f;
    private float timer;

    private bool playerIsClose;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeBtwShoot)
        {
            Shoot();
            timer = 0f;
        }
    }

    private void Shoot()
    {
        if (firePoint == null) return;
        if (bulletPrefab == null) return;

        GameObject proj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        var Projectile = proj.GetComponent<E_Projectile>();

        Projectile.InitProj(damage, transform.forward);
    }
}
