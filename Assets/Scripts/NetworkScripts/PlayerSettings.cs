using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using System.Collections.Generic;

public class PlayerSettings : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private TextMeshProUGUI playerName;
    public List<Color> colors = new List<Color>();

    private NetworkVariable<FixedString128Bytes> networkPlayerName = new NetworkVariable<FixedString128Bytes>(
        "Player: 0", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> ownerTeamId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        networkPlayerName.Value = "Player: " + (OwnerClientId + 1);

        if (playerName != null)
            playerName.text = networkPlayerName.Value.ToString();

        UpdateTeamColor(ownerTeamId.Value);

        // Підписуємось на зміну команди
        ownerTeamId.OnValueChanged += (oldValue, newValue) =>
        {
            UpdateTeamColor(newValue);
        };
    }

    private void UpdateTeamColor(int teamIndex)
    {
        if (teamIndex >= 0 && teamIndex < colors.Count)
        {
            Material newMaterial = new Material(skinnedMeshRenderer.material);
            newMaterial.color = colors[teamIndex];
            skinnedMeshRenderer.material = newMaterial;
        }
    }

    public void RequestChangeTeamColor(int teamIndex)
    {
        SetTeamColorServerRpc(teamIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTeamColorServerRpc(int teamIndex)
    {
        ownerTeamId.Value = teamIndex;
    }
}