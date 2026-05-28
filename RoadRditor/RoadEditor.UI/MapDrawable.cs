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

    public Action? RequestRedraw { get; set; }
    private Microsoft.Maui.Graphics.IImage? _spriteSheet;
    private bool _imageLoaded = false;
    private bool _isLoading = false;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Map == null) return;

        if (!_imageLoaded && !_isLoading)
        {
            LoadResources();
        }

        canvas.SaveState();
        
        canvas.Translate(OffsetX, OffsetY);
        canvas.Scale(Zoom, Zoom);

        float currentTileSize = TileSize;

        // 1. Фон (Темный как в эталоне)
        canvas.FillColor = Color.FromArgb("#111111");
        canvas.FillRectangle(0, 0, Map.Width * currentTileSize, Map.Height * currentTileSize);

        // 2. Сетка
        canvas.StrokeColor = Color.FromRgba(255, 255, 255, 50);
        canvas.StrokeSize = 0.5f;
        for (int x = 0; x <= Map.Width; x++)
            canvas.DrawLine(x * currentTileSize, 0, x * currentTileSize, Map.Height * currentTileSize);
        for (int y = 0; y <= Map.Height; y++)
            canvas.DrawLine(0, y * currentTileSize, Map.Width * currentTileSize, y * currentTileSize);

        // 3. Тайлы
        if (_imageLoaded && _spriteSheet != null)
        {
            foreach (var tile in Map.GetAllTiles())
            {
                float x = tile.X * currentTileSize;
                float y = tile.Y * currentTileSize;

                if (tile.Type == TileType.Empty)
                {
                    // Для Empty ничего не рисуем (будет черная сетка)
                }
                else
                {
                    // Сначала рисуем "каменный" фон под дорогой (как в эталоне)
                    DrawSpritePart(canvas, 32f, 32f, 32f, x, y, currentTileSize);
                    
                    // Затем саму дорогу
                    DrawTileImage(canvas, tile.Type, x, y, currentTileSize);
                }
            }
        }
        
        canvas.RestoreState();
    }

    private async void LoadResources()
    {
        _isLoading = true;
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("road_tiles.jpg");
            _spriteSheet = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
            _imageLoaded = true;
            RequestRedraw?.Invoke();
        }
        catch { }
        finally { _isLoading = false; }
    }

    public void DrawTileImage(ICanvas canvas, TileType type, float x, float y, float size)
    {
        if (_spriteSheet == null) return;

        var (srcX, srcY) = GetSpriteCoordinates(type);
        DrawSpritePart(canvas, srcX, srcY, 250f, x, y, size);
    }

    public void DrawSpritePart(ICanvas canvas, float srcX, float srcY, float srcSize, float destX, float destY, float destSize)
    {
        if (_spriteSheet == null) return;

        canvas.SaveState();
        canvas.ClipRectangle(destX, destY, destSize, destSize);
        
        float scale = destSize / srcSize;
        canvas.Translate(destX - (srcX * scale), destY - (srcY * scale));
        canvas.DrawImage(_spriteSheet, 0, 0, _spriteSheet.Width * scale, _spriteSheet.Height * scale);
        
        canvas.RestoreState();
    }

    private (float x, float y) GetSpriteCoordinates(TileType type) => type switch
    {
        // Координаты в пикселях для сетки 250x250 (картинка 1000x750)
        TileType.RoadHorizontal => (0, 0),
        TileType.RoadVertical => (750, 0),
        TileType.Crossroad => (500, 0),
        
        TileType.TurnTopRight => (0, 250),    // ┓
        TileType.TurnBottomRight => (500, 250), // ┛
        TileType.TurnBottomLeft => (250, 500),  // ┗
        TileType.TurnTopLeft => (0, 250),       // (замена, если нет ┏)
        
        TileType.TTypeDown => (250, 0),         // ┳
        TileType.TTypeUp => (500, 500),         // ┻
        TileType.TTypeRight => (750, 250),      // ┣
        TileType.TTypeLeft => (250, 0),         // (замена)
        
        _ => (250, 250) // Камни (фон)
    };
}
