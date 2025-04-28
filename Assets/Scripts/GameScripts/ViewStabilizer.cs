using UnityEngine;

public class ViewStabilizer : MonoBehaviour
{
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float smoothSpeed = 10f;

    private Vector3 lastHeadPosition;
    private Quaternion lastHeadRotation;

    private void Start()
    {
        lastHeadPosition = headBone.position;
        lastHeadRotation = headBone.rotation;
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = headBone.position;
        Quaternion desiredRotation = headBone.rotation;

        // Стабілізація: згладжування
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, Time.deltaTime * smoothSpeed);

        // Оновлюємо минуле положення
        lastHeadPosition = desiredPosition;
        lastHeadRotation = desiredRotation;
    }
}