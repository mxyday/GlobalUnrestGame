using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;

public class PlayerSettings : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private RagdollActivator ragdollActivator;
    [SerializeField] private PlayerController playerController;
    public List<Material> teamMaterials = new List<Material>();

    public Transform spawnPoint;

    private List<GameObject> spawnPoints; // більше не задається в інспекторі, отримаємо через метод

    private NetworkVariable<FixedString128Bytes> networkPlayerName = new NetworkVariable<FixedString128Bytes>(
        "Player: 0", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> ownerTeamId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public List<SingleShotGun> playerWeapons = new List<SingleShotGun>();

    private void Awake()
    {
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        playerWeapons = new List<SingleShotGun>(GetComponentsInChildren<SingleShotGun>());
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) // Тільки власник об'єкта ініціалізує ім'я
        {
            SetPlayerNameServerRpc("Player: " + (OwnerClientId + 1));
        }

        if (playerName != null)
            playerName.text = networkPlayerName.Value.ToString();

        // Підписка на зміни імені
        networkPlayerName.OnValueChanged += (oldValue, newValue) =>
        {
            playerName.text = newValue.ToString();
        };

        StartCoroutine(WaitForSpawnPointManager());

        ownerTeamId.OnValueChanged += (oldValue, newValue) =>
        {
            UpdateTeam(newValue);
        };
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(FixedString128Bytes newName)
    {
        networkPlayerName.Value = newName;
    }

    private IEnumerator WaitForSpawnPointManager()
    {
        while (SpawnPointManager.Instance == null)
        {
            yield return null;
        }

        SetSpawnPoints(SpawnPointManager.Instance.spawnPoints);
    }

    private void UpdateTeam(int teamIndex)
    {
        if (teamIndex >= 0 && teamIndex < teamMaterials.Count && teamMaterials[teamIndex] != null)
        {
            skinnedMeshRenderer.material = teamMaterials[teamIndex];

            if (spawnPoints != null && teamIndex < spawnPoints.Count && spawnPoints[teamIndex] != null)
            {
                spawnPoint = spawnPoints[teamIndex].transform;
            }
            else
            {
                Debug.LogWarning("Spawn point for team not assigned or out of range.");
            }
        }
    }

    public void RequestChangeTeam(int teamIndex)
    {
        Debug.Log($"[Client] Requesting team change to {teamIndex}");
        SetTeamServerRpc(teamIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTeamServerRpc(int teamIndex)
    {
        Debug.Log($"[Server] Changing team to {teamIndex} for player {OwnerClientId}");

        ownerTeamId.Value = teamIndex;
        UpdateTeam(teamIndex);
        Respawn();
    }

    public void Respawn()
    {
        if (spawnPoint != null)
        {
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

            playerController.Resurrect();

            if (ragdollActivator != null)
            {
                ragdollActivator.DeactivateRagdoll();
            }

            foreach (SingleShotGun weapon in playerWeapons)
            {
                weapon.RestoreAmmo();
                Debug.Log($"Restored ammo for {weapon}");
            }
        }
        else
        {
            Debug.LogWarning("No valid spawn point set.");
        }
    }

    public void SetSpawnPoints(List<GameObject> points)
    {
        spawnPoints = points;
        UpdateTeam(ownerTeamId.Value); // одразу оновимо спавн
    }

    public int GetTeamIndex()
    {
        return ownerTeamId.Value;
    }
}
