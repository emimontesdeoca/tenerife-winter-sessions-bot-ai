using Microsoft.Extensions.Logging;
using ReceiptScanner.Shared.Models;
using System.Net.Http.Json;

namespace ReceiptScanner.Shared.Clients
{
    public class AiApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiApiClient> _logger;

        public AiApiClient(HttpClient httpClient, ILogger<AiApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ReceiptAnalyzeResult?> AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("ImageBytes is null or empty.");
                return null;
            }

            _logger.LogInformation("Sending analyze request with image bytes of length: {Length}", imageBytes.Length);

            var request = new AnalyzeReceiptRequest
            {
                ImageBytes = imageBytes
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/ai/analyze-receipt", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to analyze receipt. StatusCode: {StatusCode}", response.StatusCode);
                    return null;
                }

                var analyzeResult = await response.Content.ReadFromJsonAsync<ReceiptAnalyzeResult>(cancellationToken: cancellationToken);

                if (analyzeResult == null)
                {
                    _logger.LogWarning("No content received from AI API service.");
                    return null;
                }

                _logger.LogInformation("Analysis result received: {AnalyzeResult}", analyzeResult);

                return analyzeResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while analyzing the receipt.");
                return null;
            }
        }
    }
}
