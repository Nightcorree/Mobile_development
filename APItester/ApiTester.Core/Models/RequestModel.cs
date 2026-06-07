namespace ApiTester.Core.Models;

public class RequestModel
{
    public string Name { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public string? BodyType { get; set; } // e.g., "application/json", "application/x-www-form-urlencoded"
}
