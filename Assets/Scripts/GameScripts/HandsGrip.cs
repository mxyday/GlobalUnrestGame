using UnityEngine;

public class HandsGrip : MonoBehaviour
{
    [Header("Source Object")]
    [SerializeField] private Transform sourceObject;

    [Header("Target Objects")]
    [SerializeField] private Transform targetObject1;
    [SerializeField] private Transform targetObject2;

    [Header("Settings")]
    [SerializeField] private bool syncPosition = true;
    [SerializeField] private bool syncRotation = true;
    [SerializeField] private bool syncScale = false;

    private Vector3 initialOffset1;
    private Vector3 initialOffset2;
    private Quaternion initialRotationOffset1;
    private Quaternion initialRotationOffset2;

    private void Start()
    {
        if (sourceObject == null || targetObject1 == null || targetObject2 == null)
        {
            Debug.LogError("Не всі об'єкти призначені в інспекторі!");
            enabled = false;
            return;
        }

        initialOffset1 = targetObject1.position - sourceObject.position;
        initialOffset2 = targetObject2.position - sourceObject.position;

        initialRotationOffset1 = Quaternion.Inverse(sourceObject.rotation) * targetObject1.rotation;
        initialRotationOffset2 = Quaternion.Inverse(sourceObject.rotation) * targetObject2.rotation;
    }

    private void Update()
    {
        if (syncPosition)
        {
            targetObject1.position = sourceObject.position + sourceObject.TransformDirection(initialOffset1);
            targetObject2.position = sourceObject.position + sourceObject.TransformDirection(initialOffset2);
        }

        if (syncRotation)
        {
            targetObject1.rotation = sourceObject.rotation * initialRotationOffset1;
            targetObject2.rotation = sourceObject.rotation * initialRotationOffset2;
        }

        if (syncScale)
        {
            targetObject1.localScale = sourceObject.localScale;
            targetObject2.localScale = sourceObject.localScale;
        }
    }
}