using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace roshaikinbot;

class Program
{
    private static readonly string _token = File.ReadAllText("../../../api_key");
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static readonly Storage _storage = new();
    private const string _ourChatId = "-4285267963";
    private const string _address = "гаврик,";
    private const string _myName = "гаврик";

    private const double _randomResponseProbability = 0.10;
    
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
        Console.WriteLine($"{senderName}: {messageText}");
        
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

            if (firstWord.Matches("пошел", "пошол", "иди", "іді", "йди", "пошёл"))
            {
                if (words[1].Matches("нахуй", "нахер"))
                {
                    await botClient.ReactToInsult(update, cancellationToken);
                    return;
                }
            }

            if (firstWord.Matches(Dictionary.InsultNouns))
            {
                await botClient.ReactToInsult(update, cancellationToken, words[0]);
                return;
            }

            if (firstWord.Matches("ты", "ти"))
            {
                if (words[1].Matches(Dictionary.InsultNouns))
                {
                    await botClient.ReactToInsult(update, cancellationToken, words[1]);
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

        if (messageText == _myName)
        {
            await botClient.ReplyMessage(Dictionary.Random(Dictionary.Greetings), update, cancellationToken);
            return;
        }

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

        var random = new Random();
        if (random.NextDouble() < _randomResponseProbability)
        {
            await botClient.ReplyMessage(Dictionary.GenerateRandomReply(), update, cancellationToken);
        }
    }
    
    static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Error handled: " + exception.Message);
    }
}
