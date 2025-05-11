using UnityEngine;

public class HandsGrip : MonoBehaviour
{
    [Header("Source Object")]
    [SerializeField] private Transform sourceObject;  // Об'єкт, з якого береться трансформ

    [Header("Target Objects")]
    [SerializeField] private Transform targetObject1; // Перший цільовий об'єкт
    [SerializeField] private Transform targetObject2; // Другий цільовий об'єкт

    [Header("Settings")]
    [SerializeField] private bool syncPosition = true;
    [SerializeField] private bool syncRotation = true;
    [SerializeField] private bool syncScale = false;  // Увімкнути, якщо потрібно синхронізувати масштаб

    private Vector3 initialOffset1;  // Початковий офсет targetObject1 відносно sourceObject
    private Vector3 initialOffset2;  // Початковий офсет targetObject2 відносно sourceObject
    private Quaternion initialRotationOffset1;  // Початковий обертальний офсет targetObject1
    private Quaternion initialRotationOffset2;  // Початковий обертальний офсет targetObject2

    private void Start()
    {
        if (sourceObject == null || targetObject1 == null || targetObject2 == null)
        {
            Debug.LogError("Не всі об'єкти призначені в інспекторі!");
            enabled = false;
            return;
        }

        // Зберігаємо початкові офсети
        initialOffset1 = targetObject1.position - sourceObject.position;
        initialOffset2 = targetObject2.position - sourceObject.position;

        // Зберігаємо початкові обертальні офсети
        initialRotationOffset1 = Quaternion.Inverse(sourceObject.rotation) * targetObject1.rotation;
        initialRotationOffset2 = Quaternion.Inverse(sourceObject.rotation) * targetObject2.rotation;
    }

    private void Update()
    {
        if (syncPosition)
        {
            // Оновлюємо позиції з урахуванням офсету
            targetObject1.position = sourceObject.position + sourceObject.TransformDirection(initialOffset1);
            targetObject2.position = sourceObject.position + sourceObject.TransformDirection(initialOffset2);
        }

        if (syncRotation)
        {
            // Оновлюємо обертання з урахуванням офсету
            targetObject1.rotation = sourceObject.rotation * initialRotationOffset1;
            targetObject2.rotation = sourceObject.rotation * initialRotationOffset2;
        }

        if (syncScale)
        {
            // Масштаб синхронізується без офсету (якщо потрібно)
            targetObject1.localScale = sourceObject.localScale;
            targetObject2.localScale = sourceObject.localScale;
        }
    }
}