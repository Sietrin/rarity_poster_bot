using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices.Marshalling;
using System.Net.NetworkInformation;
using System.Dynamic;
using System.Formats.Tar;
using System.Text;
using Newtonsoft.Json.Linq;

dynamic jsonConfig = JsonConvert.DeserializeObject(System.IO.File.ReadAllText("./config.json"));
JArray array = jsonConfig.whitelistedIds;

string ACCESS_TOKEN = jsonConfig.ACCESS_TOKEN; //access token for the bot
long groupId = jsonConfig.groupId; //groupId of the group to send images to 
string channelName = jsonConfig.channelName; //channel name to send images to
long[] whitelistedIds = array.SelectTokens("$[*]").Select(token => (long)token).ToArray();

TelegramBotClient botClient = new(ACCESS_TOKEN);

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new() {
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
    ReplyKeyboardMarkup replyKeyboardMarkup = new(new[] {
        new KeyboardButton[] { "Дальше", "Назад" },
        new KeyboardButton[] { "Просто Рарити 💜", "Рарити и подруги️ 🌈" },
        new KeyboardButton[] { "Ночь 🌕", "Утро 🌅", "Зима", "Хеллоуин" },
        new KeyboardButton[] { "Post on ArtPosting" },
    })
    {
        ResizeKeyboard = true
    };
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;
    var chatResponseId = message.Chat.Id;
    Console.WriteLine($"Received a '{messageText}' message in chat {chatResponseId}.");

    //JsonSerializer.Serialize(CurlDerpi(1))
    if (messageText == "/start") {
        await botClient.SendTextMessageAsync(
            chatId: chatResponseId,
            text: "Краткий обзор: при нажатии кнопок Назад и Дальше бот присылает картинки, при нажатии кнопок ниже последняя показанная картинка отсылается в соответствующую ветку, самая нижняя запостит картинку на артпостинг, если айди пользователя будет в белом списке",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
async void postOnArtPosting() {
        foreach (long id in whitelistedIds) {
            if (id == message.From.Id) {
                Page.previousImage();
                dynamic pageData = JsonConvert.DeserializeObject(Page.page);
                int imageId = Page.imageId; 
                Console.WriteLine("Sent an art to arposting");
                await botClient.SendPhotoAsync(
                    chatId: channelName,
                    caption: $"Першоджерело: {pageData.images[imageId].source_url} \n Derpibooru: https://derpibooru.org/images/{pageData.images[imageId].id}",
                    photo: InputFile.FromUri($"{Page.getImageLink()}"),
                    //replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);
                Page.nextImage();
                return;
            }
        }
        await botClient.SendTextMessageAsync(
            chatId: chatResponseId,
            text: $"Your user id is {(message.From.Id).ToString()}, it seems you are not whitelisted",
            //caption: Page.imageId.ToString(),
            //photo: InputFile.FromUri($"{Page.getImageLink()}"),
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
    }
    async void triggerRetreat() {
        await botClient.SendPhotoAsync(
            chatId: chatResponseId,
            caption: Page.imageId.ToString(),
            photo: InputFile.FromUri($"{Page.getImageLink()}"),
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
        Page.previousImage();
}
async void triggerAdvance() {
        Console.WriteLine("Triggered advance");
        await botClient.SendPhotoAsync(
            chatId: chatResponseId,
            caption: Page.imageId.ToString(),
            photo: InputFile.FromUri($"{Page.getImageLink()}"),
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken);
        Page.nextImage();
}
switch (messageText) {
    case "Post on ArtPosting":
        postOnArtPosting();
        break;
    case "Дальше":
        triggerAdvance();
        break;
    case "Назад":
        triggerRetreat();
        break;
    case "Просто Рарити 💜":
        sendResponse(8);
        break;
    case "Рарити и подруги️ 🌈":
        sendResponse(9);
        break;
    case "Ночь 🌕":
        sendResponse(118);
        break;
    case "Утро 🌅":
        sendResponse(117);
        break; 
    case "Зима":
        sendResponse(4);
        break;
    case "Хеллоуин":
        sendResponse(6);
        break; 
} 
async void sendResponse(int thread) {
        Page.previousImage();
        await botClient.SendPhotoAsync(
           chatId: groupId,
           messageThreadId: thread,
           photo: InputFile.FromUri($"{Page.getImageLink()}"),
           replyMarkup: replyKeyboardMarkup,
           cancellationToken: cancellationToken);
        Page.nextImage();
        triggerAdvance(); //optional, most of the times there is no point in not advancing
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

public static class Page
{
    public static ulong? pageId;
    public static int imageId = 0;
    public static dynamic? page;
    public static dynamic jsonConfig = JsonConvert.DeserializeObject(System.IO.File.ReadAllText("./config.json"));
    public static string request = jsonConfig.request;
    static void getPage(ulong? pageNumber) {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"curl \\\"https://derpibooru.org/api/v1/json/search/images?q={request}&page={pageNumber}\\\"\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        page = output;
    }
    public static string getImageLink()
    {
        if (pageId == null) pageId = uint.Parse(System.IO.File.ReadAllText("./lastPage.json"));
        if (page == null) getPage(pageId);
        if (imageId > 14) nextPage();
        if (imageId < 0) previousPage();

        dynamic pageData = JsonConvert.DeserializeObject(page);
        //Console.WriteLine($"Posting {pageId}, {imageId}, nada");
        return pageData.images[imageId].representations.medium;
    }
    static void nextPage() {
        pageId++;
        imageId = 0;
        getPage(pageId);
        System.IO.File.WriteAllText("lastPage.json", pageId.ToString());
        Console.WriteLine($"Going next page {pageId}, {imageId}");
    }
    static void previousPage() {
        pageId--;
        imageId = 14;
        getPage(pageId);
        Console.WriteLine($"Going previous page {pageId}, {imageId}");
    }
    public static void nextImage() {
        Console.WriteLine($"Sending image {imageId}");
        imageId++;
    }
    public static void previousImage() {
        Console.WriteLine($"Sending image {imageId}");
        imageId--;
    }
}