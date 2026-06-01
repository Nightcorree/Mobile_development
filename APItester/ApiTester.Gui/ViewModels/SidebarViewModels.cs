using System.Collections.ObjectModel;
using System.Linq;
using ApiTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTester.Gui.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    public ObservableCollection<RequestViewModel> Requests { get; } = new();

    public CollectionViewModel(CollectionModel model)
    {
        _name = model.Name;
        foreach (var req in model.Requests)
        {
            Requests.Add(new RequestViewModel(req));
        }
    }

    public CollectionModel ToModel()
    {
        return new CollectionModel
        {
            Name = Name,
            Requests = Requests.Select(r => r.ToModel()).ToList()
        };
    }
}

public partial class RequestViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _method;

    [ObservableProperty]
    private string _url;

    [ObservableProperty]
    private string? _body;

    public ObservableCollection<HeaderViewModel> Headers { get; } = new();

    public RequestViewModel(RequestModel model)
    {
        _name = string.IsNullOrEmpty(model.Name) ? "Запрос без названия" : model.Name;
        _method = model.Method;
        _url = model.Url;
        _body = model.Body;
        
        foreach (var header in model.Headers)
        {
            Headers.Add(new HeaderViewModel(header.Key, header.Value));
        }
    }

    public RequestModel ToModel()
    {
        return new RequestModel
        {
            Name = Name,
            Method = Method,
            Url = Url,
            Body = Body,
            BodyType = "application/json",
            Headers = Headers.Where(h => !string.IsNullOrWhiteSpace(h.Key))
                            .ToDictionary(h => h.Key, h => h.Value)
        };
    }

    public void UpdateFrom(string url, string method, string? body, ObservableCollection<HeaderViewModel> headers)
    {
        Url = url;
        Method = method;
        Body = body;
        
        Headers.Clear();
        foreach (var h in headers)
        {
            Headers.Add(new HeaderViewModel { Key = h.Key, Value = h.Value, IsEnabled = h.IsEnabled });
        }
    }
}
