using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLayer.AI;

public class HuggingFaceEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HuggingFaceEmbeddingClient> _logger;

    public HuggingFaceEmbeddingClient(HttpClient httpClient, IConfiguration configuration, ILogger<HuggingFaceEmbeddingClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ModelUrl);

    public string ModelName => _configuration["HuggingFace:ModelName"] ?? ModelNameFromUrl;

    private string? ApiKey => _configuration["HuggingFace:ApiKey"];

    private string? ModelUrl => _configuration["HuggingFace:ModelUrl"];

    private string ModelNameFromUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModelUrl))
            {
                return "huggingface-embedding";
            }

            var marker = "/models/";
            var index = ModelUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? ModelUrl[(index + marker.Length)..].Trim('/') : ModelUrl.TrimEnd('/').Split('/').Last();
        }
    }

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync($"query: {text}", cancellationToken);
    }

    public Task<float[]> EmbedPassageAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedAsync($"passage: {text}", cancellationToken);
    }

    private async Task<float[]> EmbedAsync(string input, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Hugging Face embedding API is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ModelUrl)
        {
            Content = JsonContent.Create(new
            {
                inputs = input,
                options = new
                {
                    wait_for_model = true
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Hugging Face embedding API request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Hugging Face embedding API is unavailable.", ex);
        }

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Hugging Face embedding API returned status {StatusCode}.", response.StatusCode);
                throw new InvalidOperationException($"Hugging Face embedding API returned status {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error))
            {
                throw new InvalidOperationException($"Hugging Face embedding API returned an error: {error.GetString()}");
            }

            var vector = ReadVector(root);
            if (vector.Length == 0)
            {
                throw new InvalidOperationException("Hugging Face embedding API did not return a vector.");
            }

            return vector;
        }
    }

    private static float[] ReadVector(JsonElement element)
    {
        if (TryReadNumberArray(element, out var directVector))
        {
            return Normalize(directVector);
        }

        var vectors = new List<float[]>();
        CollectVectors(element, vectors);
        if (vectors.Count == 0)
        {
            return [];
        }

        return Normalize(MeanPool(vectors));
    }

    private static void CollectVectors(JsonElement element, List<float[]> vectors)
    {
        if (TryReadNumberArray(element, out var vector))
        {
            vectors.Add(vector);
            return;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var child in element.EnumerateArray())
        {
            CollectVectors(child, vectors);
        }
    }

    private static bool TryReadNumberArray(JsonElement element, out float[] vector)
    {
        vector = [];
        if (element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var values = new List<float>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetSingle(out var value))
            {
                return false;
            }

            values.Add(value);
        }

        vector = values.ToArray();
        return vector.Length > 0;
    }

    private static float[] MeanPool(IReadOnlyList<float[]> vectors)
    {
        var dimensions = vectors.Min(x => x.Length);
        var pooled = new float[dimensions];
        foreach (var vector in vectors)
        {
            for (var i = 0; i < dimensions; i++)
            {
                pooled[i] += vector[i];
            }
        }

        for (var i = 0; i < dimensions; i++)
        {
            pooled[i] /= vectors.Count;
        }

        return pooled;
    }

    private static float[] Normalize(float[] vector)
    {
        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude <= 0)
        {
            return vector;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / magnitude);
        }

        return vector;
    }
}
