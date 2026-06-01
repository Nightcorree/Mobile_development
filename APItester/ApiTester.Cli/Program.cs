using System.CommandLine;
using ApiTester.Core.Models;
using ApiTester.Core.Services;

namespace ApiTester.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo>(
            "--run",
            "Path to the collection JSON file to run.")
        {
            IsRequired = true
        };

        var rootCommand = new RootCommand("ApiTester CLI - Run API collections from the command line.");
        rootCommand.AddOption(fileOption);

        rootCommand.SetHandler(async (file) =>
        {
            await RunCollectionAsync(file!);
        }, fileOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunCollectionAsync(FileInfo file)
    {
        if (!file.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: File not found at {file.FullName}");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"Loading collection from {file.Name}...");
        var collection = await FileService.LoadCollectionAsync(file.FullName);

        if (collection == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Failed to load or parse collection.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"\nRunning collection: {collection.Name}");
        Console.WriteLine(new string('-', 40));

        using var client = new HttpClientService();
        int successCount = 0;

        foreach (var request in collection.Requests)
        {
            Console.Write($"[{request.Method}] {request.Url} ... ");

            var response = await client.SendRequestAsync(request);

            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"SUCCESS ({response.StatusCode})");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"FAILED ({response.StatusCode})");
            }
            Console.ResetColor();

            Console.WriteLine($" in {response.ResponseTime.TotalMilliseconds:F0}ms");

            if (response.StatusCode >= 200 && response.StatusCode < 300) successCount++;
        }

        Console.WriteLine(new string('-', 40));
        Console.WriteLine($"Finished: {successCount}/{collection.Requests.Count} successful.");
    }
}
