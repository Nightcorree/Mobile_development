using System;
using System.IO;
using System.Linq;
using RoadEditor.Core;

namespace RoadEditor.CLI;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            ShowHelp();
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "info":
                if (args.Length < 2)
                {
                    Console.WriteLine("Ошибка: Укажите путь к файлу карты.");
                    return;
                }
                ShowMapInfo(args[1]);
                break;

            default:
                Console.WriteLine($"Неизвестная команда: {command}");
                ShowHelp();
                break;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("=== Road Editor CLI ===");
        Console.WriteLine("Использование:");
        Console.WriteLine("  RoadEditor.CLI.exe <команда> [аргументы]");
        Console.WriteLine();
        Console.WriteLine("Команды:");
        Console.WriteLine("  info <путь_к_файлу>    Выводит информацию о карте (размер, количество тайлов).");
        Console.WriteLine("  --help, -h             Показать эту справку.");
    }

    static void ShowMapInfo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Ошибка: Файл не найден по пути '{filePath}'");
            return;
        }

        try
        {
            var map = MapSerializer.LoadFromFile(filePath);
            if (map == null)
            {
                Console.WriteLine("Ошибка: Не удалось загрузить карту.");
                return;
            }

            var allTiles = map.GetAllTiles().ToList();
            int totalRoads = allTiles.Count(t => t.Type != TileType.Empty);

            Console.WriteLine("--- Информация о карте ---");
            Console.WriteLine($"Файл: {Path.GetFileName(filePath)}");
            Console.WriteLine($"Размер: {map.Width} x {map.Height}");
            Console.WriteLine($"Всего ячеек: {map.Width * map.Height}");
            Console.WriteLine($"Занято дорогами: {totalRoads}");
            
            if (totalRoads > 0)
            {
                Console.WriteLine("\nРаспределение по типам:");
                var groups = allTiles
                    .Where(t => t.Type != TileType.Empty)
                    .GroupBy(t => t.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() });

                foreach (var group in groups)
                {
                    Console.WriteLine($"  {group.Type}: {group.Count}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка при чтении файла: {ex.Message}");
        }
    }
}
