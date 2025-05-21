using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    public event EventHandler OnPlayerEnter;
    public event EventHandler OnPlayerExit;

    private float captureProgress = 0f; // -1 (TeamB), 0 (neutral), 1 (TeamA)
    private const float captureSpeed = 0.25f; // швидкість зміни за секунду

    public enum State
    {
        Neutral,
        TeamA,
        TeamB,
    }

    private List<MapAreaCollider> mapAreaColliderList;
    //private float progress;
    private State state;

    private void Awake()
    {
        mapAreaColliderList = new List<MapAreaCollider>();

        foreach (Transform clild in transform)
        {
            MapAreaCollider mapAreaCollider = clild.GetComponent<MapAreaCollider>();
            if (mapAreaCollider != null)
            {
                mapAreaColliderList.Add(mapAreaCollider);
                mapAreaCollider.OnPlayerEnter += MapAreaCollider_OnPlayerEnter;
                mapAreaCollider.OnPlayerExit += MapAreaCollider_OnPlayerExit;
            }
        }

        state = State.Neutral;
    }

    private void MapAreaCollider_OnPlayerEnter(object sender, PlayerMapAreasEventArgs e)
    {
        OnPlayerEnter?.Invoke(this, e);
        Debug.Log("Player entered area");
    }

    private void MapAreaCollider_OnPlayerExit(object sender, PlayerMapAreasEventArgs e)
    {
        OnPlayerExit?.Invoke(this, e);
    }

    private void Update()
    {
        int teamACount = 0;
        int teamBCount = 0;

        foreach (MapAreaCollider mapAreaCollider in mapAreaColliderList)
        {
            foreach (var player in mapAreaCollider.GetPlayerMapAreasList())
            {
                int teamIndex = player.GetComponent<PlayerSettings>().GetTeamIndex();
                if (teamIndex == 0) teamACount++;
                else if (teamIndex == 1) teamBCount++;
            }
        }

        int netTeamInfluence = teamACount - teamBCount;
        float targetChange = netTeamInfluence * captureSpeed * Time.deltaTime;

        if ((netTeamInfluence > 0 && captureProgress < 1f) ||
            (netTeamInfluence < 0 && captureProgress > -1f))
        {
            captureProgress = Mathf.Clamp(captureProgress + targetChange, -1f, 1f);
        }
    }

    public float GetProgress()
    {
        return captureProgress;
    }

    public float GetCaptureProgress()
    {
        return captureProgress;
    }
}