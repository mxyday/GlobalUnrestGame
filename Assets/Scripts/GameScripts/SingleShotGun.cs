using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SingleShotGun : Gun
{
    [Header("Shooting Settings")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float fireDelay = 50f; // �������� �� ��������� (� ��������)
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject aimTarget;
    [SerializeField] private NetworkHitEffect networkHitEffect;

    private AimTargetController aimTargetController;
    private float lastShotTime; // ��� ���������� �������

    [Header("Weapon Kickback")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float kickbackAmount = 1f; // ���� ������
    [SerializeField] private float kickbackSpeed = 10f;    // �������� "�������"
    [SerializeField] private float returnSpeed = 10f;       // �������� ����������
    private Vector3 originalPosition;                      // ��������� ������� ����

    private void Start()
    {
        aimTargetController = aimTarget.GetComponent<AimTargetController>();

        if (networkHitEffect == null)
        {
            networkHitEffect = GetComponent<NetworkHitEffect>();
        }

        lastShotTime = -fireDelay; // ���������� ������ ������ ������

        originalPosition = transform.localPosition; // �����'������� ��������� �������
    }

    public override void Use()
    {
        // ����������, �� ������ ��������� ���� � ���������� �������
        if (Time.time - lastShotTime >= fireDelay)
        {
            Shoot();
            lastShotTime = Time.time; // ��������� ��� ���������� �������
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

            // ������� ������
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

        // ������ "��������" ����� �����
        while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                kickbackSpeed * Time.deltaTime
            );
            yield return null;
        }

        // ������ ��������� �� ��������� �������
        while (Vector3.Distance(transform.localPosition, originalPosition) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalPosition,
                returnSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.localPosition = originalPosition; // Գ������ �����������
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