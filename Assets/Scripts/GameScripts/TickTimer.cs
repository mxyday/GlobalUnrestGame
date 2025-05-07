using System;
using UnityEngine;

public class TickRunner
{
    private readonly float tickInterval = 1f / 64f;
    private float timer = 0f;

    public void Tick(Action onTick, float deltaTime)
    {
        timer += deltaTime;

        while (timer >= tickInterval)
        {
            timer -= tickInterval;
            onTick?.Invoke();
        }
    }

    public void Reset()
    {
        timer = 0f;
    }
}