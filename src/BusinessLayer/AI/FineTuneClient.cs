using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BusinessLayer.AI;

public class FineTuneClient : IFineTuneClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public FineTuneClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(EndpointUrl);

    private string? EndpointUrl => _configuration["FineTune:EndpointUrl"];

    private string? ApiKey => _configuration["FineTune:ApiKey"];

    public async Task<FineTuneResponse> GenerateAsync(FineTuneRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Fine-tuned endpoint is not configured.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUrl)
        {
            Content = JsonContent.Create(request)
        };

        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Fine-tuned endpoint returned status {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<FineTuneResponse>(cancellationToken: cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Answer))
        {
            throw new InvalidOperationException("Fine-tuned endpoint did not return an answer.");
        }

        return result;
    }
}
