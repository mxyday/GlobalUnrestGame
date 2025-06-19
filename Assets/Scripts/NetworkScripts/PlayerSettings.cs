using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode.Components;

public class PlayerSettings : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private RagdollActivator ragdollActivator;
    [SerializeField] private PlayerController playerController;
    public List<Material> teamMaterials = new List<Material>();

    public Transform spawnPoint;

    private List<GameObject> spawnPoints;

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

    private IEnumerator InitializePlayer()
    {
        while (SpawnPointManager.Instance == null)
            yield return null;

        SetSpawnPoints(SpawnPointManager.Instance.spawnPoints);

        while (ownerTeamId.Value == 0 && !IsServer)
            yield return null;

        if (!UpdateTeam(ownerTeamId.Value))
        {
            yield return new WaitForSeconds(1f);
            UpdateTeam(ownerTeamId.Value);
        }

        if (IsOwner && spawnPoint != null)
            RequestRespawn();
        else
            Debug.LogWarning("Spawn point not initialized on start.");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetPlayerNameServerRpc("Player: " + (OwnerClientId + 1));
        }

        if (playerName != null)
            playerName.text = networkPlayerName.Value.ToString();

        networkPlayerName.OnValueChanged += (oldValue, newValue) =>
        {
            playerName.text = newValue.ToString();
        };

        StartCoroutine(InitializePlayer());

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

    private bool UpdateTeam(int teamIndex)
    {
        if (teamIndex >= 0 && teamIndex < teamMaterials.Count && teamMaterials[teamIndex] != null)
        {
            skinnedMeshRenderer.material = teamMaterials[teamIndex];

            if (spawnPoints != null && teamIndex < spawnPoints.Count && spawnPoints[teamIndex] != null)
            {
                spawnPoint = spawnPoints[teamIndex].transform;
                Debug.Log($"Updated spawnPoint to {spawnPoint.position} for team {teamIndex}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Spawn point for team {teamIndex} not assigned or out of range.");
            }
        }
        return false;
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
        if (UpdateTeam(teamIndex) && IsServer)
        {
            RequestRespawn();
        }
        else
        {
            Debug.LogWarning($"Failed to update team {teamIndex}, spawn point not set.");
        }
    }

    public void RequestRespawn()
    {
        if (IsOwner)
        {
            RespawnRequestServerRpc();
        }
    }

    [ServerRpc]
    private void RespawnRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        Respawn();
        RespawnClientRpc();
    }

    [ClientRpc]
    private void RespawnClientRpc()
    {
        if (IsOwner) return;

        playerController.Resurrect();

        if (ragdollActivator != null)
            ragdollActivator.DeactivateRagdoll();

        foreach (SingleShotGun weapon in playerWeapons)
        {
            weapon.RestoreAmmo();
            Debug.Log($"[Client] Restored ammo for {weapon.name}");
        }
    }

    public void Respawn()
    {
        if (!IsServer || spawnPoint == null) return;

        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(spawnPoint.position, spawnPoint.rotation, transform.localScale);
            Debug.Log($"[Server] Teleported to {spawnPoint.position}");
        }

        playerController.Resurrect();

        if (ragdollActivator != null)
            ragdollActivator.DeactivateRagdoll();

        foreach (SingleShotGun weapon in playerWeapons)
        {
            weapon.RestoreAmmo();
            Debug.Log($"[Server] Restored ammo for {weapon.name}");
        }
    }

    private IEnumerator RetryRespawn()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateTeam(ownerTeamId.Value);
        if (IsOwner && spawnPoint != null)
            RequestRespawn();
    }

    public void SetSpawnPoints(List<GameObject> points)
    {
        spawnPoints = points;
        UpdateTeam(ownerTeamId.Value);
    }

    public int GetTeamIndex()
    {
        return ownerTeamId.Value;
    }
}