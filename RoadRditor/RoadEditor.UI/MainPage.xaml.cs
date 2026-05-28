using RoadEditor.Core;
using Microsoft.Maui.Graphics;

namespace RoadEditor.UI;

public partial class MainPage : ContentPage
{
    public MapDrawable MapDrawable { get; } = new MapDrawable();
    private RoadMap _map;
    private TileType _currentTool = TileType.RoadHorizontal;

    public MainPage()
    {
        InitializeComponent();
        
        // Инициализируем карту по умолчанию
        _map = new RoadMap(20, 20);
        MapDrawable.Map = _map;
        
        BindingContext = this;
    }

    private void OnToolSelected(object sender, EventArgs e)
    {
        if (sender is Button button && Enum.TryParse(button.CommandParameter?.ToString(), out TileType tool))
        {
            _currentTool = tool;
            StatusLabel.Text = $"Selected Tool: {tool}";
        }
    }

    private void OnMapTapped(object sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        
        // Рассчитываем координаты сетки с учетом смещения и зума
        float x = (point.X - MapDrawable.OffsetX) / MapDrawable.Zoom;
        float y = (point.Y - MapDrawable.OffsetY) / MapDrawable.Zoom;

        int tileX = (int)(x / MapDrawable.TileSize);
        int tileY = (int)(y / MapDrawable.TileSize);

        if (tileX >= 0 && tileX < _map.Width && tileY >= 0 && tileY < _map.Height)
        {
            _map.SetTile(tileX, tileY, _currentTool);
            
            // Если выбран режим рисования (не пустой тайл), применяем авто-тайлинг
            if (_currentTool != TileType.Empty)
            {
                RoadAutomation.AutoUpdateNeighbors(_map, tileX, tileY);
            }
            else
            {
                // При удалении тайла тоже нужно обновить соседей
                RoadAutomation.AutoUpdateNeighbors(_map, tileX, tileY);
            }

            MapGraphicsView.Invalidate(); // Перерисовываем
        }
    }

    private void OnPointerWheelChanged(object sender, PointerEventArgs e)
    {
        // В MAUI 8+ PointerEventArgs содержит информацию о колесике через платформенные специфики или сторонние библиотеки,
        // но мы можем реализовать простой зум через кнопки или дождаться полной поддержки.
        // Для демонстрации добавим метод изменения зума.
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

    private void OnZoomInClicked(object sender, EventArgs e) => ChangeZoom(0.1f);
    private void OnZoomOutClicked(object sender, EventArgs e) => ChangeZoom(-0.1f);

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Упрощенное перемещение (нужно хранить начальное смещение для плавности)
                MapDrawable.OffsetX += (float)e.TotalX * 0.1f; 
                MapDrawable.OffsetY += (float)e.TotalY * 0.1f;
                MapGraphicsView.Invalidate();
                break;
        }
    }

    private void OnNewMapClicked(object sender, EventArgs e)
    {
        _map = new RoadMap(20, 20);
        MapDrawable.Map = _map;
        MapGraphicsView.Invalidate();
    }

    private async void OnSaveMapClicked(object sender, EventArgs e)
    {
        try
        {
            var fileSavePicker = Microsoft.Maui.Storage.FilePicker.Default;
            string json = System.Text.Json.JsonSerializer.Serialize(new MapData
            {
                Width = _map.Width,
                Height = _map.Height,
                Tiles = _map.GetAllTiles().ToList()
            });

            // В MAUI нет прямого FileSavePicker, поэтому используем временный файл и делимся им
            // или сохраняем в известное место. Для Windows/Desktop можно использовать FolderPicker или специфичные API.
            // Упростим: сохраняем в Documents
            string fileName = "road_map.json";
            string targetFile = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            
            MapSerializer.SaveToFile(_map, targetFile);
            
            await DisplayAlert("Успех", $"Карта сохранена: {targetFile}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnLoadMapClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите файл карты",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.Android, new[] { "application/json" } }
                })
            });

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

    private async void OnExportClicked(object sender, EventArgs e)
    {
        // Для полноценного экспорта в PNG в MAUI обычно требуется SkiaSharp или платформенный код.
        // Мы можем добавить кнопку в интерфейс и вывести сообщение о подготовке.
        await DisplayAlert("Экспорт", "Функция экспорта в PNG будет доступна после интеграции SkiaSharp или использования платформенных Canvas.", "OK");
    }

    private async void OnResizeMapClicked(object sender, EventArgs e)
    {
        string result = await DisplayActionSheet("Resize Map", "Cancel", null, "30x30", "50x50", "10x10");
        if (result != null && result != "Cancel")
        {
            var parts = result.Split('x');
            int size = int.Parse(parts[0]);
            _map.Resize(size, size);
            MapGraphicsView.Invalidate();
        }
    }
}
