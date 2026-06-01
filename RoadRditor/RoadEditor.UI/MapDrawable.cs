using Microsoft.Maui.Graphics;
using RoadEditor.Core;

namespace RoadEditor.UI;

public enum ToolMode { Navigate, DrawLine, DrawRect, Eraser }

public class MapDrawable : IDrawable
{
    public RoadMap? Map { get; set; }
    public float TileSize { get; set; } = 80f; // Увеличил размер ячеек по умолчанию
    public float OffsetX { get; set; } = 0f;
    public float OffsetY { get; set; } = 0f;
    public float Zoom { get; set; } = 1.0f;

    // Свойства для предпросмотра выделения
    public ToolMode CurrentMode { get; set; } = ToolMode.DrawRect;
    public Point? SelectionStart { get; set; }
    public Point? SelectionEnd { get; set; }

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
        
        // Вместо глобальных Translate и Scale, будем вычислять координаты вручную.
        // Это часто решает проблемы с отсечением (clipping) на Windows/WinUI.

        float currentTileSize = TileSize;
        float scaledTileSize = currentTileSize * Zoom;

        // Определяем видимую область в координатах тайлов для оптимизации (Culling)
        int startX = (int)Math.Floor((-OffsetX) / (currentTileSize * Zoom));
        int startY = (int)Math.Floor((-OffsetY) / (currentTileSize * Zoom));
        int endX = (int)Math.Ceiling((dirtyRect.Width - OffsetX) / (currentTileSize * Zoom));
        int endY = (int)Math.Ceiling((dirtyRect.Height - OffsetY) / (currentTileSize * Zoom));

        // Ограничиваем границами карты
        startX = Math.Max(0, startX);
        startY = Math.Max(0, startY);
        endX = Math.Min(Map.Width - 1, endX);
        endY = Math.Min(Map.Height - 1, endY);

        // 1. Фон (Темный как в эталоне)
        // Рисуем фон только для видимой части карты
        float mapScreenWidth = Map.Width * scaledTileSize;
        float mapScreenHeight = Map.Height * scaledTileSize;
        
        canvas.FillColor = Color.FromArgb("#111111");
        // Ограничиваем отрисовку фона видимой областью
        RectF visibleMapRect = new RectF(OffsetX, OffsetY, mapScreenWidth, mapScreenHeight);
        canvas.FillRectangle(visibleMapRect);

        // 2. Сетка
        canvas.StrokeColor = Color.FromRgba(255, 255, 255, 50);
        canvas.StrokeSize = 0.5f;
        
        // Вертикальные линии
        for (int x = Math.Max(0, startX); x <= Math.Min(Map.Width, endX + 1); x++)
        {
            float lx = OffsetX + x * scaledTileSize;
            canvas.DrawLine(lx, OffsetY, lx, OffsetY + Map.Height * scaledTileSize);
        }
        // Горизонтальные линии
        for (int y = Math.Max(0, startY); y <= Math.Min(Map.Height, endY + 1); y++)
        {
            float ly = OffsetY + y * scaledTileSize;
            canvas.DrawLine(OffsetX, ly, OffsetX + Map.Width * scaledTileSize, ly);
        }

        // 3. Тайлы
        if (_imageLoaded && _spriteSheet != null)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    var tile = Map.GetTile(x, y);
                    if (tile == null || tile.Type == TileType.Empty) continue;

                    float screenX = OffsetX + x * scaledTileSize;
                    float screenY = OffsetY + y * scaledTileSize;

                    if (tile.Type == TileType.Stone)
                    {
                        DrawTileImage(canvas, TileType.Stone, screenX, screenY, scaledTileSize);
                    }
                    else
                    {
                        // Сначала рисуем "каменный" фон под дорогой
                        DrawTileImage(canvas, TileType.Stone, screenX, screenY, scaledTileSize);
                        // Затем саму дорогу
                        DrawTileImage(canvas, tile.Type, screenX, screenY, scaledTileSize);
                    }
                }
            }
        }

        // 4. Предпросмотр выделения (Overlay)
        if (SelectionStart.HasValue && SelectionEnd.HasValue && CurrentMode != ToolMode.Navigate)
        {
            DrawSelectionOverlay(canvas, scaledTileSize);
        }
        
        canvas.RestoreState();
    }

    private void DrawSelectionOverlay(ICanvas canvas, float scaledTileSize)
    {
        if (!SelectionStart.HasValue || !SelectionEnd.HasValue) return;

        canvas.FillColor = Color.FromRgba(255, 255, 255, 80); 
        
        int x1 = (int)SelectionStart.Value.X;
        int y1 = (int)SelectionStart.Value.Y;
        int x2 = (int)SelectionEnd.Value.X;
        int y2 = (int)SelectionEnd.Value.Y;

        if (CurrentMode == ToolMode.DrawRect || CurrentMode == ToolMode.Eraser)
        {
            int sX = Math.Min(x1, x2);
            int sY = Math.Min(y1, y2);
            int w = Math.Abs(x1 - x2) + 1;
            int h = Math.Abs(y1 - y2) + 1;

            canvas.FillRectangle(OffsetX + sX * scaledTileSize, OffsetY + sY * scaledTileSize, w * scaledTileSize, h * scaledTileSize);
        }
        else if (CurrentMode == ToolMode.DrawLine)
        {
            if (Math.Abs(x1 - x2) > Math.Abs(y1 - y2))
            {
                int sX = Math.Min(x1, x2);
                int width = Math.Abs(x1 - x2) + 1;
                canvas.FillRectangle(OffsetX + sX * scaledTileSize, OffsetY + y1 * scaledTileSize, width * scaledTileSize, scaledTileSize);
            }
            else
            {
                int sY = Math.Min(y1, y2);
                int height = Math.Abs(y1 - y2) + 1;
                canvas.FillRectangle(OffsetX + x1 * scaledTileSize, OffsetY + sY * scaledTileSize, scaledTileSize, height * scaledTileSize);
            }
        }
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
        
        // Создаем область отсечения для конкретного тайла
        canvas.ClipRectangle(destX, destY, destSize, destSize);
        
        float scaleX = destSize / srcW;
        float scaleY = destSize / srcH;
        
        // Рисуем изображение с учетом смещения, чтобы нужная часть спрайт-листа попала в область отсечения
        canvas.DrawImage(_spriteSheet, 
            destX - (srcX * scaleX), 
            destY - (srcY * scaleY), 
            _spriteSheet.Width * scaleX, 
            _spriteSheet.Height * scaleY);
        
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
            TileType.TurnTopRight => (w, 0, w, h),      
            TileType.Crossroad => (w * 2, 0, w, h),
            TileType.RoadVertical => (w * 3, 0, w, h),
            
            // Ряд 2 (y=h)
            TileType.TurnTopLeft => (0, h, w, h),        
            TileType.Stone => (w, h, w, h),             
            TileType.TurnBottomRight => (w * 2, h, w, h), 
            TileType.TurnBottomLeft => (w * 3, h, w, h),  
            
            // Ряд 3 (y=2h)
            TileType.TTypeRight => (0, h * 2, w, h),      
            TileType.TTypeUp => (w, h * 2, w, h),       
            TileType.TTypeDown => (w * 2, h * 2, w, h),     
            TileType.TTypeLeft => (w * 3, h * 2, w, h),   
            
            _ => (w, h, w, h) // По умолчанию камень
        };
    }
}
