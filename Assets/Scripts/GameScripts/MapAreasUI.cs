using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapAreasUI : MonoBehaviour
{
    [SerializeField] private List<MapArea> mapAreaList;

    private MapArea mapArea;
    private Slider mapAreaProgress;

    private void Awake()
    {
        mapAreaProgress = GetComponentInChildren<Slider>();
    }

    private void Start()
    {
        foreach (MapArea mapArea in mapAreaList)
        {
            mapArea.OnPlayerEnter += MapArea_OnPlayerEnter;
            mapArea.OnPlayerExit += MapArea_OnPlayerExit;
        }

        Hide();
    }

    private void Update()
    {
        if (mapArea != null)
        {
            mapAreaProgress.value = mapArea.GetProgress();
        }
    }

    private void MapArea_OnPlayerEnter(object sender, System.EventArgs e)
    {
        mapArea = sender as MapArea;
        Show();
    }

    private void MapArea_OnPlayerExit(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
