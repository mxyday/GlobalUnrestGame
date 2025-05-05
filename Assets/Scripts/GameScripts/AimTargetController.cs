using Unity.Netcode;
using UnityEngine;

public class AimTargetController : NetworkBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f;
    [SerializeField] private float maxRecoilOffset = 2f; // ����������� ������� �����

    private Vector3 recoilOffset = Vector3.zero;
    private Vector3 targetRecoilOffset = Vector3.zero;

    private NetworkVariable<Vector3> syncedPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Owner);

    private void Update()
    {
        if (aimTransform == null) return;

        if (IsOwner)
        {
            // ������������ recoil offset
            targetRecoilOffset = Vector3.Lerp(targetRecoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
            recoilOffset = Vector3.Lerp(recoilOffset, targetRecoilOffset, recoilRecoverySpeed * Time.deltaTime);

            // ������� ����� ������������
            Vector3 targetPosition = aimTransform.position + aimTransform.forward * distance + recoilOffset;

            // ������������
            syncedPosition.Value = targetPosition;
            transform.position = targetPosition;
        }
        else
        {
            transform.position = syncedPosition.Value;
        }
    }

    public void ApplyRecoil(float recoilStrength)
    {
        if (!IsOwner) return;

        // ���������� ������ �����/���
        float horizontal = Random.Range(-1f, 1f);
        float vertical = Random.Range(0.8f, 1.2f);

        Vector3 recoilDirection = new Vector3(horizontal, vertical, 0f).normalized;

        // ���������� � ������ ���������� ����������
        Vector3 recoilVector = aimTransform.TransformDirection(recoilDirection * recoilStrength);

        // ������ �� recoil offset (� ����������)
        targetRecoilOffset += recoilVector;
        targetRecoilOffset = Vector3.ClampMagnitude(targetRecoilOffset, maxRecoilOffset);
    }
}
