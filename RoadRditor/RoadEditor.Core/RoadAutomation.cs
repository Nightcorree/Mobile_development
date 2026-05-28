using System;

namespace RoadEditor.Core;

public static class RoadAutomation
{
    public static void UpdateTileConnections(RoadMap map, int x, int y)
    {
        var currentTile = map.GetTile(x, y);
        if (currentTile == null || currentTile.Type == TileType.Empty) return;

        bool top = IsRoad(map, x, y - 1);
        bool bottom = IsRoad(map, x, y + 1);
        bool left = IsRoad(map, x - 1, y);
        bool right = IsRoad(map, x + 1, y);

        int connectionCount = (top ? 1 : 0) + (bottom ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        if (connectionCount == 4)
        {
            currentTile.Type = TileType.Crossroad;
        }
        else if (connectionCount == 3)
        {
            if (!top) currentTile.Type = TileType.TTypeDown;
            else if (!bottom) currentTile.Type = TileType.TTypeUp;
            else if (!left) currentTile.Type = TileType.TTypeRight;
            else currentTile.Type = TileType.TTypeLeft;
        }
        else if (connectionCount == 2)
        {
            if (top && bottom) currentTile.Type = TileType.RoadVertical;
            else if (left && right) currentTile.Type = TileType.RoadHorizontal;
            else if (top && left) currentTile.Type = TileType.TurnTopLeft;
            else if (top && right) currentTile.Type = TileType.TurnTopRight;
            else if (bottom && left) currentTile.Type = TileType.TurnBottomLeft;
            else if (bottom && right) currentTile.Type = TileType.TurnBottomRight;
        }
        else if (connectionCount == 1)
        {
            if (top || bottom) currentTile.Type = TileType.RoadVertical;
            else currentTile.Type = TileType.RoadHorizontal;
        }
    }

    private static bool IsRoad(RoadMap map, int x, int y)
    {
        var tile = map.GetTile(x, y);
        return tile != null && tile.Type != TileType.Empty;
    }

    public static void AutoUpdateNeighbors(RoadMap map, int x, int y)
    {
        UpdateTileConnections(map, x, y);
        UpdateTileConnections(map, x, y - 1);
        UpdateTileConnections(map, x, y + 1);
        UpdateTileConnections(map, x - 1, y);
        UpdateTileConnections(map, x + 1, y);
    }
}
