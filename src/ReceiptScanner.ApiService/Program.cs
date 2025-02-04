using ReceiptScanner.Shared.Clients;
using ReceiptScanner.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Register AiApiClient with HttpClient
builder.Services.AddHttpClient<AiApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://aiservice");
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// POST endpoint to analyze receipt
app.MapPost("/api/analyze-receipt", async (AnalyzeReceiptRequest request, AiApiClient aiApiClient, ILogger<Program> logger) =>
{
    if (request.ImageBytes == null || request.ImageBytes.Length == 0)
    {
        logger.LogWarning("ImageBytes is null or empty.");
        return Results.BadRequest("ImageBytes is required.");
    }

    logger.LogInformation("Received analyze receipt request with image bytes of length: {Length}", request.ImageBytes.Length);

    try
    {
        var result = await aiApiClient.AnalyzeAsync(request.ImageBytes);

        if (result == null)
        {
            logger.LogWarning("Failed to analyze the receipt.");
            return Results.Problem("Unable to process the receipt at this time. Please try again later.");
        }

        logger.LogInformation("Analysis completed successfully. Result: {Result}", result);

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while processing the receipt.");
        return Results.Problem("An error occurred while processing the receipt. Please try again later.");
    }
});

app.MapDefaultEndpoints();

app.Run();
