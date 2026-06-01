namespace ApiTester.Core.Models;

public class CollectionModel
{
    public string Name { get; set; } = "New Collection";
    public List<RequestModel> Requests { get; set; } = new();
}
