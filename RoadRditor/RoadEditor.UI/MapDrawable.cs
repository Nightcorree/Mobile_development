using Microsoft.Maui.Graphics;
using RoadEditor.Core;

namespace RoadEditor.UI;

public class MapDrawable : IDrawable
{
    public RoadMap Map { get; set; }
    public float TileSize { get; set; } = 80f; // Увеличил размер ячеек по умолчанию
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
                else if (tile.Type == TileType.Stone)
                {
                    // Рисуем только "каменный" фон
                    DrawTileImage(canvas, TileType.Stone, x, y, currentTileSize);
                }
                else
                {
                    // Сначала рисуем "каменный" фон под дорогой (как в эталоне)
                    DrawTileImage(canvas, TileType.Stone, x, y, currentTileSize);
                    
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

        var (srcX, srcY, srcW, srcH) = GetSpriteCoordinates(type);
        DrawSpritePart(canvas, srcX, srcY, srcW, srcH, x, y, size);
    }

    public void DrawSpritePart(ICanvas canvas, float srcX, float srcY, float srcW, float srcH, float destX, float destY, float destSize)
    {
        if (_spriteSheet == null) return;

        canvas.SaveState();
        canvas.ClipRectangle(destX, destY, destSize, destSize);
        
        float scaleX = destSize / srcW;
        float scaleY = destSize / srcH;
        canvas.Translate(destX - (srcX * scaleX), destY - (srcY * scaleY));
        canvas.DrawImage(_spriteSheet, 0, 0, _spriteSheet.Width * scaleX, _spriteSheet.Height * scaleY);
        
        canvas.RestoreState();
    }

    private (float x, float y, float w, float h) GetSpriteCoordinates(TileType type)
    {
        if (_spriteSheet == null) return (0, 0, 0, 0);

        float w = _spriteSheet.Width / 4f;
        float h = _spriteSheet.Height / 3f;

        return type switch
        {
            // Ряд 1 (y=0)
            TileType.RoadHorizontal => (0, 0, w, h),
            TileType.TurnTopRight => (w, 0, w, h),      // ┓
            TileType.Crossroad => (w * 2, 0, w, h),
            TileType.RoadVertical => (w * 3, 0, w, h),
            
            // Ряд 2 (y=h)
            TileType.TurnTopLeft => (0, h, w, h),        // ┏
            TileType.Stone => (w, h, w, h),             // Фон (камни)
            TileType.TurnBottomRight => (w * 2, h, w, h), // ┛
            TileType.TurnBottomLeft => (w * 3, h, w, h),  // ┗
            
            // Ряд 3 (y=2h)
            TileType.TTypeRight => (0, h * 2, w, h),      // ┣
            TileType.TTypeDown => (w, h * 2, w, h),       // ┳
            TileType.TTypeUp => (w * 2, h * 2, w, h),     // ┻
            TileType.TTypeLeft => (w * 3, h * 2, w, h),   // ┫
            
            _ => (w, h, w, h) // По умолчанию камень
        };
    }
}
