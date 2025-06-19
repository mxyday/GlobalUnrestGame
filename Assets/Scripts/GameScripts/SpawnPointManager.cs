using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnPointManager : NetworkBehaviour
{
    public static SpawnPointManager Instance { get; private set; }

    public List<GameObject> spawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeSpawnPointsForExistingPlayers();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void InitializeSpawnPointsForExistingPlayers()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("SpawnPointManager: No spawn points assigned!");
            return;
        }

        var players = FindObjectsByType<PlayerSettings>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player != null)
            {
                player.SetSpawnPoints(spawnPoints);
                Debug.Log($"Initialized spawn points for player {player.name}");
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer && spawnPoints != null && spawnPoints.Count > 0)
        {
            var players = FindObjectsByType<PlayerSettings>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerClientId == clientId && player != null)
                {
                    player.SetSpawnPoints(spawnPoints);
                    Debug.Log($"Assigned spawn points to new player {player.name} (Client ID: {clientId})");
                    break;
                }
            }
        }
    }
}