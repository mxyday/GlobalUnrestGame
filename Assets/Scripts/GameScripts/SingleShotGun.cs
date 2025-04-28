using Unity.VisualScripting;
using UnityEngine;

public class SingleShotGun : Gun
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] GameObject hitEffectPrefab;
    [SerializeField] GameObject aimTarget;

    private AimTargetController aimTargetController;

    private void Start()
    {
        aimTargetController = aimTarget.GetComponent<AimTargetController>();
    }

    public override void Use()
    {
        Shoot();
    }

    void Shoot()
    {
        Debug.Log("Shoot fired");
        aimTargetController.ApplyRecoil(2f);

        Ray ray = new Ray(shootPoint.position, shootPoint.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Shoot hit");

            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, 2f);
            }

            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)ItemInfo).damage);
        }
    }
}