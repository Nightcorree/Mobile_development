using SkiaSharp;
using RoadEditor.Core;
using System.IO;

namespace RoadEditor.UI;

public static class MapExporter
{
    public static async Task ExportToPng(RoadMap map, string targetPath)
    {
        // Загружаем исходный спрайтлист
        using var stream = await FileSystem.OpenAppPackageFileAsync("road_tiles.jpg");
        using var bitmap = SKBitmap.Decode(stream);

        int tileSize = 250; // Размер одного тайла в спрайтлисте
        int outputTileSize = 100; // Размер тайла в итоговом файле

        int width = map.Width * outputTileSize;
        int height = map.Height * outputTileSize;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Black);

        foreach (var tile in map.GetAllTiles())
        {
            if (tile.Type == TileType.Empty) continue;

            float destX = tile.X * outputTileSize;
            float destY = tile.Y * outputTileSize;
            var destRect = new SKRect(destX, destY, destX + outputTileSize, destY + outputTileSize);

            // 1. Рисуем фон (камень) всегда под дорогой или если это просто камень
            var stoneRect = GetSourceRect(TileType.Stone, tileSize);
            canvas.DrawBitmap(bitmap, stoneRect, destRect);

            // 2. Если это дорога, рисуем её поверх камня
            if (tile.Type != TileType.Stone)
            {
                var roadRect = GetSourceRect(tile.Type, tileSize);
                canvas.DrawBitmap(bitmap, roadRect, destRect);
            }
        }

        // Сохраняем результат
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var saveStream = File.OpenWrite(targetPath);
        data.SaveTo(saveStream);
    }

    private static SKRect GetSourceRect(TileType type, int size)
    {
        var (x, y) = type switch
        {
            TileType.RoadHorizontal => (0, 0),
            TileType.TurnTopLeft => (250, 0),
            TileType.Crossroad => (500, 0),
            TileType.RoadVertical => (750, 0),
            TileType.TurnTopRight => (0, 250),
            TileType.Stone => (250, 250),
            TileType.TurnBottomLeft => (500, 250),
            TileType.TurnBottomRight => (750, 250),
            TileType.TTypeRight => (0, 500),
            TileType.TTypeDown => (250, 500),
            TileType.TTypeUp => (500, 500),
            TileType.TTypeLeft => (750, 500),
            _ => (250, 250)
        };

        return new SKRect(x, y, x + size, y + size);
    }
}
