namespace BusinessLayer.AI;

/// <summary>
/// Creates IEmbeddingClient instances by model configuration name.
/// Supports HuggingFace-format models (multilingual-e5-base, PhoBERT, bge-m3)
/// and OpenAI-format models (text-embedding-3-small).
/// </summary>
public class EmbeddingClientFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public EmbeddingClientFactory(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    /// <summary>Returns the default (primary) embedding client registered in DI.</summary>
    public IEmbeddingClient GetDefault()
    {
        var configs = GetModelConfigs();
        if (configs.Count == 0)
        {
            return CreateHuggingFaceFromLegacyConfig();
        }

        return CreateClient(configs[0]);
    }

    /// <summary>Returns embedding clients for all configured models.</summary>
    public IReadOnlyList<IEmbeddingClient> GetAll()
    {
        var configs = GetModelConfigs();
        if (configs.Count == 0)
        {
            return [CreateHuggingFaceFromLegacyConfig()];
        }

        return configs.Select(CreateClient).ToList();
    }

    /// <summary>Returns an embedding client by its display name.</summary>
    public IEmbeddingClient? GetByName(string name)
    {
        var configs = GetModelConfigs();
        var config = configs.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return config is null ? null : CreateClient(config);
    }

    /// <summary>Returns the display names of all configured models.</summary>
    public IReadOnlyList<string> GetModelNames()
    {
        var configs = GetModelConfigs();
        if (configs.Count == 0)
        {
            var legacyModel = _configuration["HuggingFace:ModelName"] ?? "intfloat/multilingual-e5-base";
            return [legacyModel];
        }

        return configs.Select(c => c.Name).ToList();
    }

    private IReadOnlyList<EmbeddingModelConfig> GetModelConfigs()
    {
        var section = _configuration.GetSection("EmbeddingModels");
        if (!section.Exists())
        {
            return [];
        }

        return section.GetChildren()
            .Select(child => new EmbeddingModelConfig
            {
                Name = child["Name"] ?? "",
                Type = child["Type"] ?? "HuggingFace",
                ModelUrl = child["ModelUrl"] ?? "",
                ApiKeyConfig = child["ApiKeyConfig"] ?? "",
                UsePassagePrefix = bool.TryParse(child["UsePassagePrefix"], out var p) && p
            })
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();
    }

    private IEmbeddingClient CreateClient(EmbeddingModelConfig config)
    {
        var apiKey = string.IsNullOrWhiteSpace(config.ApiKeyConfig)
            ? null
            : _configuration[config.ApiKeyConfig];

        if (config.Type.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            return new OpenAiEmbeddingClient(
                httpClient,
                config.Name,
                apiKey,
                config.ModelUrl,
                _loggerFactory.CreateLogger<OpenAiEmbeddingClient>());
        }

        // Default: HuggingFace-format (works for multilingual-e5-base, PhoBERT, bge-m3)
        var hfClient = _httpClientFactory.CreateClient();
        hfClient.Timeout = TimeSpan.FromSeconds(120);
        return new ConfigurableHuggingFaceClient(
            hfClient,
            config.Name,
            config.ModelUrl,
            apiKey,
            config.UsePassagePrefix,
            _loggerFactory.CreateLogger<HuggingFaceEmbeddingClient>());
    }

    private IEmbeddingClient CreateHuggingFaceFromLegacyConfig()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        return new HuggingFaceEmbeddingClient(httpClient, _configuration, _loggerFactory.CreateLogger<HuggingFaceEmbeddingClient>());
    }

    private class EmbeddingModelConfig
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "HuggingFace";
        public string ModelUrl { get; set; } = "";
        public string ApiKeyConfig { get; set; } = "";
        public bool UsePassagePrefix { get; set; }
    }
}

/// <summary>
/// A HuggingFace-format embedding client with configurable model name and URL.
/// Used for PhoBERT-base, bge-m3, and other HuggingFace Inference API models.
/// </summary>
public class ConfigurableHuggingFaceClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly string _modelUrl;
    private readonly string? _apiKey;
    private readonly bool _usePassagePrefix;
    private readonly ILogger _logger;

    public ConfigurableHuggingFaceClient(HttpClient httpClient, string modelName, string modelUrl, string? apiKey, bool usePassagePrefix, ILogger logger)
    {
        _httpClient = httpClient;
        _modelName = modelName;
        _modelUrl = modelUrl;
        _apiKey = apiKey;
        _usePassagePrefix = usePassagePrefix;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_modelUrl);

    public string ModelName => _modelName;

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken)
    {
        var prefixed = _usePassagePrefix ? $"query: {text}" : text;
        return EmbedAsync(prefixed, cancellationToken);
    }

    public Task<float[]> EmbedPassageAsync(string text, CancellationToken cancellationToken)
    {
        var prefixed = _usePassagePrefix ? $"passage: {text}" : text;
        return EmbedAsync(prefixed, cancellationToken);
    }

    private async Task<float[]> EmbedAsync(string input, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException($"Embedding model '{_modelName}' is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _modelUrl)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new
            {
                inputs = input,
                options = new { wait_for_model = true }
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("{Model} embedding API returned {StatusCode}: {Body}", _modelName, response.StatusCode, json);
            throw new InvalidOperationException($"{_modelName} embedding API returned status {(int)response.StatusCode}.");
        }

        using var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;

        var vector = ReadVector(root);
        if (vector.Length == 0)
        {
            throw new InvalidOperationException($"{_modelName} embedding API did not return a vector.");
        }

        return Normalize(vector);
    }

    private static float[] ReadVector(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var first = true;
            var allNumbers = true;
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != System.Text.Json.JsonValueKind.Number)
                {
                    allNumbers = false;
                    break;
                }
                first = false;
            }

            if (!first && allNumbers)
            {
                return element.EnumerateArray().Select(x => x.GetSingle()).ToArray();
            }

            // Nested array - take first sub-array
            foreach (var child in element.EnumerateArray())
            {
                var result = ReadVector(child);
                if (result.Length > 0)
                {
                    return result;
                }
            }
        }

        return [];
    }

    private static float[] Normalize(float[] vector)
    {
        var magnitude = Math.Sqrt(vector.Sum(x => (double)x * x));
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
