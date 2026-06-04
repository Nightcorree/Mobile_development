using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ApiTester.Core.Models;

namespace ApiTester.Core.Services;

public class HttpClientService : IDisposable
{
    private readonly HttpClient _httpClient;

    public HttpClientService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ResponseModel> SendRequestAsync(RequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return new ResponseModel
            {
                StatusCode = 0,
                Body = "Ошибка: Некорректный или пустой URL. Убедитесь, что он начинается с http:// или https://",
                ResponseTime = TimeSpan.Zero
            };
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);

        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (!string.IsNullOrEmpty(request.Body))
        {
            var content = new StringContent(request.Body, Encoding.UTF8, request.BodyType ?? "application/json");
            httpRequest.Content = content;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cts.Token);
            stopwatch.Stop();

            var responseModel = new ResponseModel
            {
                StatusCode = (int)response.StatusCode,
                ResponseTime = stopwatch.Elapsed,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength ?? 0
            };

            responseModel.Body = await response.Content.ReadAsStringAsync();

            foreach (var header in response.Headers)
            {
                responseModel.Headers[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                responseModel.Headers[header.Key] = string.Join(", ", header.Value);
            }

            return responseModel;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ResponseModel
            {
                StatusCode = 0,
                Body = $"Ошибка: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
