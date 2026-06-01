namespace ApiTester.Core.Models;

public class EnvironmentModel
{
    public string Name { get; set; } = "Default";
    public Dictionary<string, string> Variables { get; set; } = new();
}
