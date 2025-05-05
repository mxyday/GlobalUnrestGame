using Unity.Netcode;
using UnityEngine;

public class HeadTargetController : NetworkBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f;

    private Vector3 recoilOffset = Vector3.zero;

    // Синхронізація тільки позиції
    private NetworkVariable<Vector3> syncedPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Owner);

    private void Update()
    {
        if (aimTransform == null) return;

        if (IsOwner)
        {
            // Обрахунок recoil
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

            // Позиція прицілу
            Vector3 targetPosition = aimTransform.position + aimTransform.forward * distance + recoilOffset;

            // Синхронізація позиції
            syncedPosition.Value = targetPosition;

            // Застосування локально
            transform.position = targetPosition;
        }
        else
        {
            // Для інших клієнтів — оновлення позиції з мережевої змінної
            transform.position = syncedPosition.Value;
        }
    }

    public void ApplyRecoil(float recoilStrength)
    {
        if (!IsOwner) return; // Віддача тільки на локальному клієнті
        recoilOffset += Vector3.up * recoilStrength;
    }
}
