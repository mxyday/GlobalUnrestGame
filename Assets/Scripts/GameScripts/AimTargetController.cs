using UnityEngine;

public class AimTargetController : MonoBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f; // �������� ���������� ������

    private Vector3 recoilOffset = Vector3.zero;

    private void Update()
    {
        if (aimTransform == null) return;

        // ��������� �������� recoilOffset ����� �� ����
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        // ������� ������������ � ����������� recoil
        transform.position = aimTransform.position + aimTransform.forward * distance + recoilOffset;
    }

    /// <summary>
    /// ����������� ���� ������� ����������� ������
    /// </summary>
    /// <param name="recoilStrength">���� ������</param>
    public void ApplyRecoil(float recoilStrength)
    {
        // ³����� ��� ����� �� world space
        recoilOffset += Vector3.up * recoilStrength;
    }
}
