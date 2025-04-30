using UnityEngine;
using Unity.Netcode;

public class NetworkHitEffect : NetworkBehaviour
{
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float effectDuration = 2f;

    public void PlayEffect(Vector3 position, Vector3 normal)
    {
        // Локальне відтворення для стрільця
        SpawnEffect(position, normal);

        // Відправка на всіх інших клієнтів
        if (IsServer)
        {
            PlayEffectClientRpc(position, normal);
        }
        else if (IsOwner)
        {
            PlayEffectServerRpc(position, normal);
        }
    }

    [ServerRpc]
    private void PlayEffectServerRpc(Vector3 position, Vector3 normal)
    {
        PlayEffectClientRpc(position, normal);
    }

    [ClientRpc]
    private void PlayEffectClientRpc(Vector3 position, Vector3 normal)
    {
        if (!IsOwner) // Щоб уникнути дублювання
        {
            SpawnEffect(position, normal);
        }
    }

    private void SpawnEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(effect, effectDuration);
        }
    }
}