using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using ApiTester.Core.Models;
using ApiTester.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTester.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClientService _httpClientService;
    private const string DefaultCollectionPath = "collections.json";

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
    private EnvironmentModel? _currentEnvironment;

    public ObservableCollection<CollectionViewModel> Collections { get; } = new();
    public ObservableCollection<HeaderViewModel> Headers { get; } = new();
    public ObservableCollection<string> Methods { get; } = new() { "GET", "POST", "PUT", "DELETE" };

    public MainWindowViewModel()
    {
        _httpClientService = new HttpClientService();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        CurrentEnvironment = new EnvironmentModel
        {
            Name = "Dev",
            Variables = new() { { "baseUrl", "https://jsonplaceholder.typicode.com" } }
        };

        if (File.Exists(DefaultCollectionPath))
        {
            var loaded = await FileService.LoadCollectionAsync(DefaultCollectionPath);
            if (loaded != null)
            {
                Collections.Add(new CollectionViewModel(loaded));
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
                new RequestModel 
                { 
                    Name = "Create Post", 
                    Method = "POST", 
                    Url = "{{baseUrl}}/posts", 
                    Body = "{\"title\": \"foo\", \"body\": \"bar\", \"userId\": 1}",
                    Headers = new() { { "Content-Type", "application/json" } }
                }
            }
        };
        Collections.Add(new CollectionViewModel(sampleColl));
    }

    partial void OnSelectedRequestChanged(RequestViewModel? value)
    {
        if (value != null)
        {
            Url = value.Url;
            Method = value.Method;
            RequestBody = value.Body;
            
            Headers.Clear();
            foreach (var h in value.Headers)
            {
                Headers.Add(new HeaderViewModel { Key = h.Key, Value = h.Value, IsEnabled = h.IsEnabled });
            }
            // Ensure at least one empty row
            if (Headers.Count == 0 || !string.IsNullOrWhiteSpace(Headers.Last().Key))
            {
                Headers.Add(new HeaderViewModel());
            }
        }
    }

    [RelayCommand]
    private void AddHeader()
    {
        Headers.Add(new HeaderViewModel());
    }

    [RelayCommand]
    private void RemoveHeader(HeaderViewModel header)
    {
        Headers.Remove(header);
    }

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

        var processedRequest = VariableProcessor.ProcessRequest(rawRequest, CurrentEnvironment);

        try
        {
            var response = await _httpClientService.SendRequestAsync(processedRequest);
            ResponseText = response.Body;
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
            await SaveCollectionsAsync();
            StatusText = "Request saved to collection";
        }
        else
        {
            StatusText = "Select a request from sidebar to save changes";
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

    private async Task SaveCollectionsAsync()
    {
        if (Collections.Count > 0)
        {
            var model = Collections[0].ToModel();
            await FileService.SaveCollectionAsync(model, DefaultCollectionPath);
        }
    }
}
