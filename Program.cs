using Telegram.Bot;
using Telegram.Bot.Requests;
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
        
        Console.WriteLine();
        Console.WriteLine($"(chat id: {update.Message.Chat.Id})");
        Console.WriteLine($"Responded to {update.Message.From?.FirstName ?? "unknown"}: {responseText}");
    }
    
    public static async Task ReplyMessage(
        this ITelegramBotClient botClient,
        List<string> dictionary, 
        Update update,
        CancellationToken cancellationToken)
    {
        string responseText = Dictionary.Random(dictionary);
        
        await botClient.SendMessage(
            update.Message!.Chat.Id,
            responseText,
            replyParameters: new ReplyParameters { MessageId = update.Message.Id },
            cancellationToken: cancellationToken);
        
        Console.WriteLine();
        Console.WriteLine($"(chat id: {update.Message.Chat.Id})");
        Console.WriteLine($"Responded: {responseText}");
    }

    public static async Task ReactToInsult(
        this ITelegramBotClient botClient,
        string insult,
        Update update,
        CancellationToken cancellationToken)
    {
        User sender = update.Message!.From!;
        if (insult.Matches(Dictionary.Insults))
        {
            if (sender.Username == "vvoolodyaa")
            {
                await botClient.ReplyMessage(Dictionary.InsultVolodya, update, cancellationToken);
            }
            if (sender.Username == "poproshaikin")
            {
                await botClient.ReplyMessage(Dictionary.InsultStas, update, cancellationToken);
            }

            if (sender.Username == "dennisorl")
            {
                await botClient.ReplyMessage(Dictionary.InsultDenis, update, cancellationToken);
            }
            else
            {
                await botClient.ReplyMessage("Сам ты " + insult, update, cancellationToken);
            }
        }
    }
    
    public static async Task HandleDrunkenBeer(
        this ITelegramBotClient botClient,
        string thirdWord,
        Update update,
        Storage storage,
        CancellationToken cancellationToken)
    {
        User sender = update.Message!.From!;
        
        // amount
        var amountMl = thirdWord.ParseAmount();
        if (amountMl is null)
        {
            await botClient.ReplyMessage(Dictionary.Bullshit, update, cancellationToken);
            return;
        }

        var unit = thirdWord.ParseUnit();
        if (!Dictionary.UnitConversions.TryGetValue(unit, out double conversionFactor))
        {
            await botClient.ReplyMessage(Dictionary.Bullshit, update, cancellationToken);
            return;
        }
            
        storage.AddDrunkenBeer(amountMl.Value, sender.Id, update.Message.Chat.Id);

        await botClient.ReplyMessage(
            $"Я запомнил: {sender.FirstName} выпил(a) {amountMl / conversionFactor}{unit} пива", 
            update,
            cancellationToken
        );
    }
}

class Program
{
    private static readonly string _token = File.ReadAllText("../../../api_key");
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static readonly string _ourChatId = "-4285267963";
    private const string _address = "гаврик,";
    private const string _myName = "гаврик";
    private static readonly Storage _storage = new();
    
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
        if (update.Message == null || update.Message.Date < _startTime)
            return;
        
        var chatId = update.Message.Chat.Id;
        User sender = update.Message.From!;
        var senderName = sender.FirstName;
        var messageText = update.Message.Text;

        Console.WriteLine();
        Console.WriteLine($"(chat id: {update.Message.Chat.Id})");
        Console.WriteLine($"{senderName ?? "unknown"}: {messageText}");

