using Unity.Netcode;
using UnityEngine;

public class HeadTargetController : NetworkBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f;

    private Vector3 recoilOffset = Vector3.zero;

    // ������������ ����� �������
    private NetworkVariable<Vector3> syncedPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Owner);

    private void Update()
    {
        if (aimTransform == null) return;

        if (IsOwner)
        {
            // ��������� recoil
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

            // ������� �������
            Vector3 targetPosition = aimTransform.position + aimTransform.forward * distance + recoilOffset;

            // ������������ �������
            syncedPosition.Value = targetPosition;

            // ������������ ��������
            transform.position = targetPosition;
        }
        else
        {
            // ��� ����� �볺��� � ��������� ������� � �������� �����
            transform.position = syncedPosition.Value;
        }
    }

    public void ApplyRecoil(float recoilStrength)
    {
        if (!IsOwner) return; // ³����� ����� �� ���������� �볺��
        recoilOffset += Vector3.up * recoilStrength;
    }
}
