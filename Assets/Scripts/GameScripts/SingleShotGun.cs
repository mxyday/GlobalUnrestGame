using UnityEngine;
using Unity.Netcode;

public class SingleShotGun : Gun  // Змініть наслідування
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject aimTarget;
    [SerializeField] private NetworkHitEffect networkHitEffect;

    private AimTargetController aimTargetController;

    private void Start()
    {
        aimTargetController = aimTarget.GetComponent<AimTargetController>();

        if (networkHitEffect == null)
        {
            networkHitEffect = GetComponent<NetworkHitEffect>();
        }
    }

    public override void Use()
    {
        Shoot();
    }

    private void Shoot()
    {
        Debug.Log("Shoot fired");
        aimTargetController.ApplyRecoil(2f);

        Ray ray = new Ray(shootPoint.position, shootPoint.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Shoot hit");

            // Обробка ефекту
            if (networkHitEffect != null)
            {
                networkHitEffect.PlayEffect(hit.point, hit.normal);
            }
            else if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(hitEffect, 2f);
            }

            // Обробка пошкоджень тільки на сервері
            if (NetworkManager.Singleton.IsServer)
            {
                hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)ItemInfo).damage);
            }
        }
    }
}