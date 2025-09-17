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
        
        Console.WriteLine($"Responded to {update.Message.From?.FirstName ?? "unknown"}: {responseText}          (chat id: {update.Message.Chat.Id})");
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
    private static readonly string _token = File.ReadAllText("../../../api_key");
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static readonly string _ourChatId = "-4285267963";
    
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

        while (true)
        {
            string input = Console.ReadLine()!;

            if (input == "Kill")
            {
                await cts.CancelAsync();
                break;
            }
            else
            {
                await bot.SendMessage(_ourChatId, input, cancellationToken: cts.Token);
            }
        }
    }
    
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.Date >= _startTime)
        {
            var sender = update.Message.From?.FirstName;
            var messageText = update.Message.Text;
            
            Console.WriteLine($"{sender ?? "unknown"}: {messageText}          (chat id: {update.Message.Chat.Id})");

            if (messageText.Matches("пиво", "пива", "пивчик", "пивко"))
            {
                var fact = Replies.Random(Replies.BeerFacts);
                await botClient.ReplyMessage(fact, update, cancellationToken);
            }

            if (messageText.Matches("шмаль", "шмали", "травчик", "ганджа", "ганжубас", "марихуана", "косяк", "блант",
                    "конопля", "тгк", "анаша", "водник", "сухой"))
            {
                var fact = Replies.Random(Replies.WeedFacts);
                await botClient.ReplyMessage(fact, update, cancellationToken);
            }
            
            if (messageText.Matches("привет", "здрасте", "здарова", "хай", "hello", "hi", "hey"))
            {
                var greeting = Replies.Random(Replies.Greetings);
                await botClient.ReplyMessage(greeting, update, cancellationToken);
            }
        }
    }
    
    static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Error handled: " + exception.Message);
    }
}