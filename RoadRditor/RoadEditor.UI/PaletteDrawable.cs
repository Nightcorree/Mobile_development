using Microsoft.Maui.Graphics;
using RoadEditor.Core;
using System.Collections.Generic;

namespace RoadEditor.UI;

public class PaletteDrawable : IDrawable
{
    public MapDrawable? MainDrawable { get; set; }
    
    // Сетка 4x3 из файла Тайлы.jpg
    public readonly List<TileType> PaletteTiles = new()
    {
        TileType.RoadHorizontal, TileType.TurnTopLeft, TileType.Crossroad, TileType.RoadVertical,
        TileType.TurnTopRight,   TileType.Stone,       TileType.TurnBottomLeft, TileType.TurnBottomRight,
        TileType.TTypeRight,     TileType.TTypeDown,    TileType.TTypeUp, TileType.TTypeLeft
    };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (MainDrawable == null) return;

        float colCount = 4f;
        float rowCount = 3f;
        float tileW = dirtyRect.Width / colCount;
        float tileH = dirtyRect.Height / rowCount;

        for (int i = 0; i < PaletteTiles.Count; i++)
        {
            int col = i % 4;
            int row = i / 4;
            float x = col * tileW;
            float y = row * tileH;

            if (PaletteTiles[i] == TileType.Stone || PaletteTiles[i] == TileType.Empty)
            {
                // Рисуем просто камень (пустой тайл в центре палитры)
                MainDrawable.DrawTileImage(canvas, TileType.Stone, x, y, Math.Min(tileW, tileH));
            }
            else
            {
                // Сначала фон
                MainDrawable.DrawTileImage(canvas, TileType.Stone, x, y, Math.Min(tileW, tileH));
                // Затем дорогу
                MainDrawable.DrawTileImage(canvas, PaletteTiles[i], x, y, Math.Min(tileW, tileH));
            }
            
            // Сетка палитры
            canvas.StrokeColor = Colors.DimGray;
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(x, y, tileW, tileH);
        }
    }
}
