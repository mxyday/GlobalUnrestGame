using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SingleShotGun : Gun
{
    [Header("Shooting Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireDelay = 50f; // Затримка між пострілами (в секундах)
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject aimTarget;
    [SerializeField] private NetworkHitEffect networkHitEffect;

    private AimTargetController aimTargetController;
    private float lastShotTime; // Час останнього пострілу

    [Header("Weapon Kickback")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float kickbackAmount = 1f; // Сила відкату
    [SerializeField] private float kickbackSpeed = 10f;    // Швидкість "відскоку"
    [SerializeField] private float returnSpeed = 10f;       // Швидкість повернення
    private Vector3 originalPosition;                      // Початкова позиція зброї

    private void Start()
    {
        aimTargetController = aimTarget.GetComponent<AimTargetController>();

        if (networkHitEffect == null)
        {
            networkHitEffect = GetComponent<NetworkHitEffect>();
        }

        lastShotTime = -fireDelay; // Дозволяємо перший постріл відразу

        originalPosition = transform.localPosition; // Запам'ятовуємо початкову позицію
    }

    public override void Use()
    {
        // Перевіряємо, чи минуло достатньо часу з останнього пострілу
        if (Time.time - lastShotTime >= fireDelay)
        {
            Shoot();
            lastShotTime = Time.time; // Оновлюємо час останнього пострілу
        }
    }

    private void Shoot()
    {
        Debug.Log("Shoot fired");
        aimTargetController.ApplyRecoil(recoilForce);

        StartCoroutine(ApplyKickback());

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

            if (hit.collider.TryGetComponent<NetworkObject>(out var targetNetworkObject))
            {
                ApplyDamageServerRpc(targetNetworkObject, ((GunInfo)ItemInfo).damage);
            }
        }
    }

    private IEnumerator ApplyKickback()
    {
        Vector3 targetPosition = originalPosition + new Vector3(0, 0, -kickbackAmount);

        // Швидко "відтягуємо" зброю назад
        while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                kickbackSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Плавно повертаємо на початкову позицію
        while (Vector3.Distance(transform.localPosition, originalPosition) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalPosition,
                returnSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.localPosition = originalPosition; // Фінальне вирівнювання
    }

    [ServerRpc]
    private void ApplyDamageServerRpc(NetworkObjectReference targetRef, float damage)
    {
        if (targetRef.TryGet(out NetworkObject target))
        {
            target.GetComponent<IDamageable>()?.TakeDamage(damage);
        }
    }
}