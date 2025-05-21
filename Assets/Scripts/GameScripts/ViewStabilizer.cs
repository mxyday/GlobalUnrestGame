using UnityEngine;

public class ViewStabilizer : MonoBehaviour
{
    [SerializeField] private Transform headBone;         // ʳ���� ������
    [SerializeField] private Transform cameraTransform;  // ������
    [SerializeField] private float smoothSpeed = 10f;    // ������������ �������
    [SerializeField] private float yawSmoothSpeed = 5f;  // ������������ ��������������� ��������
    [SerializeField] private float maxOffsetDistance = 0.2f; // ����. ��������� ���������
    [SerializeField] private bool stabilizeYaw = true;   // �� ����������� yaw (��������� �� Y)

    private Vector3 initialLocalOffset;
    private float currentYaw;

    private void Start()
    {
        if (headBone == null || cameraTransform == null) return;

        initialLocalOffset = Quaternion.Inverse(headBone.rotation) * (cameraTransform.position - headBone.position);
        currentYaw = headBone.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (headBone == null || cameraTransform == null) return;

        // --- ������� ---
        Vector3 desiredPosition = headBone.position + headBone.rotation * initialLocalOffset;
        Vector3 smoothedPosition = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        Vector3 offset = smoothedPosition - desiredPosition;
        if (offset.magnitude > maxOffsetDistance)
        {
            smoothedPosition = desiredPosition + offset.normalized * maxOffsetDistance;
        }

        cameraTransform.position = smoothedPosition;

        // --- ��������� ---
        if (stabilizeYaw)
        {
            float desiredYaw = headBone.eulerAngles.y;
            currentYaw = Mathf.LerpAngle(currentYaw, desiredYaw, Time.deltaTime * yawSmoothSpeed);

            float yawOffset = Mathf.DeltaAngle(headBone.eulerAngles.y, currentYaw);
            float maxYawOffset = 10f; // ����������� ��������� � ��������

            yawOffset = Mathf.Clamp(yawOffset, -maxYawOffset, maxYawOffset);
            currentYaw = headBone.eulerAngles.y + yawOffset;

            Vector3 headEuler = headBone.eulerAngles;
            headEuler.y = currentYaw;

            cameraTransform.rotation = Quaternion.Euler(headEuler);
        }
        else
        {
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, headBone.rotation, Time.deltaTime * smoothSpeed);
        }
    }
}
