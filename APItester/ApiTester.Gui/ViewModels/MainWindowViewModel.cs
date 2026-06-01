using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ApiTester.Core.Models;
using ApiTester.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTester.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClientService _httpClientService;
    private const string DefaultCollectionPath = "collections.json";
    private const string DefaultEnvironmentsPath = "environments.json";

    [ObservableProperty]
    private string _requestName = "New Request";

    [ObservableProperty]
    private string _url = "{{baseUrl}}/posts/1";

    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string? _requestBody;

    [ObservableProperty]
    private string? _responseText;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private CollectionViewModel? _selectedCollection;

    [ObservableProperty]
    private RequestViewModel? _selectedRequest;

    [ObservableProperty]
    private EnvironmentViewModel? _currentEnvironment;

    public ObservableCollection<CollectionViewModel> Collections { get; } = new();
    public ObservableCollection<HeaderViewModel> Headers { get; } = new();
    public ObservableCollection<EnvironmentViewModel> Environments { get; } = new();
    public ObservableCollection<string> Methods { get; } = new() { "GET", "POST", "PUT", "DELETE" };

    public MainWindowViewModel()
    {
        _httpClientService = new HttpClientService();
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
            var defaultEnv = new EnvironmentModel { Name = "No Environment", Variables = new() };
            var devEnv = new EnvironmentModel { Name = "Dev", Variables = new() { { "baseUrl", "https://jsonplaceholder.typicode.com" } } };
            Environments.Add(new EnvironmentViewModel(defaultEnv));
            Environments.Add(new EnvironmentViewModel(devEnv));
            await SaveEnvironmentsAsync();
        }

        CurrentEnvironment = Environments.FirstOrDefault(e => e.Name != "No Environment") ?? Environments.First();

        if (File.Exists(DefaultCollectionPath))
        {
            var loaded = await FileService.LoadCollectionAsync(DefaultCollectionPath);
            if (loaded != null)
            {
                Collections.Add(new CollectionViewModel(loaded));
                if (Collections[0].Requests.Count > 0)
                {
                    SelectedRequest = Collections[0].Requests[0];
                }
            }
        }
        else
        {
            LoadDefaultData();
            await SaveCollectionsAsync();
        }
    }

    private void LoadDefaultData()
    {
        var sampleColl = new CollectionModel 
        { 
            Name = "Sample Collection",
            Requests = new() 
            {
                new RequestModel { Name = "Get Post 1", Method = "GET", Url = "{{baseUrl}}/posts/1" },
                new RequestModel { Name = "Create Post", Method = "POST", Url = "{{baseUrl}}/posts", Body = "{\"title\": \"foo\", \"body\": \"bar\", \"userId\": 1}" }
            }
        };
        Collections.Add(new CollectionViewModel(sampleColl));
        SelectedRequest = Collections[0].Requests[0];
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
        StatusText = "Sending...";
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

            StatusText = $"Status: {response.StatusCode} | Time: {response.ResponseTime.TotalMilliseconds:F0}ms";
        }
        catch (Exception ex)
        {
            ResponseText = $"Error: {ex.Message}";
            StatusText = "Error";
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
            StatusText = "Request saved to collection";
        }
    }

    [RelayCommand]
    private async Task AddRequest()
    {
        if (Collections.Count == 0)
        {
            Collections.Add(new CollectionViewModel(new CollectionModel { Name = "New Collection" }));
        }
        
        var newReq = new RequestViewModel(new RequestModel { Name = "New Request", Method = "GET", Url = "https://" });
        Collections[0].Requests.Add(newReq);
        SelectedRequest = newReq;
        await SaveCollectionsAsync();
    }

    [RelayCommand]
    private async Task RemoveRequest(RequestViewModel request)
    {
        foreach (var collection in Collections)
        {
            if (collection.Requests.Contains(request))
            {
                collection.Requests.Remove(request);
                if (SelectedRequest == request)
                {
                    SelectedRequest = collection.Requests.FirstOrDefault();
                }
                await SaveCollectionsAsync();
                StatusText = "Request deleted";
                break;
            }
        }
    }

    private async Task SaveCollectionsAsync()
    {
        if (Collections.Count > 0)
        {
            var model = Collections[0].ToModel();
            await FileService.SaveCollectionAsync(model, DefaultCollectionPath);
        }
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
        var newEnv = new EnvironmentViewModel(new EnvironmentModel { Name = "New Environment" });
        Environments.Add(newEnv);
        CurrentEnvironment = newEnv;
        await SaveEnvironmentsAsync();
    }

    [RelayCommand]
    private async Task SaveEnvironments()
    {
        await SaveEnvironmentsAsync();
        StatusText = "Environments saved";
    }
}
