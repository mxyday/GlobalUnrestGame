using System.Collections.Generic;
using System;
using UnityEngine;

public class MapAreaCollider : MonoBehaviour
{
    public event EventHandler<PlayerMapAreasEventArgs> OnPlayerEnter;
    public event EventHandler<PlayerMapAreasEventArgs> OnPlayerExit;

    private List<PlayerMapAreas> playerMapAreasList = new List<PlayerMapAreas>();

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent<PlayerMapAreas>(out PlayerMapAreas playerMapAreas))
        {
            playerMapAreasList.Add(playerMapAreas);
            Debug.Log($"[MapAreaCollider] Player entered: {playerMapAreas.gameObject.name}");
            OnPlayerEnter?.Invoke(this, new PlayerMapAreasEventArgs(playerMapAreas));
        }
        else
        {
            Debug.Log($"[MapAreaCollider] Object entered but has no PlayerMapAreas: {collider.gameObject.name}");
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.TryGetComponent<PlayerMapAreas>(out PlayerMapAreas playerMapAreas))
        {
            playerMapAreasList.Remove(playerMapAreas);
            OnPlayerExit?.Invoke(this, new PlayerMapAreasEventArgs(playerMapAreas));

        }
    }

    public List<PlayerMapAreas> GetPlayerMapAreasList()
    {
        return playerMapAreasList;
    }
}