using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLayer.AI;

public class OpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly string? _apiKey;
    private readonly string _apiUrl;
    private readonly ILogger<OpenAiEmbeddingClient> _logger;

    public OpenAiEmbeddingClient(HttpClient httpClient, string modelName, string? apiKey, string apiUrl, ILogger<OpenAiEmbeddingClient> logger)
    {
        _httpClient = httpClient;
        _modelName = modelName;
        _apiKey = apiKey;
        _apiUrl = apiUrl;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public string ModelName => _modelName;

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(text, cancellationToken);
    }

    public Task<float[]> EmbedPassageAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(text, cancellationToken);
    }

    private async Task<float[]> EmbedAsync(string input, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("OpenAI embedding API is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = JsonContent.Create(new
            {
                model = _modelName,
                input
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI embedding API returned {StatusCode}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"OpenAI embedding API returned status {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var dataArray = root.GetProperty("data");
        var embedding = dataArray[0].GetProperty("embedding");

        var vector = new float[embedding.GetArrayLength()];
        var index = 0;
        foreach (var value in embedding.EnumerateArray())
        {
            vector[index++] = value.GetSingle();
        }

        return vector;
    }
}
