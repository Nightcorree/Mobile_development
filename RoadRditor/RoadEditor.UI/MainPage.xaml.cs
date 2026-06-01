using RoadEditor.Core;
using Microsoft.Maui.Graphics;

namespace RoadEditor.UI;

public partial class MainPage : ContentPage
{
    public MapDrawable MapDrawable { get; } = new MapDrawable();
    public TilePreviewDrawable PreviewDrawable { get; } = new TilePreviewDrawable();
    private RoadMap _map;
    private TileType _currentTool = TileType.RoadHorizontal;
    private ToolMode _currentMode = ToolMode.DrawRect; 
    private Point? _dragStartPoint;

    public PaletteDrawable PaletteDrawable { get; } = new PaletteDrawable();

    public MainPage()
    {
        InitializeComponent();
        
        _map = new RoadMap(20, 20);
        MapDrawable.Map = _map;
        MapDrawable.RequestRedraw = () => 
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                MapGraphicsView.Invalidate();
                PreviewGraphicsView.Invalidate();
                PaletteGraphicsView.Invalidate();
            });
        };

        PreviewDrawable.MainDrawable = MapDrawable;
        PreviewDrawable.SelectedType = _currentTool;
        PaletteDrawable.MainDrawable = MapDrawable;

#if WINDOWS
        MapGraphicsView.HandlerChanged += (s, e) =>
        {
            if (MapGraphicsView.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement nativeView)
            {
                nativeView.PointerWheelChanged += (sender, args) =>
                {
                    var pointerPoint = args.GetCurrentPoint(nativeView);
                    var mousePoint = new Point(pointerPoint.Position.X, pointerPoint.Position.Y);
                    
                    int delta = pointerPoint.Properties.MouseWheelDelta;
                    float zoomDelta = delta > 0 ? 0.15f : -0.15f;

                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        ChangeZoom(zoomDelta, mousePoint);
                    });

                    args.Handled = true;
                };
            }
        };