        // if I am addressed
        if (messageText.ToLower().StartsWith(_address))
        {
            string sentence = messageText.ToLower()[_address.Length..].Trim();
            string[] words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // dont bother me
            if (words.Length == 0)
            {
                var response = Dictionary.Random(Dictionary.DontBotherMe);
                await botClient.ReplyMessage(response, update, cancellationToken);
                return;
            }

            string firstWord = words[0];

            if (firstWord.Matches("сосать"))
            {
                await botClient.ReplyMessage("Хочешь мне отсосать?", update, cancellationToken);
                return;
            }

            if (firstWord.Matches(Dictionary.Insults))
            {
                await botClient.ReactToInsult(words[0], update, cancellationToken);
                return;
            }

            if (firstWord.Matches("ты", "ти"))
            {
                if (words[1].Matches(Dictionary.Insults))
                {
                    await botClient.ReactToInsult(words[1], update, cancellationToken);
                    return;
                }
            }
            
            // sender
            if (firstWord.Matches("я"))
            {
                string secondWord = words[1];
                // drank
                if (secondWord.Matches("выпил", "выпила"))
                {
                    string thirdWord = words[2];

                    if ("123456789.".Contains(thirdWord[0]) && thirdWord.EndsWith(Dictionary.LiquidUnits))
                    {
                        await botClient.HandleDrunkenBeer(thirdWord, update, _storage, cancellationToken);
                        return;
                    }
                }
            }
            
            // how much
            if (firstWord.Matches("сколько"))
            {
                string secondWord = words[1];
                if (secondWord.Matches("я"))
                {
                    string thirdWord = words[2];
                    if (thirdWord.Matches("выпил", "випил", "випив", "випила", "выпила"))
                    {
                        double amountMl = _storage.GetDrunkenBeer(sender.Id, chatId);
                        if (amountMl == 0)
                        {
                            await botClient.ReplyMessage("Ты еще не пил", update, cancellationToken);
                            return;
                        }
                        else
                        {
                            var multiplier = Dictionary.UnitConversions["л"];
                            await botClient.ReplyMessage($"Ты выпил(a) {amountMl/multiplier}л пива", update, cancellationToken);
                            return;
                        }
                    }
                }
            }
            
            // clean
            if (firstWord.Matches("очисти", "обнули"))
            {
                string secondWord = words[1];
                if (secondWord.Matches("мой"))
                {
                    string thirdWord = words[2];
                    if (thirdWord.Matches("счет", "счёт"))
                    {
                        _storage.CleanupDrunkenBeer(sender.Id, chatId);
                        
                        await botClient.ReplyMessage(Dictionary.YourBalanceIsCleaned, update, cancellationToken);
                        return;
                    }
                }
            }
        }

        // general responses

        if (messageText.Matches("пиво", "пива", "пивчик", "пивко"))
        {
            var fact = Dictionary.Random(Dictionary.BeerFacts);
            await botClient.ReplyMessage(fact, update, cancellationToken);
            return;
        }

        if (messageText.Matches("шмаль", "шмали", "травчик", "ганджа", "ганжубас", "марихуана", "косяк", "блант",
                "конопля", "тгк", "анаша", "водник", "сухой"))
        {
            var fact = Dictionary.Random(Dictionary.WeedFacts);
            await botClient.ReplyMessage(fact, update, cancellationToken);
            return;
        }

        if (messageText.Matches("привет", "здрасте", "здарова", "хай ", "hello", "hi ", "hey "))
        {
            var greeting = Dictionary.Random(Dictionary.Greetings);
            await botClient.ReplyMessage(greeting, update, cancellationToken);
            return;
        }
    }
    
    static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Error handled: " + exception.Message);
    }
}

static class StringExtensions
{
    
    public static bool Matches(this string str, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (str.ToLower().Contains(pattern.ToLower()))
                return true;
        }
        return false;
    }
    
    public static bool Matches(this string str, IEnumerable<string> patterns)
    {
        return str.Matches(patterns.ToArray());
    }

    public static bool EndsWith(this string str, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (str.ToLower().EndsWith(pattern.ToLower()))
                return true;
        }
        return false;
    }
    
    public static bool EndsWith(this string str, params char[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (str.ToLower().EndsWith(pattern.ToString().ToLower()))
                return true;
        }
        return false;
    }

    public static bool EndsWith(this string str, IEnumerable<string> patterns)
    {
        return str.EndsWith(patterns.ToArray());
    }
    
    public static double? ParseAmount(this string str)
    {
        str = str.ToLower().Trim();
        int i = 0;
        while (i < str.Length && ("1234567890.,".Contains(str[i])))
            i++;

        if (i == 0)
            return null;

        string amountStr = str[..i].Replace(',', '.');
        if (!double.TryParse(amountStr, out double amount))
            return null;

        string unit = str[i..].Trim();
        if (Dictionary.UnitConversions.TryGetValue(unit, out double conversionFactor))
        {
            return amount * conversionFactor;
        }

        return null; // Unknown unit
    }
    
    public static string? ParseUnit(this string str)
    {
        str = str.ToLower().Trim();
        int i = 0;
        while (i < str.Length && ("1234567890.,".Contains(str[i])))
            i++;
   
        if (i == 0 || i == str.Length)
            return null;
   
        string unit = str[i..].Trim();
        return string.IsNullOrEmpty(unit) ? null : unit;
    }
    
}