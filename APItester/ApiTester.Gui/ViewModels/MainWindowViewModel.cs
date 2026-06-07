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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTester.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClientService _httpClientService;
    private const string DefaultCollectionPath = "collections.json";
    private const string DefaultEnvironmentsPath = "environments.json";

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
    public ObservableCollection<CollectionViewModel> FilteredCollections { get; } = new();
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
                    foreach (var m in envModels) Environments.Add(new EnvironmentViewModel(m));
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

        UpdateFilteredCollections();

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

    private void UpdateFilteredCollections()
    {
        FilteredCollections.Clear();
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Чтобы UI всегда видел актуальные данные из Collections
            foreach (var c in Collections) FilteredCollections.Add(c);
            return;
        }

        var query = SearchQuery.ToLower();
        foreach (var collection in Collections)
        {
            var filteredRequests = collection.Requests
                .Where(r => r.Name.ToLower().Contains(query) || r.Url.ToLower().Contains(query))
                .ToList();

            if (collection.Name.ToLower().Contains(query) || filteredRequests.Any())
            {
                var newColl = new CollectionViewModel(new CollectionModel { Name = collection.Name });
                foreach (var r in filteredRequests) newColl.Requests.Add(r);
                FilteredCollections.Add(newColl);
            }
        }
    }

    partial void OnSearchQueryChanged(string value) => UpdateFilteredCollections();

    partial void OnSelectedItemChanged(object? value)
    {
        if (value is RequestViewModel req)
        {
            SelectedRequest = req;
        }
    }

    partial void OnSelectedRequestChanged(RequestViewModel? value)
    {
        if (value != null)
        {
            RequestName = value.Name;
            Url = value.Url;
            Method = value.Method;
            RequestBody = value.Body;
            
            Headers.Clear();
            foreach (var h in value.Headers)
            {
                Headers.Add(new HeaderViewModel { Key = h.Key, Value = h.Value, IsEnabled = h.IsEnabled });
            }
            if (Headers.Count == 0 || !string.IsNullOrWhiteSpace(Headers.Last().Key))
            {
                Headers.Add(new HeaderViewModel());
            }
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

        var rawRequest = new RequestModel
        {
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
            
            if (response.ContentType?.Contains("application/json") == true && !string.IsNullOrEmpty(response.Body))
            {
                try {
                    var obj = JsonSerializer.Deserialize<JsonElement>(response.Body);
                    ResponseText = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                } catch { ResponseText = response.Body; }
            }
            else { ResponseText = response.Body; }

            StatusText = $"Статус: {response.StatusCode} | Время: {response.ResponseTime.TotalMilliseconds:F0}мс";
        }
        catch (Exception ex)
        {
            ResponseText = $"Ошибка: {ex.Message}";
            StatusText = "Ошибка";
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
            UpdateFilteredCollections();
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
        
        // ВАЖНО: вызываем обновление только если активен поиск, 
        // иначе Avalonia может упасть при добавлении в дерево
        if (!string.IsNullOrWhiteSpace(SearchQuery)) UpdateFilteredCollections();
        
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
            if (!string.IsNullOrWhiteSpace(SearchQuery)) UpdateFilteredCollections();
            StatusText = "Запрос переименован";
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
                    if (!string.IsNullOrWhiteSpace(SearchQuery)) UpdateFilteredCollections();
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
            if (!string.IsNullOrWhiteSpace(SearchQuery)) UpdateFilteredCollections();
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
            if (!string.IsNullOrWhiteSpace(SearchQuery)) UpdateFilteredCollections();
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
            if (!string.IsNullOrWhiteSpace(SearchQuery)) UpdateFilteredCollections();
            StatusText = "Коллекция удалена";
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

    private async Task SaveCollectionsAsync()
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

    [RelayCommand]
    private async Task AddEnvironment()
    {
        var newEnv = new EnvironmentViewModel(new EnvironmentModel { Name = "Новое окружение" });
        Environments.Add(newEnv);
        CurrentEnvironment = newEnv;
        await SaveEnvironmentsAsync();
    }

    [RelayCommand]
    private async Task RemoveEnvironment()
    {
        if (CurrentEnvironment == null || Environments.Count <= 1)
        {
            StatusText = "Нельзя удалить последнее окружение";
            return;
        }

        if (await ShowConfirmDialog($"Удалить окружение \"{CurrentEnvironment.Name}\"?"))
        {
            var toRemove = CurrentEnvironment;
            Environments.Remove(toRemove);
            CurrentEnvironment = Environments.First();
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
