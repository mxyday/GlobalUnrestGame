using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MapAreasUI : MonoBehaviour
{
    [SerializeField] private List<MapArea> mapAreaList;

    [SerializeField] private Slider ATeamSlider;
    [SerializeField] private Slider BTeamSlider;

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
        if (mapArea == null) return;

        float progress = mapArea.GetCaptureProgress(); // від -1 до 1

        ATeamSlider.value = progress > 0 ? progress : 0f;
        BTeamSlider.value = progress < 0 ? -progress : 0f;
    }

    private void MapArea_OnPlayerEnter(object sender, EventArgs e)
    {
        if (e is PlayerMapAreasEventArgs args)
        {
            var settings = args.PlayerMapAreas.GetComponent<PlayerSettings>();
            if (settings != null && settings.IsOwner)
            {
                mapArea = sender as MapArea;
                Show();
            }
        }
    }

    private void MapArea_OnPlayerExit(object sender, EventArgs e)
    {
        if (e is PlayerMapAreasEventArgs args)
        {
            var settings = args.PlayerMapAreas.GetComponent<PlayerSettings>();
            if (settings != null && settings.IsOwner)
            {
                Hide();
            }
        }
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
