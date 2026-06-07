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
        var nb = map.GetTile(nx, ny);
        if (nb == null || nb.Type == TileType.Empty || nb.Type == TileType.Stone) return false;

        var current = map.GetTile(x, y);
        if (current == null) return false;

        // Направление от (x,y) к (nx,ny)
        int dx = nx - x;
        int dy = ny - y;

        if (dx != 0) // Попытка горизонтального соединения
        {
            // Не соединяем горизонтально, если ОБА тайла являются "чисто вертикальными" дорогами,
            // у которых уже есть вертикальные соседи. Это предотвращает "слипание" параллельных вертикальных дорог.
            bool iAmVertical = current.Type == TileType.RoadVertical && (IsRoad(map, x, y - 1) || IsRoad(map, x, y + 1));
            bool nbIsVertical = nb.Type == TileType.RoadVertical && (IsRoad(map, nx, ny - 1) || IsRoad(map, nx, ny + 1));
            
            if (iAmVertical && nbIsVertical) return false;
        }
        else if (dy != 0) // Попытка вертикального соединения
        {
            // Не соединяем вертикально, если ОБА тайла являются "чисто горизонтальными" дорогами,
            // у которых уже есть горизонтальные соседи.
            bool iAmHorizontal = current.Type == TileType.RoadHorizontal && (IsRoad(map, x - 1, y) || IsRoad(map, x + 1, y));
            bool nbIsHorizontal = nb.Type == TileType.RoadHorizontal && (IsRoad(map, nx - 1, ny) || IsRoad(map, nx + 1, ny));
            
            if (iAmHorizontal && nbIsHorizontal) return false;
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

    public static void GenerateRandomRoad(RoadMap map, int targetCrossroads)
    {
        // 1. Полная очистка карты
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                map.SetTile(x, y, TileType.Empty);

        Random rand = new();

        // 2. Определение осевых линий с шагом 3 (для большей плотности на малых картах)
        List<int> hLanes = new();
        for (int y = 1; y < map.Height - 1; y += 3) hLanes.Add(y);
        
        List<int> vLanes = new();
        for (int x = 1; x < map.Width - 1; x += 3) vLanes.Add(x);

        if (hLanes.Count == 0 || vLanes.Count == 0) return;

        // 3. Выбор линий (берем с запасом, чтобы потом проредить)
        int hCount = Math.Min(hLanes.Count, (int)Math.Sqrt(targetCrossroads) + 3);
        int vCount = Math.Min(vLanes.Count, (int)Math.Sqrt(targetCrossroads) + 3);
        
        var selectedH = hLanes.OrderBy(_ => rand.Next()).Take(hCount).ToList();
        var selectedV = vLanes.OrderBy(_ => rand.Next()).Take(vCount).ToList();

        // 4. Отрисовка базовой сетки
        foreach (int y in selectedH)
            for (int x = 0; x < map.Width; x++)
                map.SetTile(x, y, TileType.RoadHorizontal);

        foreach (int x in selectedV)
            for (int y = 0; y < map.Height; y++)
                map.SetTile(x, y, TileType.RoadHorizontal);

        // Сразу чистим тупики, которые могли возникнуть из-за обрывов линий на краях
        CleanupDeadEnds(map);

        // 5. Прореживание до нужного количества перекрестков
        while (true)
        {
            List<(int x, int y)> crosses = new();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (IsRoad(map, x, y))
                    {
                        int neighbors = GetNeighborCount(map, x, y);
                        if (neighbors >= 3) crosses.Add((x, y));
                    }
                }
            }

            if (crosses.Count <= targetCrossroads) break;

            // Выбираем случайный перекресток и разрываем одну связь
            var (cx, cy) = crosses[rand.Next(crosses.Count)];
            int dir = rand.Next(4);
            int dx = dir == 0 ? 1 : (dir == 1 ? -1 : 0);
            int dy = dir == 2 ? 1 : (dir == 3 ? -1 : 0);
            
            if (cx + dx >= 0 && cx + dx < map.Width && cy + dy >= 0 && cy + dy < map.Height)
                map.SetTile(cx + dx, cy + dy, TileType.Empty);
            
            CleanupDeadEnds(map);
        }

        // 6. Финальное обновление
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                UpdateTileConnections(map, x, y);
    }

    private static int GetNeighborCount(RoadMap map, int x, int y)
    {
        int count = 0;
        if (IsRoad(map, x, y - 1)) count++;
        if (IsRoad(map, x, y + 1)) count++;
        if (IsRoad(map, x - 1, y)) count++;
        if (IsRoad(map, x + 1, y)) count++;
        return count;
    }

    private static void CleanupDeadEnds(RoadMap map)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (IsRoad(map, x, y))
                    {
                        int neighbors = GetNeighborCount(map, x, y);
                        bool isEdge = (x == 0 || x == map.Width - 1 || y == 0 || y == map.Height - 1);
                        
                        // Плитка удаляется если:
                        // 1. У нее вообще нет соседей (изолированная точка)
                        // 2. У нее 1 сосед и она НЕ на краю (тупик внутри)
                        if (neighbors == 0 || (neighbors == 1 && !isEdge))
                        {
                            map.SetTile(x, y, TileType.Empty);
                            changed = true;
                        }
                    }
                }
            }
        }
    }




    private static void ConnectPoints(RoadMap map, (int x, int y) p1, (int x, int y) p2)
    {
        int cx = p1.x;
        int cy = p1.y;

        while (cx != p2.x)
        {
            map.SetTile(cx, cy, TileType.RoadHorizontal);
            cx += Math.Sign(p2.x - cx);
        }
        while (cy != p2.y)
        {
            map.SetTile(cx, cy, TileType.RoadHorizontal);
            cy += Math.Sign(p2.y - cy);
        }
        map.SetTile(p2.x, p2.y, TileType.RoadHorizontal);
    }
}
