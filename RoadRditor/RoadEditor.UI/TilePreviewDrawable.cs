using Microsoft.Maui.Graphics;
using RoadEditor.Core;

namespace RoadEditor.UI;

public class TilePreviewDrawable : IDrawable
{
    public TileType SelectedType { get; set; } = TileType.RoadHorizontal;
    public MapDrawable? MainDrawable { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (MainDrawable == null) return;
        
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        
        if (SelectedType == TileType.Empty)
        {
            // Рисуем каменный фон
            MainDrawable.DrawTileImage(canvas, TileType.Stone, 0, 0, size);
        }
        else
        {
            // Сначала каменный фон
            MainDrawable.DrawTileImage(canvas, TileType.Stone, 0, 0, size);
            // Поверх - текстура дороги
            MainDrawable.DrawTileImage(canvas, SelectedType, 0, 0, size);
        }
    }
}
