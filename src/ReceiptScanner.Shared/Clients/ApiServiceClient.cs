using Microsoft.Extensions.Logging;
using ReceiptScanner.Shared.Models;
using System.Net.Http.Json;

namespace ReceiptScanner.Shared.Clients
{
    public class ApiServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiServiceClient> _logger;

        public ApiServiceClient(HttpClient httpClient, ILogger<ApiServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ReceiptAnalyzeResult?> AnalyzeReceiptAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("ImageBytes is null or empty.");
                return null;
            }

            _logger.LogInformation("Sending analyze receipt request with image bytes of length: {Length}", imageBytes.Length);

            var request = new AnalyzeReceiptRequest
            {
                ImageBytes = imageBytes
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/analyze-receipt", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to analyze receipt. StatusCode: {StatusCode}", response.StatusCode);
                    return null;
                }

                var analyzeResult = await response.Content.ReadFromJsonAsync<ReceiptAnalyzeResult>(cancellationToken: cancellationToken);

                if (analyzeResult == null)
                {
                    _logger.LogWarning("No content received from ApiService.");
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
