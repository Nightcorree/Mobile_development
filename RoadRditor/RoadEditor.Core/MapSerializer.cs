using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace RoadEditor.Core;

public class MapData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<Tile> Tiles { get; set; }
}

public static class MapSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions 
    { 
        WriteIndented = true 
    };

    public static void SaveToFile(RoadMap map, string filePath)
    {
        var data = new MapData
        {
            Width = map.Width,
            Height = map.Height,
            Tiles = map.GetAllTiles().ToList()
        };

        string json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public static RoadMap LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Map file not found", filePath);

        string json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<MapData>(json);

        if (data == null)
            return null;

        var map = new RoadMap(data.Width, data.Height);
        foreach (var tileData in data.Tiles)
        {
            map.SetTile(tileData.X, tileData.Y, tileData.Type);
        }

        return map;
    }
}
