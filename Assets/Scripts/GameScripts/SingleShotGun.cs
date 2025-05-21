using UnityEngine;
using Unity.Netcode;

public class SingleShotGun : Gun
{
    [SerializeField] private Transform weaponHolder;

    [Header("Shooting Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireDelay = 50f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private NetworkHitEffect networkHitEffect;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilRotationAmount = 3f;
    [SerializeField] private float recoilRecoverySpeed = 10f;

    [Header("Recoil Pivot Settings")]
    [SerializeField] private Transform recoilPivot;
    [SerializeField] private Vector3 recoilPivotOffset = new Vector3(0, -0.2f, 0);

    [Header("Weapon Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float maxJumpOffset = 2f;
    [SerializeField] private float jumpRecoverySpeed = 5f;

    [Header("Weapon Kickback")]
    [SerializeField] private float kickbackAmount = 1f;
    [SerializeField] private float kickbackSpeed = 10f;
    [SerializeField] private float returnSpeed = 10f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalPivotPosition;

    private Vector3 recoilOffset = Vector3.zero;
    private Vector3 targetRecoilOffset = Vector3.zero;

    private Vector3 jumpOffset = Vector3.zero;
    private Vector3 targetJumpOffset = Vector3.zero;

    private Vector3 currentKickbackOffset = Vector3.zero;
    private Vector3 targetKickbackOffset = Vector3.zero;

    private float lastShotTime;

    private void Start()
    {
        if (networkHitEffect == null)
            networkHitEffect = GetComponent<NetworkHitEffect>();

        lastShotTime = -fireDelay;
        originalPosition = weaponHolder.localPosition;
        originalRotation = transform.localRotation;
        originalPivotPosition = recoilPivot != null ? recoilPivot.localPosition : Vector3.zero;
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
        // Recoil position recovery
        targetRecoilOffset = Vector3.Lerp(targetRecoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        recoilOffset = Vector3.Lerp(recoilOffset, targetRecoilOffset, recoilRecoverySpeed * Time.deltaTime);

        // Rotation recovery
        transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, recoilRecoverySpeed * Time.deltaTime);

        // Pivot position recovery
        if (recoilPivot != null)
        {
            recoilPivot.localPosition = Vector3.Lerp(recoilPivot.localPosition, originalPivotPosition, recoilRecoverySpeed * Time.deltaTime);
        }

        // Jump recovery
        targetJumpOffset = Vector3.Lerp(targetJumpOffset, Vector3.zero, jumpRecoverySpeed * Time.deltaTime);
        jumpOffset = Vector3.Lerp(jumpOffset, targetJumpOffset, jumpRecoverySpeed * Time.deltaTime);

        // Kickback recovery
        targetKickbackOffset = Vector3.Lerp(targetKickbackOffset, Vector3.zero, returnSpeed * Time.deltaTime);
        currentKickbackOffset = Vector3.Lerp(currentKickbackOffset, targetKickbackOffset, kickbackSpeed * Time.deltaTime);

        // Apply all offsets (FIXED: тепер враховуємо jumpOffset)
        Vector3 totalOffset = weaponHolder.parent.InverseTransformDirection(recoilOffset + jumpOffset) + new Vector3(0, 0, currentKickbackOffset.z);
        weaponHolder.localPosition = originalPosition + totalOffset;
    }

    private void Shoot()
    {
        Debug.Log("Shoot fired");

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

        ApplyRecoil();
        ApplyJump(jumpForce);
        ApplyKickback();
    }

    private void ApplyRecoil()
    {
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomY = Random.Range(0.8f, 1.2f);

        Vector3 recoilRotation = new Vector3(
            -recoilRotationAmount * randomY,
            recoilRotationAmount * randomX,
            0
        );

        // Apply rotation
        transform.localRotation *= Quaternion.Euler(recoilRotation);

        // Apply pivot offset if needed
        if (recoilPivot != null)
        {
            recoilPivot.localPosition += new Vector3(0, 0, -0.1f); // Small backward push
        }
    }

    private void ApplyJump(float jumpStrength)
    {
        float horizontal = Random.Range(-1f, 1f);
        float vertical = Random.Range(0.8f, 1.2f);
        Vector3 jumpDirection = new Vector3(horizontal, vertical, 0f).normalized;
        Vector3 jumpVector = transform.TransformDirection(jumpDirection * jumpStrength);

        targetJumpOffset += jumpVector;
        targetJumpOffset = Vector3.ClampMagnitude(targetJumpOffset, maxJumpOffset);
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