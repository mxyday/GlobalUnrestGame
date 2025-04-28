using Unity.VisualScripting;
using UnityEngine;

public class SingleShotGun : Gun
{
    [SerializeField] Camera cam;
    [SerializeField] GameObject hitEffectPrefab;

    public override void Use()
    {
        Shoot();
    }

    void Shoot()
    {
        Debug.Log("Shoot fired");
        Ray ray = cam.ViewportPointToRay(new Vector3 (0.5f, 0.5f));
        ray.origin = cam.transform.position;
        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Shoot hit");

            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, 2f); // через 2 секунди ефект сам видаляється
            }

            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)ItemInfo).damage);
        }
    }
}

