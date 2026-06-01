using System;

namespace RoadEditor.Core;

public static class RoadAutomation
{
    public static void UpdateTileConnections(RoadMap map, int x, int y)
    {
        var currentTile = map.GetTile(x, y);
        if (currentTile == null || currentTile.Type == TileType.Empty || currentTile.Type == TileType.Stone) return;

        bool top = ShouldConnect(map, x, y, x, y - 1);
        bool bottom = ShouldConnect(map, x, y, x, y + 1);
        bool left = ShouldConnect(map, x, y, x - 1, y);
        bool right = ShouldConnect(map, x, y, x + 1, y);

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
        else // connectionCount == 0
        {
            if (currentTile.Type == TileType.Empty) currentTile.Type = TileType.RoadHorizontal;
        }
    }

    private static bool IsRoad(RoadMap map, int x, int y)
    {
        var tile = map.GetTile(x, y);
        return tile != null && tile.Type != TileType.Empty && tile.Type != TileType.Stone;
    }

    private static bool ShouldConnect(RoadMap map, int x, int y, int nx, int ny)
    {
        if (!IsRoad(map, nx, ny)) return false;

        // Направление от (x,y) к (nx,ny)
        int dx = nx - x;
        int dy = ny - y;

        if (dx != 0) // Попытка горизонтального соединения
        {
            // Не соединяем, если ОБА тайла имеют вертикальных соседей (параллельные вертикальные дороги)
            bool iHaveVert = IsRoad(map, x, y - 1) || IsRoad(map, x, y + 1);
            bool nHaveVert = IsRoad(map, nx, ny - 1) || IsRoad(map, nx, ny + 1);
            if (iHaveVert && nHaveVert) return false;
        }
        else if (dy != 0) // Попытка вертикального соединения
        {
            // Не соединяем, если ОБА тайла имеют горизонтальных соседей (параллельные горизонтальные дороги)
            bool iHaveHorz = IsRoad(map, x - 1, y) || IsRoad(map, x + 1, y);
            bool nHaveHorz = IsRoad(map, nx - 1, ny) || IsRoad(map, nx + 1, ny);
            if (iHaveHorz && nHaveHorz) return false;
        }

        return true;
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
