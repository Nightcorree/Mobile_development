using RoadEditor.Core;
using Microsoft.Maui.Graphics;

namespace RoadEditor.UI;

public partial class MainPage : ContentPage
{
    public MapDrawable MapDrawable { get; } = new MapDrawable();
    public TilePreviewDrawable PreviewDrawable { get; } = new TilePreviewDrawable();
    private RoadMap _map;
    private TileType _currentTool = TileType.RoadHorizontal;

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
        
        BindingContext = this;

#if WINDOWS
        MapGraphicsView.HandlerChanged += (s, e) =>
        {
            if (MapGraphicsView.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement nativeView)
            {
                nativeView.PointerWheelChanged += (sender, args) =>
                {
                    var pointerPoint = args.GetCurrentPoint(nativeView);
                    var mousePoint = new Point(pointerPoint.Position.X, pointerPoint.Position.Y);
                    
                    // Получаем дельту прокрутки. Обычно это 120 или -120 за один шаг колесика.
                    int delta = pointerPoint.Properties.MouseWheelDelta;
                    float zoomDelta = delta > 0 ? 0.15f : -0.15f;

                    // Вызываем зум в основном потоке
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        ChangeZoom(zoomDelta, mousePoint);
                    });

                    args.Handled = true;
                };
            }
        };
#endif
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

    private async void OnMapTapped(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default) return;
        
        float x = (point.X - MapDrawable.OffsetX) / (MapDrawable.Zoom);
        float y = (point.Y - MapDrawable.OffsetY) / (MapDrawable.Zoom);

        // Используем 80f, так как мы увеличили размер в MapDrawable
        int tileX = (int)(x / 80f);
        int tileY = (int)(y / 80f);

        if (tileX >= 0 && tileX < _map.Width && tileY >= 0 && tileY < _map.Height)
        {
            var existingTile = _map.GetTile(tileX, tileY);
            
            // Если кликаем тем же инструментом по уже стоящему тайлу - удаляем его (ластик)
            if (existingTile != null && existingTile.Type == _currentTool && _currentTool != TileType.Empty)
            {
                _map.SetTile(tileX, tileY, TileType.Empty);
            }
            else
            {
                _map.SetTile(tileX, tileY, _currentTool);
            }
            
            MapGraphicsView.Invalidate();
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
            // Масштабирование относительно точки (курсора)
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

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startOffsetX = MapDrawable.OffsetX;
                _startOffsetY = MapDrawable.OffsetY;
                break;
            case GestureStatus.Running:
                MapDrawable.OffsetX = _startOffsetX + (float)e.TotalX;
                MapDrawable.OffsetY = _startOffsetY + (float)e.TotalY;
                MapGraphicsView.Invalidate();
                break;
        }
    }

    private void OnToolSelected(object? sender, EventArgs e)
    {
        if (sender is Button button && Enum.TryParse(button.CommandParameter?.ToString(), out TileType tool))
        {
            _currentTool = tool;
            PreviewDrawable.SelectedType = tool;
            PreviewGraphicsView.Invalidate();
        }
    }

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
            await DisplayAlertAsync("Успех", $"Карта успешно сохранена в Загрузки:\n{targetPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
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
                await DisplayAlertAsync("Успех", "Карта загружена", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", $"Ошибка при загрузке: {ex.Message}", "OK");
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
            
            await DisplayAlertAsync("Готово", $"Изображение карты успешно сохранено в Загрузки:\n{targetPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка экспорта", ex.Message, "OK");
        }
    }

    private async void OnResizeMapClicked(object? sender, EventArgs e)
    {
        string result = await DisplayActionSheetAsync("Размер карты", "Отмена", null, "30x30", "50x50", "10x10");
        if (result != null && result != "Отмена")
        {
            var parts = result.Split('x');
            int size = int.Parse(parts[0]);
            _map.Resize(size, size);
            MapGraphicsView.Invalidate();
        }
    }

    // Вспомогательный метод для асинхронного вызова (MAUI Page методы)
    private Task DisplayAlertAsync(string title, string message, string cancel) => 
        MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, cancel));

    private Task<string> DisplayActionSheetAsync(string title, string cancel, string destruction, params string[] buttons) =>
        MainThread.InvokeOnMainThreadAsync(() => DisplayActionSheet(title, cancel, destruction, buttons));
}
