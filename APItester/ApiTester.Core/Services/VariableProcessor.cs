using System.Text.RegularExpressions;
using ApiTester.Core.Models;

namespace ApiTester.Core.Services;

public static class VariableProcessor
{
    private static readonly Regex VariableRegex = new(@"\{\{(.+?)\}\}", RegexOptions.Compiled);

    public static string Process(string input, EnvironmentModel? environment)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return VariableRegex.Replace(input, match =>
        {
            var key = match.Groups[1].Value.Trim();
            
            if (key.StartsWith("$"))
            {
                return key switch
                {
                    "$guid" => Guid.NewGuid().ToString(),
                    "$timestamp" => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    "$randomInt" => new Random().Next(0, 1000).ToString(),
                    _ => match.Value
                };
            }

            if (environment == null) return match.Value;
            return environment.Variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    public static RequestModel ProcessRequest(RequestModel request, EnvironmentModel? environment)
    {
        if (environment == null) return request;

        return new RequestModel
        {
            Name = Process(request.Name, environment),
            Method = request.Method,
            Url = Process(request.Url, environment),
            Body = request.Body != null ? Process(request.Body, environment) : null,
            BodyType = request.BodyType,
            Headers = request.Headers.ToDictionary(
                kvp => Process(kvp.Key, environment),
                kvp => Process(kvp.Value, environment))
        };
    }
}
