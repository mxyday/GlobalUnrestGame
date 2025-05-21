using UnityEngine;

public class ItemTransformStabilizer : MonoBehaviour
{
    [SerializeField] private Transform headBone;             // Наприклад, кістка голови
    [SerializeField] private Transform stabilizedTransform;    // Що стабілізуємо (зброя, приціл)
    [SerializeField] private float smoothSpeed = 10f;          // Згладжування
    [SerializeField] private float maxOffsetDistance = 0.2f;   // Макс. відстань
    [SerializeField] private float yawSmoothSpeed = 5f;        // Згладжування горизонтального повороту
    [SerializeField] private bool stabilizeYaw = true;         // Увімкнути/вимкнути стабілізацію обертання

    private Vector3 initialLocalOffset;
    private float currentYaw;

    private void Start()
    {
        if (headBone == null || stabilizedTransform == null) return;

        initialLocalOffset = Quaternion.Inverse(headBone.rotation) * (stabilizedTransform.position - headBone.position);
        currentYaw = headBone.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (headBone == null || stabilizedTransform == null) return;

        // --- Позиція ---
        Vector3 desiredPosition = headBone.position + headBone.rotation * initialLocalOffset;
        Vector3 smoothedPosition = Vector3.Lerp(stabilizedTransform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        Vector3 offset = smoothedPosition - desiredPosition;
        if (offset.magnitude > maxOffsetDistance)
        {
            smoothedPosition = desiredPosition + offset.normalized * maxOffsetDistance;
        }

        stabilizedTransform.position = smoothedPosition;

        // --- Обертання (YAW стабілізація) ---
        if (stabilizeYaw)
        {
            float desiredYaw = headBone.eulerAngles.y;
            currentYaw = Mathf.LerpAngle(currentYaw, desiredYaw, Time.deltaTime * yawSmoothSpeed);

            float yawOffset = Mathf.DeltaAngle(headBone.eulerAngles.y, currentYaw);
            float maxYawOffset = 10f; // Максимальне відхилення у градусах

            yawOffset = Mathf.Clamp(yawOffset, -maxYawOffset, maxYawOffset);
            currentYaw = headBone.eulerAngles.y + yawOffset;

            Vector3 currentEuler = headBone.eulerAngles;
            currentEuler.y = currentYaw;

            stabilizedTransform.rotation = Quaternion.Euler(currentEuler);
        }
        else
        {
            stabilizedTransform.rotation = Quaternion.Slerp(stabilizedTransform.rotation, headBone.rotation, Time.deltaTime * smoothSpeed);
        }
    }
}