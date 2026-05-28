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
    }

    private void OnPaletteTapped(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default) return;

        float tileSize = (float)(PaletteGraphicsView.Width / 3.0);
        int col = (int)(point.X / tileSize);
        int row = (int)(point.Y / tileSize);
        int index = row * 3 + col;

        if (index >= 0 && index < PaletteDrawable.PaletteTiles.Count)
        {
            _currentTool = PaletteDrawable.PaletteTiles[index];
            PreviewDrawable.SelectedType = _currentTool;
            PreviewGraphicsView.Invalidate();
        }
    }

    private void OnFillClicked(object? sender, EventArgs e)
    {
        for (int x = 0; x < _map.Width; x++)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                if (_map.GetTile(x, y)?.Type == TileType.Empty)
                {
                    // В качестве камней используем логику фона.
                    // Если пользователь хочет прям "залить" камнем, нам нужен отдельный тип TileType.Stone
                    // Пока для визуального сходства с эталоном оставим так или добавим авто-генерацию.
                    // Для простоты сделаем все пустые клетки Horizontal (чтоб показать работу заливки),
                    // Но правильнее добавить TileType.StoneBackground
                }
            }
        }
        MapGraphicsView.Invalidate();
    }

    private async void OnMapTapped(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        if (point == default) return;
        
        float x = (point.X - MapDrawable.OffsetX) / (MapDrawable.Zoom);
        float y = (point.Y - MapDrawable.OffsetY) / (MapDrawable.Zoom);

        int tileX = (int)(x / MapDrawable.TileSize);
        int tileY = (int)(y / MapDrawable.TileSize);

        if (tileX >= 0 && tileX < _map.Width && tileY >= 0 && tileY < _map.Height)
        {
            _map.SetTile(tileX, tileY, _currentTool);
            RoadAutomation.AutoUpdateNeighbors(_map, tileX, tileY);
            MapGraphicsView.Invalidate();
        }
    }

    public void ChangeZoom(float delta)
    {
        float newZoom = MapDrawable.Zoom + delta;
        if (newZoom > 0.1f && newZoom < 5.0f)
        {
            MapDrawable.Zoom = newZoom;
            MapGraphicsView.Invalidate();
        }
    }

    private void OnZoomInClicked(object? sender, EventArgs e) => ChangeZoom(0.1f);
    private void OnZoomOutClicked(object? sender, EventArgs e) => ChangeZoom(-0.1f);

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

    private string GetToolName(TileType type) => type switch
    {
        TileType.Empty => "Ластик",
        TileType.RoadHorizontal => "Дорога",
        TileType.Crossroad => "Перекресток",
        _ => "Дорога"
    };

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
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Выберите файл для сохранения (перезаписи)" });
            
            if (result != null)
            {
                MapSerializer.SaveToFile(_map, result.FullPath);
                await DisplayAlert("Успех", $"Карта сохранена в: {result.FullPath}", "OK");
            }
        }
        catch (Exception)
        {
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, "road_map.json");
            MapSerializer.SaveToFile(_map, targetFile);
            await DisplayAlert("Инфо", $"Сохранено в стандартную папку: {targetFile}", "OK");
        }
    }

    private async void OnLoadMapClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Выберите файл карты" });
            if (result != null)
            {
                _map = MapSerializer.LoadFromFile(result.FullPath);
                MapDrawable.Map = _map;
                MapGraphicsView.Invalidate();
                await DisplayAlert("Успех", "Карта загружена", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Экспорт", "Функция экспорта будет доступна в следующем обновлении.", "OK");
    }

    private async void OnResizeMapClicked(object? sender, EventArgs e)
    {
        string result = await DisplayActionSheet("Размер карты", "Отмена", null, "30x30", "50x50", "10x10");
        if (result != null && result != "Отмена")
        {
            var parts = result.Split('x');
            int size = int.Parse(parts[0]);
            _map.Resize(size, size);
            MapGraphicsView.Invalidate();
        }
    }
}
