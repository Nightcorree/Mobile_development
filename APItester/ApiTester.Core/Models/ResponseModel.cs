namespace ApiTester.Core.Models;

public class ResponseModel
{
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public long ContentLength { get; set; }
    public string? ContentType { get; set; }
}
