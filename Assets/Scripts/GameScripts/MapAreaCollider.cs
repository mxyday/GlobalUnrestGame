using System.Collections.Generic;
using System;
using UnityEngine;

public class MapAreaCollider : MonoBehaviour
{
    public event EventHandler OnPlayerEnter;
    public event EventHandler OnPlayerExit;

    private List<PlayerMapAreas> playerMapAreasList = new List<PlayerMapAreas>();
    private void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent<PlayerMapAreas>(out PlayerMapAreas playerMapAreas) != null)
        {
            playerMapAreasList.Add(playerMapAreas);
            OnPlayerEnter?.Invoke(this, EventArgs.Empty);
            Debug.Log("Trigger entered");
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.TryGetComponent<PlayerMapAreas>(out PlayerMapAreas playerMapAreas) != null)
        {
            playerMapAreasList.Remove(playerMapAreas);
            OnPlayerExit?.Invoke(this, EventArgs.Empty);
        }
    }

    public List<PlayerMapAreas> GetPlayerMapAreasList()
    {
        return playerMapAreasList;
    }
}