using Microsoft.Maui.Graphics;
using RoadEditor.Core;

namespace RoadEditor.UI;

public class MapDrawable : IDrawable
{
    public RoadMap Map { get; set; }
    public float TileSize { get; set; } = 40f;
    public float OffsetX { get; set; } = 0f;
    public float OffsetY { get; set; } = 0f;
    public float Zoom { get; set; } = 1.0f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Map == null) return;

        canvas.SaveState();
        
        // Применяем трансформации (масштаб и смещение)
        canvas.Translate(OffsetX, OffsetY);
        canvas.Scale(Zoom, Zoom);

        float currentTileSize = TileSize;

        // 1. Рисуем сетку
        canvas.StrokeColor = Colors.LightGray;
        canvas.StrokeSize = 1;

        for (int x = 0; x <= Map.Width; x++)
        {
            canvas.DrawLine(x * currentTileSize, 0, x * currentTileSize, Map.Height * currentTileSize);
        }

        for (int y = 0; y <= Map.Height; y++)
        {
            canvas.DrawLine(0, y * currentTileSize, Map.Width * currentTileSize, y * currentTileSize);
        }

        // 2. Рисуем тайлы (пока схематично)
        foreach (var tile in Map.GetAllTiles())
        {
            if (tile.Type != TileType.Empty)
            {
                DrawTilePlaceholder(canvas, tile, currentTileSize);
            }
        }

        canvas.RestoreState();
    }

    private void DrawTilePlaceholder(ICanvas canvas, Tile tile, float size)
    {
        float x = tile.X * size;
        float y = tile.Y * size;
        float mid = size / 2f;
        float roadWidth = size * 0.6f;

        canvas.FillColor = Colors.DarkSlateGray;
        
        switch (tile.Type)
        {
            case TileType.RoadHorizontal:
                canvas.FillRectangle(x, y + (size - roadWidth) / 2, size, roadWidth);
                break;
            case TileType.RoadVertical:
                canvas.FillRectangle(x + (size - roadWidth) / 2, y, roadWidth, size);
                break;
            case TileType.Crossroad:
                canvas.FillRectangle(x, y + (size - roadWidth) / 2, size, roadWidth);
                canvas.FillRectangle(x + (size - roadWidth) / 2, y, roadWidth, size);
                break;
            // Другие типы добавим позже или упростим для прототипа
            default:
                canvas.FillCircle(x + mid, y + mid, roadWidth / 2);
                break;
        }
    }
}