#endif
        
        BindingContext = this;
    }

    private void OnToolModeClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && Enum.TryParse(button.CommandParameter?.ToString(), out ToolMode mode))
        {
            _currentMode = mode;
            MapDrawable.CurrentMode = mode;

            foreach (var child in ((FlexLayout)button.Parent).Children)
            {
                if (child is Button b)
                {
                    b.BackgroundColor = b == button ? Color.FromArgb("#5E35B1") : Color.FromArgb("#333");
                }
            }
        }
    }

    private void OnMapInteractionStarted(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default) return;

        _dragStartPoint = point;

        if (_currentMode == ToolMode.Navigate)
        {
            _startOffsetX = MapDrawable.OffsetX;
            _startOffsetY = MapDrawable.OffsetY;
        }
        else
        {
            var tilePos = GetTileAtPoint(point);
            MapDrawable.SelectionStart = new Point(tilePos.x, tilePos.y);
            MapDrawable.SelectionEnd = new Point(tilePos.x, tilePos.y);

            if (_currentMode == ToolMode.Eraser)
            {
                PlaceTile(tilePos.x, tilePos.y, TileType.Empty);
            }

            MapGraphicsView.Invalidate();
        }
    }

    private void OnMapInteractionDragged(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default || _dragStartPoint == null) return;

        if (_currentMode == ToolMode.Navigate)
        {
            MapDrawable.OffsetX = _startOffsetX + (point.X - (float)_dragStartPoint.Value.X);
            MapDrawable.OffsetY = _startOffsetY + (point.Y - (float)_dragStartPoint.Value.Y);
            MapGraphicsView.Invalidate();
        }
        else
        {
            var tilePos = GetTileAtPoint(point);
            MapDrawable.SelectionEnd = new Point(tilePos.x, tilePos.y);

            if (_currentMode == ToolMode.Eraser)
            {
                PlaceTile(tilePos.x, tilePos.y, TileType.Empty);
            }

            MapGraphicsView.Invalidate();
        }
    }

    private void OnMapInteractionEnded(object? sender, TouchEventArgs e)
    {
        if (_currentMode != ToolMode.Navigate && MapDrawable.SelectionStart.HasValue && MapDrawable.SelectionEnd.HasValue)
        {
            if (_currentMode == ToolMode.Eraser)
            {
                ApplyDrawing(TileType.Empty);
            }
            else
            {
                ApplyDrawing(_currentTool);
            }
        }

        _dragStartPoint = null;
        MapDrawable.SelectionStart = null;
        MapDrawable.SelectionEnd = null;
        MapGraphicsView.Invalidate();
    }

    private void ApplyDrawing(TileType toolToUse)
    {
        if (!MapDrawable.SelectionStart.HasValue || !MapDrawable.SelectionEnd.HasValue) return;

        int x1 = (int)MapDrawable.SelectionStart.Value.X;
        int y1 = (int)MapDrawable.SelectionStart.Value.Y;
        int x2 = (int)MapDrawable.SelectionEnd.Value.X;
        int y2 = (int)MapDrawable.SelectionEnd.Value.Y;

        if (_currentMode == ToolMode.DrawRect || _currentMode == ToolMode.Eraser)
        {
            int startX = Math.Min(x1, x2);
            int startY = Math.Min(y1, y2);
            int endX = Math.Max(x1, x2);
            int endY = Math.Max(y1, y2);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    PlaceTile(x, y, toolToUse);
                }
            }
        }
        else if (_currentMode == ToolMode.DrawLine)
        {
            if (Math.Abs(x1 - x2) > Math.Abs(y1 - y2))
            {
                int startX = Math.Min(x1, x2);
                int endX = Math.Max(x1, x2);
                for (int x = startX; x <= endX; x++)
                    PlaceTile(x, y1, toolToUse);
            }
            else
            {
                int startY = Math.Min(y1, y2);
                int endY = Math.Max(y1, y2);
                for (int y = startY; y <= endY; y++)
                    PlaceTile(x1, y, toolToUse);
            }
        }
    }

    private void PlaceTile(int x, int y, TileType type)
    {
        if (x < 0 || y < 0) return;

        // Автоматическое расширение карты, если рисуем за пределами текущих границ
        if (x >= _map.Width || y >= _map.Height)
        {
            int newWidth = Math.Max(_map.Width, x + 5);
            int newHeight = Math.Max(_map.Height, y + 5);
            _map.Resize(newWidth, newHeight);
        }

        var existingTile = _map.GetTile(x, y);
        if (existingTile == null) return;

        if (existingTile.Type == type && type != TileType.Empty) return;

        _map.SetTile(x, y, type);
        
        // Автоматически обновляем соединения для текущего тайла и его соседей
        RoadAutomation.AutoUpdateNeighbors(_map, x, y);
    }

    private (int x, int y) GetTileAtPoint(Point point)
    {
        float x = (float)(point.X - MapDrawable.OffsetX) / MapDrawable.Zoom;
        float y = (float)(point.Y - MapDrawable.OffsetY) / MapDrawable.Zoom;
        return ((int)Math.Floor(x / MapDrawable.TileSize), (int)Math.Floor(y / MapDrawable.TileSize));
    }

    private void OnPaletteTapped(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default) return;

        float tileW = (float)(PaletteGraphicsView.Width / 4.0);
        float tileH = (float)(PaletteGraphicsView.Height / 3.0);
        int col = (int)(point.X / tileW);
        int row = (int)(point.Y / tileH);
        int index = row * 4 + col;

        if (index >= 0 && index < PaletteDrawable.PaletteTiles.Count)
        {
            _currentTool = PaletteDrawable.PaletteTiles[index];
            PreviewDrawable.SelectedType = _currentTool;
            PreviewGraphicsView.Invalidate();
        }
    }

    public void ChangeZoom(float delta, Point? pivot = null)
    {
        float oldZoom = MapDrawable.Zoom;
        float newZoom = oldZoom + delta;

        if (newZoom < 0.1f) newZoom = 0.1f;
        if (newZoom > 5.0f) newZoom = 5.0f;

        if (Math.Abs(oldZoom - newZoom) < 0.001f) return;

        if (pivot.HasValue)
        {
            float worldX = (float)((pivot.Value.X - MapDrawable.OffsetX) / oldZoom);
            float worldY = (float)((pivot.Value.Y - MapDrawable.OffsetY) / oldZoom);

            MapDrawable.Zoom = newZoom;

            MapDrawable.OffsetX = (float)(pivot.Value.X - worldX * newZoom);
            MapDrawable.OffsetY = (float)(pivot.Value.Y - worldY * newZoom);
        }
        else
        {
            MapDrawable.Zoom = newZoom;
        }

        MapGraphicsView.Invalidate();
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Running)
        {
            float delta = (float)(e.Scale - 1.0) * 0.5f;
            ChangeZoom(delta, e.ScaleOrigin);
        }
    }

    private void OnZoomInClicked(object? sender, EventArgs e) => ChangeZoom(0.15f);
    private void OnZoomOutClicked(object? sender, EventArgs e) => ChangeZoom(-0.15f);

    private float _startOffsetX;
    private float _startOffsetY;

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e) { }

    private void OnClearMapClicked(object? sender, EventArgs e)
    {
        _map = new RoadMap(_map.Width, _map.Height);
        MapDrawable.Map = _map;
        MapGraphicsView.Invalidate();
    }

    private async void OnSaveMapClicked(object? sender, EventArgs e)
    {
        try
        {
            string fileName = await DisplayPromptAsync("Сохранение", "Введите имя файла:", initialValue: "МояКарта", accept: "Сохранить", cancel: "Отмена");
            
            if (string.IsNullOrWhiteSpace(fileName)) 
                return;
            
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(downloadsFolder))
                downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string targetPath = Path.Combine(downloadsFolder, fileName);
            
            MapSerializer.SaveToFile(_map, targetPath);
            await ShowAlertAsync("Успех", $"Карта успешно сохранена в Загрузки:\n{targetPath}", "OK");
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
        }
    }

    private async void OnLoadMapClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions 
            { 
                PickerTitle = "Выберите файл карты (.json)",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } }
                })
            });

            if (result != null)
            {
                _map = MapSerializer.LoadFromFile(result.FullPath);
                MapDrawable.Map = _map;
                MapGraphicsView.Invalidate();
                await ShowAlertAsync("Успех", "Карта загружена", "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Ошибка", $"Ошибка при загрузке: {ex.Message}", "OK");
        }
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        try
        {
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(downloadsFolder))
                downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string targetPath = Path.Combine(downloadsFolder, "ExportedMap.png");

            if (File.Exists(targetPath))
                File.Delete(targetPath);

            await MapExporter.ExportToPng(_map, targetPath);
            
            await ShowAlertAsync("Готово", $"Изображение карты успешно сохранено в Загрузки:\n{targetPath}", "OK");
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Ошибка экспорта", ex.Message, "OK");
        }
    }

    private async void OnResizeMapClicked(object? sender, EventArgs e)
    {
        string result = await ShowActionSheetAsync("Размер карты", "Отмена", null, "30x30", "50x50", "10x10");
        if (result != null && result != "Отмена")
        {
            var parts = result.Split('x');
            int size = int.Parse(parts[0]);
            _map.Resize(size, size);
            MapGraphicsView.Invalidate();
        }
    }

    private Task ShowAlertAsync(string title, string message, string cancel) => 
        MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, cancel));

    private Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons) =>
        MainThread.InvokeOnMainThreadAsync(() => DisplayActionSheet(title, cancel, destruction, buttons));
}
