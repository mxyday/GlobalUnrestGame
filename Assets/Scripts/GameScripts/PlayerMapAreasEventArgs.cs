using System;

public class PlayerMapAreasEventArgs : EventArgs
{
    public PlayerMapAreas PlayerMapAreas { get; }

    public PlayerMapAreasEventArgs(PlayerMapAreas playerMapAreas)
    {
        PlayerMapAreas = playerMapAreas;
    }
}
