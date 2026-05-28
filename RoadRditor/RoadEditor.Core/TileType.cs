namespace RoadEditor.Core;

public enum TileType
{
    Empty = 0,
    
    // Прямые
    RoadHorizontal = 1,
    RoadVertical = 2,
    
    // Повороты
    TurnTopLeft = 3,
    TurnTopRight = 4,
    TurnBottomLeft = 5,
    TurnBottomRight = 6,
    
    // Перекрестки и Т-образные
    Crossroad = 7,
    TTypeUp = 8,
    TTypeDown = 9,
    TTypeLeft = 10,
    TTypeRight = 11,
    
    // Фон
    Stone = 12
}
