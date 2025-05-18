using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    public event EventHandler OnPlayerEnter;
    public event EventHandler OnPlayerExit;

    public enum State
    {
        Neutral,
        TeamA,
        TeamB,
    }

    private List<MapAreaCollider> mapAreaColliderList;
    private float progress;
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

    private void MapAreaCollider_OnPlayerEnter(object sender, EventArgs e)
    {
        OnPlayerEnter?.Invoke(this, EventArgs.Empty);
        Debug.Log("Player entered area");
    }

    private void MapAreaCollider_OnPlayerExit(object sender, EventArgs e)
    {
        OnPlayerExit?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Neutral:
                int playerCountInsideMapArea = 0;
                foreach (MapAreaCollider mapAreaCollider in mapAreaColliderList)
                {
                    playerCountInsideMapArea += mapAreaCollider.GetPlayerMapAreasList().Count;
                }

                float progressSpeed = 0.02f;
                progress += playerCountInsideMapArea * progressSpeed * Time.deltaTime;

                Debug.Log("Progress: " + progress);

                if (progress >= 1f)
                {
                    state = State.TeamA;
                    Debug.Log("Team A captured the point");
                }

                break;
            case State.TeamA:
                break;
        }
    }

    public float GetProgress()
    {
        return progress;
    }
}