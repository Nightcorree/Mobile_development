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
        var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);

        // Add headers
        foreach (var header in request.Headers)
        {
            if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                // Some headers belong to content (like Content-Type)
                // We will handle them when setting the body
            }
        }

        // Add body
        if (!string.IsNullOrEmpty(request.Body))
        {
            var content = new StringContent(request.Body, Encoding.UTF8, request.BodyType ?? "application/json");
            httpRequest.Content = content;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClient.SendAsync(httpRequest);
            stopwatch.Stop();

            var responseModel = new ResponseModel
            {
                StatusCode = (int)response.StatusCode,
                ResponseTime = stopwatch.Elapsed,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength ?? 0
            };

            // Read body
            responseModel.Body = await response.Content.ReadAsStringAsync();

            // Read headers
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
                Body = $"Error: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
