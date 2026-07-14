using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.AI;

public class HuggingFaceEmbeddingClient : IEmbeddingClient
{
    private const int MaxAttempts = 4;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceEmbeddingClient> _logger;
    private readonly string? _apiKey;
    private readonly IReadOnlyDictionary<string, EmbeddingModelConfiguration> _models;

    public HuggingFaceEmbeddingClient(HttpClient httpClient, IConfiguration configuration, ILogger<HuggingFaceEmbeddingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["HuggingFace:ApiKey"];

        var configurations = LoadModelConfigurations(configuration);
        _models = configurations.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        AvailableModels = configurations.Select(x => x.Name).ToList();

        var configuredDefault = configuration["HuggingFace:ModelName"];
        ModelName = !string.IsNullOrWhiteSpace(configuredDefault) && _models.ContainsKey(configuredDefault)
            ? configuredDefault
            : AvailableModels.FirstOrDefault() ?? "huggingface-embedding";
    }

    public bool IsConfigured => IsModelConfigured(ModelName);

    public string ModelName { get; }

    public IReadOnlyList<string> AvailableModels { get; }

    public bool IsModelConfigured(string modelName)
    {
        return !string.IsNullOrWhiteSpace(_apiKey) && _models.ContainsKey(modelName);
    }

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedQueryAsync(ModelName, text, cancellationToken);
    }

    public Task<float[]> EmbedPassageAsync(string text, CancellationToken cancellationToken)
    {
        return EmbedPassageAsync(ModelName, text, cancellationToken);
    }

    public Task<float[]> EmbedQueryAsync(string modelName, string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(modelName, text, isQuery: true, cancellationToken);
    }

    public Task<float[]> EmbedPassageAsync(string modelName, string text, CancellationToken cancellationToken)
    {
        return EmbedAsync(modelName, text, isQuery: false, cancellationToken);
    }

    private async Task<float[]> EmbedAsync(
        string modelName,
        string text,
        bool isQuery,
        CancellationToken cancellationToken)
    {
        if (!IsModelConfigured(modelName) || !_models.TryGetValue(modelName, out var model))
        {
            throw new InvalidOperationException($"Hugging Face embedding model '{modelName}' is not configured.");
        }

        var prefix = isQuery ? model.QueryPrefix : model.PassagePrefix;
        var input = $"{prefix}{text}";

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, model.ModelUrl)
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < MaxAttempts)
                {
                    var delay = GetBackoffDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Hugging Face embedding request timed out; retrying in {DelaySeconds:F1} seconds ({Attempt}/{MaxAttempts}).",
                        delay.TotalSeconds,
                        attempt,
                        MaxAttempts);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException("Hugging Face embedding API request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < MaxAttempts)
                {
                    var delay = GetBackoffDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Hugging Face embedding request failed at the network layer; retrying in {DelaySeconds:F1} seconds ({Attempt}/{MaxAttempts}).",
                        delay.TotalSeconds,
                        attempt,
                        MaxAttempts);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException("Hugging Face embedding API is unavailable.", ex);
            }

            using (response)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < MaxAttempts && IsTransient(response.StatusCode))
                    {
                        var delay = GetRetryDelay(response, attempt);
                        _logger.LogWarning(
                            "Hugging Face model {ModelName} returned transient status {StatusCode}; retrying in {DelaySeconds:F1} seconds ({Attempt}/{MaxAttempts}).",
                            modelName,
                            response.StatusCode,
                            delay.TotalSeconds,
                            attempt,
                            MaxAttempts);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Hugging Face embedding API returned status {StatusCode}.", response.StatusCode);
                    throw new InvalidOperationException(
                        $"Hugging Face model '{modelName}' returned status {(int)response.StatusCode}.");
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

        throw new InvalidOperationException($"Hugging Face model '{modelName}' failed after retrying.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || code >= 500;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var delay = response.Headers.RetryAfter?.Delta;
        if (!delay.HasValue)
        {
            return GetBackoffDelay(attempt);
        }

        var cappedSeconds = Math.Min(60, Math.Max(1, delay.Value.TotalSeconds));
        return TimeSpan.FromSeconds(cappedSeconds) + TimeSpan.FromMilliseconds(Random.Shared.Next(100, 501));
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        var seconds = Math.Min(60, Math.Pow(2, attempt));
        return TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(Random.Shared.Next(100, 501));
    }

    private static IReadOnlyList<EmbeddingModelConfiguration> LoadModelConfigurations(IConfiguration configuration)
    {
        var models = new List<EmbeddingModelConfiguration>();
        foreach (var section in configuration.GetSection("HuggingFace:Models").GetChildren())
        {
            var name = section["Name"]?.Trim();
            var modelUrl = section["ModelUrl"]?.Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(modelUrl))
            {
                continue;
            }

            models.Add(new EmbeddingModelConfiguration(
                name,
                modelUrl,
                section["QueryPrefix"] ?? "",
                section["PassagePrefix"] ?? ""));
        }

        var defaultUrl = configuration["HuggingFace:ModelUrl"]?.Trim();
        if (!string.IsNullOrWhiteSpace(defaultUrl))
        {
            var defaultName = configuration["HuggingFace:ModelName"]?.Trim();
            defaultName = string.IsNullOrWhiteSpace(defaultName) ? ModelNameFromUrl(defaultUrl) : defaultName;
            if (!models.Any(x => x.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase)))
            {
                models.Insert(0, new EmbeddingModelConfiguration(defaultName, defaultUrl, "query: ", "passage: "));
            }
        }

        return models
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static string ModelNameFromUrl(string modelUrl)
    {
        var marker = "/models/";
        var index = modelUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return modelUrl.TrimEnd('/').Split('/').Last();
        }

        var name = modelUrl[(index + marker.Length)..];
        var pipelineMarker = "/pipeline/";
        var pipelineIndex = name.IndexOf(pipelineMarker, StringComparison.OrdinalIgnoreCase);
        return (pipelineIndex >= 0 ? name[..pipelineIndex] : name).Trim('/');
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

    private sealed record EmbeddingModelConfiguration(
        string Name,
        string ModelUrl,
        string QueryPrefix,
        string PassagePrefix);
}
