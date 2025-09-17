using Telegram.Bot;
using Telegram.Bot.Types;

namespace roshaikinbot;

static class BotExtensions
{
    public static async Task ReplyMessage(
        this ITelegramBotClient botClient, 
        string responseText, 
        Update update, 
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            update.Message!.Chat.Id,
            responseText,
            replyParameters: new ReplyParameters { MessageId = update.Message!.Id },
            cancellationToken: cancellationToken);
    }
    
    public static bool Matches(this string str, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (str.ToLower().Contains(pattern.ToLower()))
                return true;
        }
        return false;
    }
}


class Program
{
    private static readonly string _token = "8035465554:AAErw3fVribIOSYIYOAwdByMobYQH5eQrQ0";
    
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("starting");
        var bot = new TelegramBotClient(_token);
        var me = await bot.GetMe();
        Console.WriteLine($"Bot started: {me.Username}");

        using var cts = new CancellationTokenSource();
        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: cts.Token
        );    
        
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        await cts.CancelAsync();
    }
    
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null)
        {
            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            if (messageText.Matches("пиво", "пива", "пивчик", "пивко"))
            {
                var random = new Random();
                var fact = Replies.RandomBeerFacts[random.Next(Replies.RandomBeerFacts.Count)];
                await botClient.ReplyMessage(fact, update, cancellationToken);
            }

            if (messageText.Matches("шмаль", "шмали", "травчик", "ганджа", "ганжубас", "марихуана", "косяк", "блант",
                    "конопля", "тгк", "анаша", "водник", "сухой"))
            {
                var random = new Random();
                var fact = Replies.RandomWeedFacts[random.Next(Replies.RandomWeedFacts.Count)];
                await botClient.ReplyMessage(fact, update, cancellationToken);
            }
            
            if (messageText.Matches("привет", "здрасте", "здарова", "хай", "hello", "hi", "hey"))
            {
                var random = new Random();
                var greeting = Replies.Greetings[random.Next(Replies.Greetings.Count)];
                await botClient.ReplyMessage(greeting, update, cancellationToken);
            }

            if (messageText.Matches("хуй", "гандон", "пидар", "пидор"))
            {
                await botClient.ReplyMessage("Сам такой!", update, cancellationToken);
            }
        }
    }
    
    static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Error handled: " + exception.Message);
    }
}