using System;
using System.Collections.ObjectModel;
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
    private string _url = "https://jsonplaceholder.typicode.com/posts/1";

    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string? _requestBody;

    [ObservableProperty]
    private string? _responseText;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<string> Methods { get; } = new() { "GET", "POST", "PUT", "DELETE" };

    public MainWindowViewModel()
    {
        _httpClientService = new HttpClientService();
    }

    [RelayCommand]
    private async Task SendRequest()
    {
        StatusText = "Sending...";
        ResponseText = string.Empty;

        var request = new RequestModel
        {
            Url = Url,
            Method = Method,
            Body = RequestBody,
            BodyType = "application/json"
        };

        try
        {
            var response = await _httpClientService.SendRequestAsync(request);
            ResponseText = response.Body;
            StatusText = $"Status: {response.StatusCode} | Time: {response.ResponseTime.TotalMilliseconds:F0}ms";
        }
        catch (Exception ex)
        {
            ResponseText = $"Error: {ex.Message}";
            StatusText = "Error";
        }
    }
}
