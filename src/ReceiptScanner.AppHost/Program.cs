var builder = DistributedApplication.CreateBuilder(args);

var aiService = builder.AddProject<Projects.ReceiptScanner_AiService>("aiservice");

var apiService = builder.AddProject<Projects.ReceiptScanner_ApiService>("apiservice")
    .WithReference(aiService)
    .WaitFor(aiService);

var botApiService = builder.AddProject<Projects.ReceiptScanner_BotApiService>("botapiservice")
    .WithReference(aiService)
    .WaitFor(aiService);

builder.AddProject<Projects.ReceiptScanner_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
