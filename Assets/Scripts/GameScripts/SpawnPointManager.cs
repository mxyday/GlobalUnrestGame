using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
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

    void Start()
    {
        var players = FindObjectsByType<PlayerSettings>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.SetSpawnPoints(spawnPoints);
        }
    }
}