using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.AI;

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

    private string? ApiKey => _configuration["Gemini:ApiKey"];

    private string Model => _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

    public async Task<string> GenerateAsync(string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent";
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

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("x-goog-api-key", ApiKey);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Gemini API request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Gemini API is unavailable.", ex);
        }

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini returned status {StatusCode}.", response.StatusCode);
                throw new InvalidOperationException($"Gemini API returned status {(int)response.StatusCode}.");
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                // Gemini may return promptFeedback with blockReason instead of candidates
                var blockReason = "";
                if (root.TryGetProperty("promptFeedback", out var feedback) &&
                    feedback.TryGetProperty("blockReason", out var reason))
                {
                    blockReason = $" (blocked: {reason.GetString()})";
                }

                throw new InvalidOperationException($"Gemini did not return any candidates{blockReason}.");
            }

            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                var finishReason = "";
                if (candidate.TryGetProperty("finishReason", out var fr))
                {
                    finishReason = $" (finishReason: {fr.GetString()})";
                }

                throw new InvalidOperationException($"Gemini candidate has no content{finishReason}.");
            }

            // Find the first text part (skip thinking parts)
            string? text = null;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var t))
                {
                    text = t.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Gemini did not return any text content.");
            }

            return text.Trim();
        }
    }
}
