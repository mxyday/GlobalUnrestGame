using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Globalization;

public class SingleShotGun : Gun
{
    [SerializeField] private Transform weaponHolder;

    [Header("Shooting Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireDelay = 50f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private NetworkHitEffect networkHitEffect;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float maxRecoilOffset = 2f;
    [SerializeField] private float recoilRecoverySpeed = 5f;

    [Header("Weapon Kickback")]
    [SerializeField] private float kickbackAmount = 1f;
    [SerializeField] private float kickbackSpeed = 10f;
    [SerializeField] private float returnSpeed = 10f;

    private Vector3 originalPosition;

    private Vector3 recoilOffset = Vector3.zero;
    private Vector3 targetRecoilOffset = Vector3.zero;

    private Vector3 currentKickbackOffset = Vector3.zero;
    private Vector3 targetKickbackOffset = Vector3.zero;

    private float lastShotTime;

    private void Start()
    {

        if (networkHitEffect == null)
            networkHitEffect = GetComponent<NetworkHitEffect>();

        lastShotTime = -fireDelay;
        originalPosition = weaponHolder.localPosition;
    }

    public override void Use()
    {
        if (Time.time - lastShotTime >= fireDelay)
        {
            Shoot();
            lastShotTime = Time.time;
        }
    }

    private void Update()
    {
        // Згладжування recoil
        targetRecoilOffset = Vector3.Lerp(targetRecoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        recoilOffset = Vector3.Lerp(recoilOffset, targetRecoilOffset, recoilRecoverySpeed * Time.deltaTime);

        // Згладжування kickback
        targetKickbackOffset = Vector3.Lerp(targetKickbackOffset, Vector3.zero, returnSpeed * Time.deltaTime);
        currentKickbackOffset = Vector3.Lerp(currentKickbackOffset, targetKickbackOffset, kickbackSpeed * Time.deltaTime);

        // Обчислення зміщення
        Vector3 totalOffset = weaponHolder.parent.InverseTransformDirection(recoilOffset) + new Vector3(0, 0, currentKickbackOffset.z);
        weaponHolder.localPosition = originalPosition + totalOffset;
    }

    private void Shoot()
    {
        Debug.Log("Shoot fired");
        ApplyRecoil(recoilForce);
        ApplyKickback();

        Ray ray = new Ray(shootPoint.position, shootPoint.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Shoot hit");

            if (networkHitEffect != null)
                networkHitEffect.PlayEffect(hit.point, hit.normal);
            else if (hitEffectPrefab != null)
                Destroy(Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal)), 2f);

            if (hit.collider.TryGetComponent<NetworkObject>(out var targetNetworkObject))
                ApplyDamageServerRpc(targetNetworkObject, ((GunInfo)ItemInfo).damage);
        }
    }

    private void ApplyRecoil(float recoilStrength)
    {
        float horizontal = Random.Range(-1f, 1f);
        float vertical = Random.Range(0.8f, 1.2f);
        Vector3 recoilDirection = new Vector3(horizontal, vertical, 0f).normalized;
        Vector3 recoilVector = transform.TransformDirection(recoilDirection * recoilStrength);

        targetRecoilOffset += recoilVector;
        targetRecoilOffset = Vector3.ClampMagnitude(targetRecoilOffset, maxRecoilOffset);
    }

    private void ApplyKickback()
    {
        targetKickbackOffset = new Vector3(0f, 0f, -kickbackAmount);
    }

    [ServerRpc]
    private void ApplyDamageServerRpc(NetworkObjectReference targetRef, float damage)
    {
        if (targetRef.TryGet(out NetworkObject target))
            target.GetComponent<IDamageable>()?.TakeDamage(damage);
    }
}