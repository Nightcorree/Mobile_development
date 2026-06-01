using System.Collections.ObjectModel;
using System.Linq;
using ApiTester.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiTester.Gui.ViewModels;

public partial class EnvironmentViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    public ObservableCollection<EnvironmentVariableViewModel> Variables { get; } = new();

    public EnvironmentViewModel(EnvironmentModel model)
    {
        _name = model.Name;
        foreach (var variable in model.Variables)
        {
            Variables.Add(new EnvironmentVariableViewModel(variable.Key, variable.Value));
        }
    }

    public EnvironmentModel ToModel()
    {
        return new EnvironmentModel
        {
            Name = Name,
            Variables = Variables.Where(v => !string.IsNullOrWhiteSpace(v.Key))
                                .ToDictionary(v => v.Key, v => v.Value)
        };
    }
}
