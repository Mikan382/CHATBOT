using System.Net.Http.Json;
using System.Text.Json;

namespace Prn222Chatbot.Web.Services.Ai;

public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    private string? ApiKey => Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _configuration["GEMINI_API_KEY"];

    private string Model => _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

    public async Task<string> GenerateAsync(string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("GEMINI_API_KEY is not configured.");
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={Uri.EscapeDataString(ApiKey!)}";
        var payload = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.9
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini returned {StatusCode}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"Gemini API returned status {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var text = root.GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini did not return any content.");
        }

        return text.Trim();
    }
}
