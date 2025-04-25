using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Collections.Generic;

public class PlayerSettings : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private TextMeshProUGUI playerName;
    private NetworkVariable<FixedString128Bytes> networkPlayerName = new NetworkVariable<FixedString128Bytes>(
        "Player: 0", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> OwnerTeamId = new NetworkVariable<int>(
    value: 0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public List<Color> colors = new List<Color>();

    private void Awake()
    {
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        networkPlayerName.Value = "Player: " + (OwnerClientId + 1);
        if (playerName != null) playerName.text = networkPlayerName.Value.ToString();
    }

    [ServerRpc]
    public void SetTeamColorServerRpc(int teamId)
    {
        OwnerTeamId.Value = teamId;
        SetTeamColorClientRpc(teamId);
    }

    [ClientRpc]
    private void SetTeamColorClientRpc(int teamId)
    {
        Material newMaterial = new Material(skinnedMeshRenderer.material);
        newMaterial.color = colors[teamId];
        skinnedMeshRenderer.material = newMaterial;
    }
}