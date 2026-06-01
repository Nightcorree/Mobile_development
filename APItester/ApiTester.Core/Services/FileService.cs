using System.Text.Json;
using ApiTester.Core.Models;

namespace ApiTester.Core.Services;

public static class FileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static async Task SaveCollectionAsync(CollectionModel collection, string filePath)
    {
        var json = JsonSerializer.Serialize(collection, Options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task<CollectionModel?> LoadCollectionAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<CollectionModel>(json, Options);
    }

    public static async Task SaveEnvironmentAsync(EnvironmentModel environment, string filePath)
    {
        var json = JsonSerializer.Serialize(environment, Options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task<EnvironmentModel?> LoadEnvironmentAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<EnvironmentModel>(json, Options);
    }
}
