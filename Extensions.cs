using Telegram.Bot;
using Telegram.Bot.Types;

namespace roshaikinbot;

public static class BotExtensions
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
        Update update,
        CancellationToken cancellationToken,
        string insult = "чертила ебаная")
    {
        User sender = update.Message!.From!;
        if (insult.Matches(Dictionary.Insults))
        {
            if (sender.Username == "vvoolodyaa")
            {
                await botClient.ReplyMessage(Dictionary.InsultVolodya, update, cancellationToken);
                return;
            }
            
            if (sender.Username == "poproshaikin")
            {
                await botClient.ReplyMessage(Dictionary.InsultStas, update, cancellationToken);
                return;
            }
            
            if (sender.Username == "dennisorl")
            {
                await botClient.ReplyMessage(Dictionary.InsultDenis, update, cancellationToken);
                return;
            }
            
            if (sender.Username == "roseplug")
            {
                await botClient.ReplyMessage(Dictionary.InsultMykyta, update, cancellationToken);
                return;
            }
            
            await botClient.ReplyMessage("Сам ты " + insult, update, cancellationToken);
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

public static class StringExtensions
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
