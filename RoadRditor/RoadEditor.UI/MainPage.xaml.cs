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
        var view = sender as VisualElement;
        var parent = view?.Parent as FlexLayout;
        var commandParam = (sender as Button)?.CommandParameter ?? (sender as ImageButton)?.CommandParameter;

        if (view != null && parent != null && Enum.TryParse(commandParam?.ToString(), out ToolMode mode))
        {
            _currentMode = mode;
            MapDrawable.CurrentMode = mode;

            foreach (var child in parent.Children)
            {
                if (child is VisualElement ve)
                {
                    ve.BackgroundColor = ve == view ? Color.FromArgb("#5E35B1") : Color.FromArgb("#333");
                }
            }
        }
    }

    private void OnMapGraphicsViewLoaded(object? sender, EventArgs e)
    {
        CenterMap();
    }

    private void CenterMap()
    {
        if (_map == null || MapGraphicsView.Width <= 0) return;

        // Рассчитываем размеры карты в пикселях с учетом текущего масштаба
        float mapPixelWidth = _map.Width * MapDrawable.TileSize * MapDrawable.Zoom;
        float mapPixelHeight = _map.Height * MapDrawable.TileSize * MapDrawable.Zoom;

        // Центрируем: (Размер области - Размер карты) / 2
        MapDrawable.OffsetX = (float)(MapGraphicsView.Width - mapPixelWidth) / 2f;
        MapDrawable.OffsetY = (float)(MapGraphicsView.Height - mapPixelHeight) / 2f;
        
        MapGraphicsView.Invalidate();
    }

    private void OnMapInteractionStarted(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default) return;

        _dragStartPoint = point;
        MapDrawable.IsCtrlPressed = IsCtrlPressed();

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

        MapDrawable.IsCtrlPressed = IsCtrlPressed();

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
        MapDrawable.IsCtrlPressed = false;
        MapGraphicsView.Invalidate();
    }

    private void ApplyDrawing(TileType toolToUse)
    {
        if (!MapDrawable.SelectionStart.HasValue || !MapDrawable.SelectionEnd.HasValue) return;

        int x1 = (int)MapDrawable.SelectionStart.Value.X;
        int y1 = (int)MapDrawable.SelectionStart.Value.Y;
        int x2 = (int)MapDrawable.SelectionEnd.Value.X;
        int y2 = (int)MapDrawable.SelectionEnd.Value.Y;

        HashSet<(int x, int y)> affectedTiles = new();
        bool isCtrl = IsCtrlPressed();

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
                    if (isCtrl && _currentMode == ToolMode.DrawRect)
                    {
                        // Рисуем только по контуру
                        if (x == startX || x == endX || y == startY || y == endY)
                        {
                            PlaceTile(x, y, TileType.RoadHorizontal, false);
                            affectedTiles.Add((x, y));
                        }
                    }
                    else
                    {
                        PlaceTile(x, y, toolToUse, false);
                        affectedTiles.Add((x, y));
                    }
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
                {
                    PlaceTile(x, y1, toolToUse, false);
                    affectedTiles.Add((x, y1));
                }
            }
            else
            {
                int startY = Math.Min(y1, y2);
                int endY = Math.Max(y1, y2);
                for (int y = startY; y <= endY; y++)
                {
                    PlaceTile(x1, y, toolToUse, false);
                    affectedTiles.Add((x1, y));
                }
            }
        }

        // После того как все плитки расставлены, обновляем их соединения
        foreach (var (x, y) in affectedTiles)
        {
            RoadAutomation.AutoUpdateNeighbors(_map, x, y);
        }
    }

    private bool IsCtrlPressed()
    {
#if WINDOWS
        var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        return state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
#else
        return false;
#endif
    }

    private void PlaceTile(int x, int y, TileType type, bool updateNeighbors = true)
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
        
        if (updateNeighbors)
        {
            // Автоматически обновляем соединения для текущего тайла и его соседей
            RoadAutomation.AutoUpdateNeighbors(_map, x, y);
        }
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

    private void OnOpenGeneratorClicked(object? sender, EventArgs e)
    {
        GeneratorOverlay.IsVisible = true;
    }

    private void OnCancelGeneration(object? sender, EventArgs e)
    {
        GeneratorOverlay.IsVisible = false;
    }

    private async void OnGenerateRoadConfirmed(object? sender, EventArgs e)
    {
        bool wOk = int.TryParse(GenWidthEntry.Text, out int w) && w > 0;
        bool hOk = int.TryParse(GenHeightEntry.Text, out int h) && h > 0;
        bool cOk = int.TryParse(GenCrossroadsEntry.Text, out int crossroads) && crossroads >= 0;

        if (!wOk || !hOk || !cOk)
        {
            await DisplayAlert("Ошибка", "Пожалуйста, введите корректные целые положительные числа.", "OK");
            return;
        }

        // Ограничение размером видимого холста (с запасом)
        // Если холст еще не проинициализирован (0), используем 50 как дефолт
        int maxW = MapGraphicsView.Width > 0 ? (int)(MapGraphicsView.Width / MapDrawable.TileSize) * 2 : 50;
        int maxH = MapGraphicsView.Height > 0 ? (int)(MapGraphicsView.Height / MapDrawable.TileSize) * 2 : 50;

        if (w > maxW || h > maxH)
        {
            await DisplayAlert("Ошибка", $"Размер слишком велик. Для текущего холста максимум: {maxW}x{maxH}", "OK");
            return;
        }

        GeneratorOverlay.IsVisible = false;
        GenerateRandomRoad(w, h, crossroads);
    }

    private void GenerateRandomRoad(int w, int h, int crossroads)
    {
        _map = new RoadMap(w, h);
        MapDrawable.Map = _map;
        
        RoadAutomation.GenerateRandomRoad(_map, crossroads);
        
        MapGraphicsView.Invalidate();
        CenterMap();
    }
}
