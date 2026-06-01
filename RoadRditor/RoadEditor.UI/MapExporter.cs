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

        int srcW = bitmap.Width / 4;
        int srcH = bitmap.Height / 3;
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
            var stoneRect = GetSourceRect(TileType.Stone, srcW, srcH);
            canvas.DrawBitmap(bitmap, stoneRect, destRect);

            // 2. Если это дорога, рисуем её поверх камня
            if (tile.Type != TileType.Stone)
            {
                var roadRect = GetSourceRect(tile.Type, srcW, srcH);
                canvas.DrawBitmap(bitmap, roadRect, destRect);
            }
        }

        // Сохраняем результат
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var saveStream = File.OpenWrite(targetPath);
        data.SaveTo(saveStream);
    }

    private static SKRect GetSourceRect(TileType type, int w, int h)
    {
        var (x, y) = type switch
        {
            TileType.RoadHorizontal => (0, 0),
            TileType.TurnTopRight => (w, 0),
            TileType.Crossroad => (w * 2, 0),
            TileType.RoadVertical => (w * 3, 0),

            TileType.TurnTopLeft => (0, h),
            TileType.Stone => (w, h),
            TileType.TurnBottomRight => (w * 2, h),
            TileType.TurnBottomLeft => (w * 3, h),

            TileType.TTypeRight => (0, h * 2),
            TileType.TTypeUp => (w, h * 2),
            TileType.TTypeDown => (w * 2, h * 2),
            TileType.TTypeLeft => (w * 3, h * 2),

            _ => (w, h)
        };

        return new SKRect(x, y, x + w, y + h);
    }
}
