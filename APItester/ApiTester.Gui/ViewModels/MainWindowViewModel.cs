using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ApiTester.Core.Models;
using ApiTester.Core.Services;
using ApiTester.Gui.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTester.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClientService _httpClientService;
    private const string DefaultCollectionPath = "collections.json";
    private const string DefaultEnvironmentsPath = "environments.json";
    private const string DefaultHistoryPath = "history.json";

    [ObservableProperty]
    private string _requestName = "Новый запрос";

    [ObservableProperty]
    private string _url = "{{baseUrl}}/posts/1";

    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string? _requestBody;

    [ObservableProperty]
    private string? _responseText;

    [ObservableProperty]
    private string _statusText = "Готов";

    [ObservableProperty]
    private int? _statusCode;

    [ObservableProperty]
    private string? _responseSize;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private object? _selectedItem;

    [ObservableProperty]
    private RequestViewModel? _selectedRequest;

    [ObservableProperty]
    private EnvironmentViewModel? _currentEnvironment;

    public ObservableCollection<CollectionViewModel> Collections { get; } = new();
    public ObservableCollection<HistoryItemViewModel> History { get; } = new();

    public static FuncValueConverter<int?, IBrush> StatusToColorConverter { get; } = new(status =>
    {
        if (status == null) return Brushes.Transparent;
        if (status >= 200 && status < 300) return Brushes.Green;
        if (status >= 400) return Brushes.Red;
        return Brushes.Orange;
    });

    public ObservableCollection<HeaderViewModel> Headers { get; } = new();
    public ObservableCollection<EnvironmentViewModel> Environments { get; } = new();
    public ObservableCollection<string> Methods { get; } = new() { "GET", "POST", "PUT", "DELETE" };

    public MainWindowViewModel()
    {
        _httpClientService = new HttpClientService();
        CollectionViewModel.RequestSave = () => _ = SaveCollectionsAsync();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (File.Exists(DefaultEnvironmentsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(DefaultEnvironmentsPath);
                var envModels = JsonSerializer.Deserialize<List<EnvironmentModel>>(json);
                if (envModels != null)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        foreach (var m in envModels) Environments.Add(new EnvironmentViewModel(m));
                    });
                }
            }
            catch { }
        }

        if (Environments.Count == 0)
        {
            var defaultEnv = new EnvironmentModel { Name = "Без окружения", Variables = new() };
            Environments.Add(new EnvironmentViewModel(defaultEnv));
            await SaveEnvironmentsAsync();
        }

        CurrentEnvironment = Environments.First();

        if (File.Exists(DefaultCollectionPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(DefaultCollectionPath);
                var models = JsonSerializer.Deserialize<List<CollectionModel>>(json);
                if (models != null)
                {
                    foreach (var m in models) Collections.Add(new CollectionViewModel(m));
                }
            }
            catch { }
        }

        if (Collections.Count == 0)
        {
            LoadDefaultData();
            await SaveCollectionsAsync();
        }

        // Загрузка истории
        if (File.Exists(DefaultHistoryPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(DefaultHistoryPath);
                var historyModels = JsonSerializer.Deserialize<List<HistoryItemData>>(json);
                if (historyModels != null)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        foreach (var data in historyModels)
                        {
                            var item = new HistoryItemViewModel(data.Request, data.StatusCode);
                            item.Timestamp = data.Timestamp;
                            History.Add(item);
                        }
                    });
                }
            }
            catch { }
        }

        if (Collections.Count > 0 && Collections[0].Requests.Count > 0)
        {
            SelectedItem = Collections[0].Requests[0];
        }
    }

    private void LoadDefaultData()
    {
        var sampleColl = new CollectionModel 
        { 
            Name = "Пример коллекции",
            Requests = new() 
            {
                new RequestModel { Name = "Получить пост 1", Method = "GET", Url = "{{baseUrl}}/posts/1" },
                new RequestModel { Name = "Создать пост", Method = "POST", Url = "{{baseUrl}}/posts", Body = "{\"title\": \"foo\", \"body\": \"bar\", \"userId\": 1}" }
            }
        };
        Collections.Add(new CollectionViewModel(sampleColl));
    }

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is RequestViewModel req)
        {
            SelectedRequest = req;
        }
        else if (value is HistoryItemViewModel hist)
        {
            LoadFromRequestModel(hist.OriginalRequest);
        }
    }

    private void LoadFromRequestModel(RequestModel model)
    {
        RequestName = model.Name;
        Url = model.Url;
        Method = model.Method;
        RequestBody = model.Body;
        
        Headers.Clear();
        foreach (var h in model.Headers)
        {
            Headers.Add(new HeaderViewModel { Key = h.Key, Value = h.Value, IsEnabled = true });
        }
        if (Headers.Count == 0 || !string.IsNullOrWhiteSpace(Headers.Last().Key))
        {
            Headers.Add(new HeaderViewModel());
        }
    }

    partial void OnSelectedRequestChanged(RequestViewModel? value)
    {
        if (value != null)
        {
            LoadFromRequestModel(value.ToModel());
        }
    }

    partial void OnRequestNameChanged(string value)
    {
        if (SelectedRequest != null)
        {
            SelectedRequest.Name = value;
        }
    }

    [RelayCommand]
    private void AddHeader() => Headers.Add(new HeaderViewModel());

    [RelayCommand]
    private void RemoveHeader(HeaderViewModel header) => Headers.Remove(header);

    [RelayCommand]
    private async Task SendRequest()
    {
        IsLoading = true;
        StatusText = "Отправка...";
        ResponseText = string.Empty;
        StatusCode = null;
        ResponseSize = null;

        var rawRequest = new RequestModel
        {
            Name = RequestName,
            Url = Url,
            Method = Method,
            Body = RequestBody,
            BodyType = "application/json",
            Headers = Headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key))
                            .ToDictionary(h => h.Key, h => h.Value)
        };

        var processedRequest = VariableProcessor.ProcessRequest(rawRequest, CurrentEnvironment?.ToModel());

        try
        {
            var response = await _httpClientService.SendRequestAsync(processedRequest);
            StatusCode = response.StatusCode;
            
            if (response.ContentLength > 1024)
                ResponseSize = $"{(response.ContentLength / 1024.0):F2} KB";
            else
                ResponseSize = $"{response.ContentLength} B";

            if (response.ContentType?.Contains("application/json") == true && !string.IsNullOrEmpty(response.Body))
            {
                try {
                    var obj = JsonSerializer.Deserialize<JsonElement>(response.Body);
                    ResponseText = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                } catch { ResponseText = response.Body; }
            }
            else { ResponseText = response.Body; }

            StatusText = $"Время: {response.ResponseTime.TotalMilliseconds:F0}мс";

            // Добавляем в историю (в начало списка)
            var historyItem = new HistoryItemViewModel(rawRequest, response.StatusCode);
            History.Insert(0, historyItem);
            if (History.Count > 50) History.RemoveAt(History.Count - 1);
            await SaveHistoryAsync();
        }
        catch (Exception ex)
        {
            ResponseText = $"Ошибка: {ex.Message}";
            StatusText = "Ошибка";
            StatusCode = 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveCurrentRequest()
    {
        if (SelectedRequest != null)
        {
            SelectedRequest.UpdateFrom(Url, Method, RequestBody, Headers);
            SelectedRequest.Name = RequestName;
            await SaveCollectionsAsync();
            StatusText = "Успешно сохранено";
        }
    }

    [RelayCommand]
    private async Task AddRequest()
    {
        var result = await ShowInputDialog("Новый запрос", "", "Создать");
        if (string.IsNullOrWhiteSpace(result)) return;

        CollectionViewModel? targetCollection = null;

        if (SelectedItem is CollectionViewModel coll)
        {
            targetCollection = coll;
        }
        else if (SelectedItem is RequestViewModel req)
        {
            targetCollection = Collections.FirstOrDefault(c => c.Requests.Contains(req));
        }

        targetCollection ??= Collections.FirstOrDefault();

        if (targetCollection == null)
        {
            var collName = await ShowInputDialog("Новая коллекция", "Моя коллекция", "Создать");
            if (string.IsNullOrWhiteSpace(collName)) return;
            targetCollection = new CollectionViewModel(new CollectionModel { Name = collName });
            Collections.Add(targetCollection);
        }
        
        var newReq = new RequestViewModel(new RequestModel { Name = result, Method = "GET", Url = "https://" });
        targetCollection.Requests.Add(newReq);
        targetCollection.IsExpanded = true;
        
        await SaveCollectionsAsync();
        SelectedItem = newReq;
    }

    [RelayCommand]
    private async Task RenameRequest(RequestViewModel request)
    {
        var result = await ShowInputDialog("Переименовать запрос", request.Name, "Сохранить");
        if (!string.IsNullOrWhiteSpace(result))
        {
            request.Name = result;
            if (SelectedRequest == request) RequestName = result;
            await SaveCollectionsAsync();
            StatusText = "Запрос переименован";
        }
    }

    [RelayCommand]
    private async Task DuplicateRequest(RequestViewModel request)
    {
        var collection = Collections.FirstOrDefault(c => c.Requests.Contains(request));
        if (collection != null)
        {
            var model = request.ToModel();
            model.Name += " (Копия)";
            var newReq = new RequestViewModel(model);
            collection.Requests.Add(newReq);
            await SaveCollectionsAsync();
            SelectedItem = newReq;
            StatusText = "Запрос продублирован";
        }
    }

    [RelayCommand]
    private async Task DuplicateCollection(CollectionViewModel collection)
    {
        var model = collection.ToModel();
        model.Name += " (Копия)";
        var newColl = new CollectionViewModel(model);
        Collections.Add(newColl);
        await SaveCollectionsAsync();
        SelectedItem = newColl;
        StatusText = "Коллекция продублирована";
    }

    [RelayCommand]
    private async Task CopyResponse()
    {
        if (string.IsNullOrEmpty(ResponseText)) return;
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(ResponseText);
                StatusText = "Ответ скопирован в буфер обмена";
            }
        }
    }

    [RelayCommand]
    private async Task RemoveRequest(RequestViewModel request)
    {
        if (await ShowConfirmDialog($"Вы точно хотите удалить запрос \"{request.Name}\"?"))
        {
            foreach (var collection in Collections)
            {
                if (collection.Requests.Contains(request))
                {
                    collection.Requests.Remove(request);
                    if (SelectedRequest == request) SelectedItem = null;
                    await SaveCollectionsAsync();
                    StatusText = "Запрос удален";
                    break;
                }
            }
        }
    }

    [RelayCommand]
    private async Task AddCollection()
    {
        var result = await ShowInputDialog("Новая коллекция", "", "Создать");
        if (!string.IsNullOrWhiteSpace(result))
        {
            var newColl = new CollectionViewModel(new CollectionModel { Name = result });
            Collections.Add(newColl);
            await SaveCollectionsAsync();
            SelectedItem = newColl;
        }
    }

    [RelayCommand]
    private async Task RenameCollection(CollectionViewModel collection)
    {
        var result = await ShowInputDialog("Переименовать коллекцию", collection.Name, "Сохранить");
        if (!string.IsNullOrWhiteSpace(result))
        {
            collection.Name = result;
            await SaveCollectionsAsync();
            StatusText = "Коллекция переименована";
        }
    }

    [RelayCommand]
    private async Task RemoveCollection(CollectionViewModel collection)
    {
        if (await ShowConfirmDialog($"Вы точно хотите удалить коллекцию \"{collection.Name}\" и все её запросы?"))
        {
            Collections.Remove(collection);
            await SaveCollectionsAsync();
            StatusText = "Коллекция удалена";
        }
    }

    [RelayCommand]
    private async Task ClearHistory()
    {
        if (await ShowConfirmDialog("Очистить всю историю запросов?"))
        {
            History.Clear();
            await SaveHistoryAsync();
            StatusText = "История очищена";
        }
    }

    private async Task<string?> ShowInputDialog(string title, string initialValue, string confirmButtonText)
    {
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur },
            Background = Avalonia.Media.Brushes.Transparent
        };

        var control = new InputDialog();
        var titleBlock = control.FindControl<TextBlock>("TitleText");
        if (titleBlock != null) titleBlock.Text = title;

        var inputTextBox = control.FindControl<TextBox>("InputTextBox");
        if (inputTextBox != null) inputTextBox.Text = initialValue;

        var btnCancel = control.FindControl<Button>("CancelButton");
        var btnConfirm = control.FindControl<Button>("ConfirmButton");
        if (btnConfirm != null) btnConfirm.Content = confirmButtonText;

        string? result = null;
        if (btnCancel != null) btnCancel.Click += (_, _) => dialog.Close(null);
        if (btnConfirm != null) btnConfirm.Click += (_, _) => 
        {
            result = inputTextBox?.Text;
            dialog.Close(result);
        };

        dialog.Content = control;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await dialog.ShowDialog<string?>(desktop.MainWindow!);
        }

        return null;
    }

    [RelayCommand]
    private void AddVariable()
    {
        CurrentEnvironment?.Variables.Add(new EnvironmentVariableViewModel());
    }

    [RelayCommand]
    private async Task RemoveVariable(EnvironmentVariableViewModel variable)
    {
        if (CurrentEnvironment != null && await ShowConfirmDialog($"Удалить переменную \"{variable.Key}\"?"))
        {
            CurrentEnvironment.Variables.Remove(variable);
        }
    }

    private async Task<bool> ShowConfirmDialog(string message)
    {
        var dialog = new Window
        {
            Title = "Подтверждение",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur },
            Background = Avalonia.Media.Brushes.Transparent
        };

        var control = new ConfirmDialog();
        var msgBlock = control.FindControl<TextBlock>("MessageText");
        if (msgBlock != null) msgBlock.Text = message;

        var btnCancel = control.FindControl<Button>("CancelButton");
        var btnConfirm = control.FindControl<Button>("ConfirmButton");

        bool dialogResult = false;
        if (btnCancel != null) btnCancel.Click += (_, _) => dialog.Close(false);
        if (btnConfirm != null) btnConfirm.Click += (_, _) => dialog.Close(true);

        dialog.Content = control;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            dialogResult = await dialog.ShowDialog<bool>(desktop.MainWindow!);
        }

        return dialogResult;
    }

    public async Task SaveCollectionsAsync()
    {
        var models = Collections.Select(c => c.ToModel()).ToList();
        var json = JsonSerializer.Serialize(models, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(DefaultCollectionPath, json);
    }

    private async Task SaveEnvironmentsAsync()
    {
        var models = Environments.Select(e => e.ToModel()).ToList();
        var json = JsonSerializer.Serialize(models, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(DefaultEnvironmentsPath, json);
    }

    private async Task SaveHistoryAsync()
    {
        var data = History.Select(h => new HistoryItemData 
        { 
            Request = h.OriginalRequest, 
            StatusCode = h.StatusCode, 
            Timestamp = h.Timestamp 
        }).ToList();
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(DefaultHistoryPath, json);
    }

    [RelayCommand]
    private async Task AddEnvironment()
    {
        var result = await ShowInputDialog("Новое окружение", "", "Создать");
        if (string.IsNullOrWhiteSpace(result)) return;

        var newEnv = new EnvironmentViewModel(new EnvironmentModel { Name = result });
        Environments.Add(newEnv);
        CurrentEnvironment = newEnv;
        await SaveEnvironmentsAsync();
    }

    [RelayCommand]
    private async Task RenameEnvironment(EnvironmentViewModel environment)
    {
        var result = await ShowInputDialog("Переименовать окружение", environment.Name, "Сохранить");
        if (!string.IsNullOrWhiteSpace(result))
        {
            environment.Name = result;
            await SaveEnvironmentsAsync();
            StatusText = "Окружение переименовано";
        }
    }

    [RelayCommand]
    private async Task RemoveEnvironment(EnvironmentViewModel environment)
    {
        if (Environments.Count <= 1)
        {
            StatusText = "Нельзя удалить последнее окружение";
            return;
        }

        if (await ShowConfirmDialog($"Вы точно хотите удалить окружение \"{environment.Name}\"?"))
        {
            var isCurrent = CurrentEnvironment == environment;
            Environments.Remove(environment);
            if (isCurrent) CurrentEnvironment = Environments.First();
            await SaveEnvironmentsAsync();
            StatusText = "Окружение удалено";
        }
    }

    [RelayCommand]
    private async Task SaveEnvironments()
    {
        await SaveEnvironmentsAsync();
        StatusText = "Окружения сохранены";
    }
}

public class HistoryItemData
{
    public RequestModel Request { get; set; } = null!;
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}
