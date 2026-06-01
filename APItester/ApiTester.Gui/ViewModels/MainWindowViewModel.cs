using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ApiTester.Core.Models;
using ApiTester.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiTester.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClientService _httpClientService;

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
    public ObservableCollection<string> Methods { get; } = new() { "GET", "POST", "PUT", "DELETE" };

    public MainWindowViewModel()
    {
        _httpClientService = new HttpClientService();
        LoadDefaultData();
    }

    private void LoadDefaultData()
    {
        // Sample Environment
        CurrentEnvironment = new EnvironmentModel
        {
            Name = "Dev",
            Variables = new() { { "baseUrl", "https://jsonplaceholder.typicode.com" } }
        };

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
    }

    partial void OnSelectedRequestChanged(RequestViewModel? value)
    {
        if (value != null)
        {
            Url = value.Url;
            Method = value.Method;
            RequestBody = value.Body;
        }
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
            BodyType = "application/json"
        };

        // Process variables
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
    private void AddRequest()
    {
        if (Collections.Count == 0)
        {
            Collections.Add(new CollectionViewModel(new CollectionModel { Name = "New Collection" }));
        }
        
        var newReq = new RequestViewModel(new RequestModel { Name = "New Request", Method = "GET", Url = "https://" });
        Collections[0].Requests.Add(newReq);
        SelectedRequest = newReq;
    }
}
