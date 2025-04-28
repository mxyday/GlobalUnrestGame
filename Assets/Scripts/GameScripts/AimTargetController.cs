using UnityEngine;

public class AimTargetController : MonoBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f; // Швидкість відновлення віддачі

    private Vector3 recoilOffset = Vector3.zero;

    private void Update()
    {
        if (aimTransform == null) return;

        // Поступово зменшуємо recoilOffset назад до нуля
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        // Основне розташування з урахуванням recoil
        transform.position = aimTransform.position + aimTransform.forward * distance + recoilOffset;
    }

    /// <summary>
    /// Викликається коли потрібно застосувати віддачу
    /// </summary>
    /// <param name="recoilStrength">Сила віддачі</param>
    public void ApplyRecoil(float recoilStrength)
    {
        // Віддача йде вверх по world space
        recoilOffset += Vector3.up * recoilStrength;
    }
}
