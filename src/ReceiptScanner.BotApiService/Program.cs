using ReceiptScanner.Shared.Clients;
using ReceiptScanner.Shared.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Register AiApiClient with HttpClient
builder.Services.AddHttpClient<AiApiClient>(client =>
{
    client.BaseAddress = new Uri("https+http://aiservice");
});

var TOKEN = builder.Configuration["Telegram:Token"];

var app = builder.Build();

// Resolve AiApiClient from DI
var aiApiClient = app.Services.GetRequiredService<AiApiClient>();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

using var cts = new CancellationTokenSource();

var bot = new TelegramBotClient(TOKEN, cancellationToken: cts.Token);
var me = await bot.GetMe();

var items = new List<ItemData>();
decimal? total = 0;

bot.OnMessage += OnMessage;

app.Run();

async Task OnMessage(Message msg, UpdateType type)
{
    if (msg.Text is not null)
    {
        switch (msg.Text.ToLower())
        {
            case "/total":
                await bot.SendMessage(msg.Chat, $"Total right now is: {total}");

                break;
            case "/list":
                if (items.Count > 0)
                {
                    await bot.SendMessage(msg.Chat, $"Listing your purchases:");
                    foreach (var item in items)
                    {
                        await bot.SendMessage(msg.Chat, $"{item.Name} - {item.Price}");
                    }
                }
                else
                {
                    await bot.SendMessage(msg.Chat, $"No items in the list");
                }
                break;
            case "/reset":
                items.Clear();
                await bot.SendMessage(msg.Chat, $"Reset done!");
                break;
            default:
                await bot.SendMessage(msg.Chat, $"Command not found, use: /total, /list or /reset");
                break;
        }
    }
    else
    {

        Console.WriteLine($"Received {type} '{msg.Text}' in {msg.Chat}");
        await bot.SendMessage(msg.Chat, $"Processing the image right now, I'll be back in a few!");

        var fileId = msg.Photo.Last().FileId;
        var fileInfo = await bot.GetFile(fileId);
        var filePath = fileInfo.FilePath;

        var randomGuid = Guid.NewGuid().ToString();
        var fullNewPath = Path.Combine(Directory.GetCurrentDirectory(), randomGuid);

        await using Stream fileStream = File.Create(fullNewPath);
        await bot.DownloadFile(filePath, fileStream);

        fileStream.Close();

        var imageBytes = File.ReadAllBytes(fullNewPath);

        var result = await aiApiClient.AnalyzeAsync(imageBytes);

        if (result is not null)
        {
            try
            {
                await bot.SendMessage(msg.Chat, $"We have added this ticket with a total amount of '{result.Result.Total}' with the following items:");
                total += result.Result.Total;

                foreach (var item in result.Result.Items)
                {
                    items.Add(item);
                    await bot.SendMessage(msg.Chat, $"{item.Name} - {item.Price}");
                }
            }
            catch (Exception e)
            {
                await bot.SendMessage(msg.Chat, $"There was an error on this one, sorry :)");
            }
        }

        File.Delete(fullNewPath);

        await bot.SendMessage(msg.Chat, $"Process finished for this image");
    }
}