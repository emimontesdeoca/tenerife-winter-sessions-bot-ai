using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ReceiptScanner.Shared;
using ReceiptScanner.Shared.Models;
using System.Text.Json;

// Builder
var builder = WebApplication.CreateBuilder(args);
var kernelBuilder = Kernel.CreateBuilder();

var deploymentName = builder.Configuration["OpenAI:DeploymentName"];
var endpoint = builder.Configuration["OpenAI:Endpoint"];
var apiKey = builder.Configuration["OpenAI:ApiKey"];
var prompt = builder.Configuration["OpenAI:Prompt"];

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add OpenAI
kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName!, endpoint!, apiKey!);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Logger;
var kernel = kernelBuilder.Build();

// build chat
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// POST endpoint to analyze receipt
app.MapPost("/ai/analyze-receipt", async (AnalyzeReceiptRequest request, ILogger<Program> logger) =>
{
    if (request.ImageBytes == null || request.ImageBytes.Length == 0)
    {
        logger.LogWarning("No image bytes received in the request.");
        return Results.BadRequest("ImageBytes is required.");
    }

    logger.LogInformation("Received analyze receipt request with image bytes of length: {Length}", request.ImageBytes.Length);

    var history = new ChatHistory();
    history.AddSystemMessage("You are a helpful assistant trained to extract structured data from receipt images.");

    var prompt = "The image provided is a store receipt. Please extract the following information in a structured JSON format: 'Store', 'Date', 'Items' (array of 'Name' and 'Price'), and 'Total'. Focus on accuracy and leave unknown fields as null. Return only the JSON object.";

    ImageContent imageContent = new(new BinaryData(request.ImageBytes), "image/jpeg");

    var collectionItems = new ChatMessageContentItemCollection
    {
        new TextContent(prompt),
        new ImageContent (new BinaryData(request.ImageBytes), "image/jpeg")
    };

    history.AddUserMessage(collectionItems);

    logger.LogInformation("Chat history created: {ChatHistory}", JsonSerializer.Serialize(history));

    var result = await chatCompletionService.GetChatMessageContentsAsync(history);

    var content = result[^1].Content!
        .Replace("```json", "")
        .Replace("```", "");

    if (content == null)
    {
        logger.LogWarning("No content received from chatCompletionService.");
        return Results.Ok(new ReceiptAnalyzeResult());
    }

    logger.LogInformation("Content received: {Content}", content);

    try
    {
        var analyzeResult = new ReceiptAnalyzeResult
        {
            CreatedAt = DateTime.UtcNow,
            Result = JsonSerializer.Deserialize<ReceiptData>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
        };

        logger.LogInformation("Analysis result created: {AnalyzeResult}", JsonSerializer.Serialize(analyzeResult));

        return Results.Ok(analyzeResult);
    }
    catch (Exception e)
    {
        var errorResult = new ReceiptAnalyzeResult();
        return Results.Ok(errorResult);
    }
});
app.MapDefaultEndpoints();

app.Run();
