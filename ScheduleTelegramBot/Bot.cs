using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot
{
    public class Bot
    {
        private readonly string apiAccessToken;
        private readonly TelegramBotClient botClient;
        private readonly Schedule schedule;

        public static readonly HashSet<BotCommand> BotCommands =
            typeof(BotCommandType).GetEnumValues()
                                  .Cast<BotCommandType>()
                                  .Select(type => new BotCommand(type))
                                  .ToHashSet();

        private static readonly Dictionary<string, BotCommandType> botCommandsMap =
            BotCommands.ToDictionary(command => command.ChatRepresentation, command => command.CommandType);

        private readonly Dictionary<string, WorkDay> workDaysMap =
            typeof(WorkDay).GetEnumValues()
                           .Cast<WorkDay>()
                           .ToDictionary(day => day.GetAttribute<ChatRepresentation>().Representation, day => day);


        private readonly HashSet<string> accessTokens = new HashSet<string>();

        private readonly Dictionary<long, BotCommandType> previousCommandForChat =
            new Dictionary<long, BotCommandType>();

        private static readonly InlineKeyboardMarkup inlineWorkDaysKeyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        WorkDay.Monday.GetAttribute<ChatRepresentation>().Representation),
                    InlineKeyboardButton.WithCallbackData(
                        WorkDay.Tuesday.GetAttribute<ChatRepresentation>().Representation),
                    InlineKeyboardButton.WithCallbackData(
                        WorkDay.Wednesday.GetAttribute<ChatRepresentation>().Representation)
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        WorkDay.Thursday.GetAttribute<ChatRepresentation>().Representation),
                    InlineKeyboardButton.WithCallbackData(
                        WorkDay.Friday.GetAttribute<ChatRepresentation>().Representation),
                    InlineKeyboardButton.WithCallbackData(
                        WorkDay.Saturday.GetAttribute<ChatRepresentation>().Representation)
                }
            });

        private readonly Dictionary<long, WorkDay> chatEditDay = new Dictionary<long, WorkDay>();
        private readonly Dictionary<long, int> chatWithInlineKeyBoard = new Dictionary<long, int>();

        public Bot(string apiAccessToken)
        {
            accessTokens.Add(this.apiAccessToken = apiAccessToken);
            botClient = new TelegramBotClient(apiAccessToken);
            schedule = new Schedule();
        }

        public void StartReceiving(int millisecondsToWork)
        {
            botClient.OnMessage += BotOnMessage;
            botClient.OnCallbackQuery += BotOnCallbackQuery;
            botClient.OnReceiveError += BotOnReceiveError;

            botClient.StartReceiving();

            Thread.Sleep(millisecondsToWork);

            botClient.StopReceiving();
        }

        private async void BotOnMessage(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text) return;

            var chatId = message.Chat.Id;
            var firstWordOfReceivedMessage = message.Text.Split().First();

            if (chatWithInlineKeyBoard.ContainsKey(chatId))
            {
                await botClient.DeleteMessageAsync(chatId, chatWithInlineKeyBoard[chatId]);
                chatWithInlineKeyBoard.Remove(chatId);

                if (previousCommandForChat[chatId] == BotCommandType.RecallPassword)
                    previousCommandForChat.Remove(chatId);
            }

            if (botCommandsMap.ContainsKey(firstWordOfReceivedMessage))
            {
                chatEditDay.Remove(chatId);

                var receivedCommandType = botCommandsMap[firstWordOfReceivedMessage];

                switch (receivedCommandType)
                {
                    case BotCommandType.Today:
                        var today = message.Date.DayOfWeek;
                        await botClient.SendTextMessageAsync(chatId, today == DayOfWeek.Sunday
                                                                 ? BotReplica.OnTodayRequestInSunday
                                                                 : schedule.GetDaySchedule((WorkDay)today));
                        previousCommandForChat.Remove(chatId);
                        break;
                    case BotCommandType.All:
                        await botClient.SendTextMessageAsync(chatId, schedule.GetFullSchedule());
                        previousCommandForChat.Remove(chatId);
                        break;
                    case BotCommandType.CustomDay:
                        chatWithInlineKeyBoard[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCorrectAccessTokenToEdit,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        previousCommandForChat[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.EditSchedule:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnEditScheduleRequest);
                        previousCommandForChat[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.GetPassword:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        previousCommandForChat[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.RecallPassword:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        previousCommandForChat[chatId] = receivedCommandType;
                        break;
                }
            }
            else if (previousCommandForChat.ContainsKey(chatId) &&
                     previousCommandForChat[chatId] != BotCommandType.CustomDay)
                switch (previousCommandForChat[chatId])
                {
                    case BotCommandType.EditSchedule when accessTokens.Contains(message.Text):
                        chatWithInlineKeyBoard[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCorrectAccessTokenToEdit,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        break;
                    case BotCommandType.GetPassword when accessTokens.Contains(message.Text):
                        var newAccessToken = GenerateAccessToken();
                        await botClient.SendTextMessageAsync(
                            chatId, $"{BotReplica.OnCorrectAccessTokenToGetPassword}{newAccessToken}");
                        accessTokens.Add(newAccessToken);
                        previousCommandForChat.Remove(chatId);
                        break;
                    case BotCommandType.RecallPassword when accessTokens.Contains(message.Text):
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnCorrectAccessTokenToRemovePassword,
                                                             replyMarkup: GetInlineAccessTokensKeyboard());
                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnIncorrectAccessToken);
                        break;
                }
            else if (chatEditDay.ContainsKey(chatId))
            {
                if (schedule.TrySetDaySchedule(chatEditDay[chatId], message.Text))
                {
                    chatWithInlineKeyBoard[chatId] = (await botClient.SendTextMessageAsync(
                        chatId, BotReplica.OnSuccessfullyDayScheduleEdit,
                        replyMarkup: inlineWorkDaysKeyboard)).MessageId;

                    chatEditDay.Remove(chatId);
                }
                else
                    await botClient.SendTextMessageAsync(chatId, BotReplica.OnUnsuccessfullyDayScheduleEdit);
            }
            else
            {
                previousCommandForChat.Remove(chatId);
                await botClient.SendTextMessageAsync(chatId, BotReplica.HelpMessage);
            }
        }

        private async void BotOnCallbackQuery(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            var chatId = callbackQuery.Message.Chat.Id;

            switch (previousCommandForChat[chatId])
            {
                case BotCommandType.EditSchedule:
                    await botClient.DeleteMessageAsync(chatId, chatWithInlineKeyBoard[chatId]);
                    chatWithInlineKeyBoard.Remove(chatId);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, BotReplica.OnInlinePickDayToEdit);
                    chatEditDay[callbackQuery.Message.Chat.Id] = workDaysMap[callbackQuery.Data];
                    previousCommandForChat.Remove(chatId);
                    break;
                case BotCommandType.CustomDay:
                    await botClient.SendTextMessageAsync(chatId,
                                                         schedule.GetDaySchedule(workDaysMap[callbackQuery.Data]));
                    break;
                case BotCommandType.RecallPassword:
                    await botClient.DeleteMessageAsync(chatId, chatWithInlineKeyBoard[chatId]);

                    accessTokens.Remove(callbackQuery.Data);
                    await botClient.SendTextMessageAsync(chatId, $"Access-token '{callbackQuery.Data}' отозван.",
                                                         replyMarkup: GetInlineAccessTokensKeyboard());
                    break;
            }
        }

        #region BotOnReceiveError

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs) =>
            Console.WriteLine("Received error: {0} — {1}",
                              receiveErrorEventArgs.ApiRequestException.ErrorCode,
                              receiveErrorEventArgs.ApiRequestException.Message);

        #endregion

        private InlineKeyboardMarkup GetInlineAccessTokensKeyboard() => new InlineKeyboardMarkup(
            accessTokens
                .Where(accessToken => !accessToken.Equals(apiAccessToken))
                .Select(accessToken => new[] { InlineKeyboardButton.WithCallbackData(accessToken) }));

        private static string GenerateAccessToken(int length = 32)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%&";

            var resultPassword = new StringBuilder();
            Random random = new Random();
            while (0 < length--) resultPassword.Append(valid[random.Next(valid.Length)]);

            return resultPassword.ToString();
        }
    }
}