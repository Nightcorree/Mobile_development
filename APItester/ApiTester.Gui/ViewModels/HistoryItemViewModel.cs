using System;
using ApiTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTester.Gui.ViewModels;

public partial class HistoryItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _method;

    [ObservableProperty]
    private string _url;

    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private int _statusCode;

    public RequestModel OriginalRequest { get; }

    public HistoryItemViewModel(RequestModel request, int statusCode)
    {
        OriginalRequest = request;
        Name = request.Name;
        Method = request.Method;
        Url = request.Url;
        StatusCode = statusCode;
        Timestamp = DateTime.Now;
    }
}
