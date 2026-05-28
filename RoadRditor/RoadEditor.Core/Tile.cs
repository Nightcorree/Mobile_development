namespace RoadEditor.Core;

public class Tile
{
    public int X { get; set; }
    public int Y { get; set; }
    public TileType Type { get; set; }

    public Tile() { }

    public Tile(int x, int y, TileType type)
    {
        X = x;
        Y = y;
        Type = type;
    }
}
