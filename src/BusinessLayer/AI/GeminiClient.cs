using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.AI;

public class GeminiClient : IGeminiClient
{
    private const int MaxAttempts = 4;
    private static readonly Regex RetryDelayPattern = new(
        @"^(?<seconds>\d+(?:\.\d+)?)s$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string? _apiKey;
    private readonly string _model;

    public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"];
        _model = configuration["Gemini:Model"]?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(_model))
        {
            throw new InvalidOperationException("Missing Gemini:Model configuration.");
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string> GenerateAsync(string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
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

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

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
                        "Gemini request timed out; retrying in {DelaySeconds:F1} seconds ({Attempt}/{MaxAttempts}).",
                        delay.TotalSeconds,
                        attempt,
                        MaxAttempts);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException("Gemini API request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < MaxAttempts)
                {
                    var delay = GetBackoffDelay(attempt);
                    _logger.LogWarning(
                        ex,
                        "Gemini request failed at the network layer; retrying in {DelaySeconds:F1} seconds ({Attempt}/{MaxAttempts}).",
                        delay.TotalSeconds,
                        attempt,
                        MaxAttempts);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                throw new InvalidOperationException("Gemini API is unavailable.", ex);
            }

            using (response)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < MaxAttempts && IsTransient(response.StatusCode))
                    {
                        var delay = GetRetryDelay(response, json, attempt);
                        _logger.LogWarning(
                            "Gemini returned transient status {StatusCode}; retrying in {DelaySeconds:F1} seconds ({Attempt}/{MaxAttempts}).",
                            response.StatusCode,
                            delay.TotalSeconds,
                            attempt,
                            MaxAttempts);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Gemini returned status {StatusCode}.", response.StatusCode);
                    throw new InvalidOperationException($"Gemini API returned status {(int)response.StatusCode}.");
                }

                return ReadGeneratedText(json);
            }
        }

        throw new InvalidOperationException("Gemini API request failed after retrying.");
    }

    private static string ReadGeneratedText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
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
            if (candidate.TryGetProperty("finishReason", out var reason))
            {
                finishReason = $" (finishReason: {reason.GetString()})";
            }

            throw new InvalidOperationException($"Gemini candidate has no content{finishReason}.");
        }

        string? text = null;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textPart))
            {
                text = textPart.GetString();
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

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || code >= 500;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, string json, int attempt)
    {
        var delay = response.Headers.RetryAfter?.Delta ?? ReadRetryDelay(json);
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

    private static TimeSpan? ReadRetryDelay(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("error", out var error) ||
                !error.TryGetProperty("details", out var details) ||
                details.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var detail in details.EnumerateArray())
            {
                if (!detail.TryGetProperty("retryDelay", out var retryDelay))
                {
                    continue;
                }

                var match = RetryDelayPattern.Match(retryDelay.GetString() ?? "");
                if (match.Success && double.TryParse(
                        match.Groups["seconds"].Value,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out var seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
