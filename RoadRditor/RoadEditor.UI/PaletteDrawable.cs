using Microsoft.Maui.Graphics;
using RoadEditor.Core;
using System.Collections.Generic;

namespace RoadEditor.UI;

public class PaletteDrawable : IDrawable
{
    public MapDrawable MainDrawable { get; set; }
    
    // Список тайлов для палитры (3x3 сетка)
    public readonly List<TileType> PaletteTiles = new()
    {
        TileType.TurnTopLeft, TileType.RoadVertical, TileType.TurnTopRight,
        TileType.RoadHorizontal, TileType.Crossroad, TileType.RoadHorizontal,
        TileType.TurnBottomLeft, TileType.RoadVertical, TileType.TurnBottomRight
    };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (MainDrawable == null) return;

        float tileSize = dirtyRect.Width / 3f;

        for (int i = 0; i < PaletteTiles.Count; i++)
        {
            int col = i % 3;
            int row = i / 3;
            float x = col * tileSize;
            float y = row * tileSize;

            // Сначала рисуем фон (камень)
            MainDrawable.DrawSpritePart(canvas, 32f, 32f, 32f, x, y, tileSize);
            
            // Затем саму дорогу из палитры
            MainDrawable.DrawTileImage(canvas, PaletteTiles[i], x, y, tileSize);
            
            // Рисуем сетку
            canvas.StrokeColor = Colors.DarkGray;
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(x, y, tileSize, tileSize);
        }
    }
}
