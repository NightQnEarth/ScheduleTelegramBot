using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace ScheduleTelegramBot // TODO: Change Procfile and others.. 
{
    public static class Program
    {
        private const string TokenStorageName = "TelegramBotToken";
        private static TelegramBotClient botClient;
        private static string telegramBotToken;

        public static readonly HashSet<BotCommand> BotCommands =
            typeof(BotCommandType).GetEnumValues()
                                  .Cast<BotCommandType>()
                                  .Select(type => new BotCommand(type))
                                  .ToHashSet();

        private static readonly Dictionary<string, BotCommandType> botCommandsTypeMap =
            BotCommands.ToDictionary(command => command.ChatRepresentation, command => command.CommandType);

        private static readonly HashSet<string> accessTokens = new HashSet<string>();

        private static readonly Dictionary<long, BotCommandType?> previousCommandForChat =
            new Dictionary<long, BotCommandType?>();

        public static void Main()
        {
            telegramBotToken = File.ReadAllText(TokenStorageName, Encoding.UTF8);
            accessTokens.Add(telegramBotToken);

            botClient = new TelegramBotClient(telegramBotToken);

            botClient.OnMessage += BotOnMessage;
#if DEBUG
            botClient.OnReceiveError += BotOnReceiveError;
#endif
            botClient.StartReceiving();

            Thread.Sleep(Timeout.Infinite);
        }

        private static async void BotOnMessage(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text) return;

            var chatId = message.Chat.Id;
            var firstWordOfReceivedMessage = message.Text.Split().First();

            if (botCommandsTypeMap.ContainsKey(firstWordOfReceivedMessage))
                switch (botCommandsTypeMap[firstWordOfReceivedMessage])
                {
                    case BotCommandType.Today:
                        break;
                    case BotCommandType.All:
                        break;
                    case BotCommandType.CustomDay:
                        break;
                    case BotCommandType.EditSchedule:
                        previousCommandForChat[chatId] = BotCommandType.EditSchedule;
                        await botClient.SendTextMessageAsync(message.Chat, BotReplica.OnEditScheduleRequest);
                        break;
                    case BotCommandType.GetPassword:
                        previousCommandForChat[chatId] = BotCommandType.GetPassword;
                        await botClient.SendTextMessageAsync(message.Chat, BotReplica.OnAdministrationRequest);
                        break;
                    case BotCommandType.RecallPassword:
                        previousCommandForChat[chatId] = BotCommandType.RecallPassword;
                        await botClient.SendTextMessageAsync(message.Chat, BotReplica.OnAdministrationRequest);
                        break;
                }
            else if (previousCommandForChat.ContainsKey(chatId))
            {
                if (previousCommandForChat[chatId] is BotCommandType.EditSchedule &&
                    accessTokens.Contains(message.Text))
                    throw new NotImplementedException();
                if (previousCommandForChat[chatId] == BotCommandType.GetPassword ||
                    previousCommandForChat[chatId] == BotCommandType.RecallPassword)
                    throw new NotImplementedException();
                await botClient.SendTextMessageAsync(message.Chat, BotReplica.OnIncorrectAccessToken);
            }
            else
            {
                previousCommandForChat.Remove(message.Chat.Id);
                await botClient.SendTextMessageAsync(message.Chat, BotReplica.HelpMessage);
            }
        }

        #region BotOnReceiveError

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs) =>
            Console.WriteLine("Received error: {0} — {1}",
                              receiveErrorEventArgs.ApiRequestException.ErrorCode,
                              receiveErrorEventArgs.ApiRequestException.Message);

        #endregion
    }
}