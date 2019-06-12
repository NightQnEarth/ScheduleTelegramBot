using System;
using System.IO;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ScheduleTelegramBot
{
    static class Program
    {
        private const string TokenStorageFilename = "TelegramBotToken";
        private static TelegramBotClient botClient;

        public static void Main()
        {
            var telegramBotToken = File.ReadAllText(TokenStorageFilename, Encoding.UTF8);
            botClient = new TelegramBotClient(telegramBotToken);
#if DEBUG
            var bot = botClient.GetMeAsync().Result;
            Console.WriteLine($"Hello, World! I am user {bot.Id} and my name is {bot.FirstName}{Environment.NewLine}.");
#endif
            botClient.OnMessage += BotOnMessage;

            botClient.StartReceiving();

            Thread.Sleep(Timeout.Infinite);
        }

        private static async void BotOnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
#if DEBUG
                Console.WriteLine(
                    $"Received a text message from {e.Message.From.FirstName} {e.Message.From.LastName}.");
                Console.WriteLine($"Message content:{Environment.NewLine}{e.Message.Text}");
#endif
                await botClient.SendTextMessageAsync(e.Message.Chat, "You said:\n" + e.Message.Text);
            }
        }
    }
}