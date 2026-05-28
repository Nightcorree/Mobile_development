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
            canvas.FillColor = Colors.IndianRed;
            canvas.FillRectangle(0, 0, size, size);
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2;
            canvas.DrawLine(0, 0, size, size);
            canvas.DrawLine(size, 0, 0, size);
        }
        else
        {
            MainDrawable.DrawTileImage(canvas, SelectedType, 0, 0, size);
        }
    }
}
