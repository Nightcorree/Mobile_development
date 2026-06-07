using System.Collections.Generic;

namespace RoadEditor.Core;

public class RoadMap
{
    private Tile[,] _tiles;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public RoadMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];
        InitializeEmpty();
    }

    private void InitializeEmpty()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_tiles[x, y] == null)
                {
                    _tiles[x, y] = new Tile(x, y, TileType.Empty);
                }
            }
        }
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        
        return _tiles[x, y];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            _tiles[x, y].Type = type;
        }
    }

    public void Resize(int newWidth, int newHeight)
    {
        var newTiles = new Tile[newWidth, newHeight];
        
        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                if (x < Width && y < Height)
                {
                    newTiles[x, y] = _tiles[x, y];
                }
                else
                {
                    newTiles[x, y] = new Tile(x, y, TileType.Empty);
                }
            }
        }

        _tiles = newTiles;
        Width = newWidth;
        Height = newHeight;
    }

    public void ShiftAndResize(int newWidth, int newHeight, int shiftX, int shiftY)
    {
        var newTiles = new Tile[newWidth, newHeight];

        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                int oldX = x - shiftX;
                int oldY = y - shiftY;

                if (oldX >= 0 && oldX < Width && oldY >= 0 && oldY < Height)
                {
                    newTiles[x, y] = _tiles[oldX, oldY];
                    newTiles[x, y].X = x;
                    newTiles[x, y].Y = y;
                }
                else
                {
                    newTiles[x, y] = new Tile(x, y, TileType.Empty);
                }
            }
        }

        _tiles = newTiles;
        Width = newWidth;
        Height = newHeight;
    }

    // Метод для удобного получения всех тайлов (например, для сериализации)
    public IEnumerable<Tile> GetAllTiles()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                yield return _tiles[x, y];
            }
        }
    }
}
