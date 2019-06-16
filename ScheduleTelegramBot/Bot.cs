using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot
{
    public class Bot // TODO: Refactor fields.
    {
        #region Fields

        private readonly TelegramBotClient botClient;
        private readonly Schedule schedule;

        public static readonly HashSet<BotCommand> BotCommands =
            typeof(BotCommandType).GetEnumValues()
                                  .Cast<BotCommandType>()
                                  .Select(type => new BotCommand(type))
                                  .ToHashSet();

        private static readonly Dictionary<string, BotCommandType> botCommandsMap =
            BotCommands.ToDictionary(command => command.ChatRepresentation, command => command.CommandType);

        private static readonly Dictionary<string, WorkDay> workDaysMap =
            typeof(WorkDay).GetEnumValues()
                           .Cast<WorkDay>()
                           .ToDictionary(day => day.GetAttribute<ChatRepresentation>().Representation, day => day);


        private readonly AccessTokensCache accessTokensCache;

        private readonly Dictionary<long, BotCommandType> chatPreviousCommand = new Dictionary<long, BotCommandType>();

        private static readonly InlineKeyboardMarkup inlineWorkDaysKeyboard = new InlineKeyboardMarkup(
            workDaysMap.Values.Select(day => new[]
            {
                InlineKeyboardButton.WithCallbackData(day.GetAttribute<ChatRepresentation>().Representation)
            }));

        private readonly Dictionary<long, WorkDay> chatEditDay = new Dictionary<long, WorkDay>();
        private readonly Dictionary<long, int> chatWithInlineKeyBoard = new Dictionary<long, int>();

        #endregion

        public Bot(AccessTokensCache accessTokensCache)
        {
            this.accessTokensCache = accessTokensCache;

            botClient = new TelegramBotClient(accessTokensCache.ApiAccessToken);
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

                if (chatPreviousCommand[chatId] == BotCommandType.RecallPassword)
                    chatPreviousCommand.Remove(chatId);
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
                        chatPreviousCommand.Remove(chatId);
                        break;
                    case BotCommandType.All:
                        await botClient.SendTextMessageAsync(chatId, schedule.GetFullSchedule());
                        chatPreviousCommand.Remove(chatId);
                        break;
                    case BotCommandType.CustomDay:
                        chatWithInlineKeyBoard[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCustomDayCommand,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        chatPreviousCommand[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.EditSchedule:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnEditScheduleRequest);
                        chatPreviousCommand[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.GetPassword:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        chatPreviousCommand[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.RecallPassword:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        chatPreviousCommand[chatId] = receivedCommandType;
                        break;
                }
            }
            else if (chatPreviousCommand.ContainsKey(chatId) &&
                     chatPreviousCommand[chatId] != BotCommandType.CustomDay)
                switch (chatPreviousCommand[chatId])
                {
                    case BotCommandType.EditSchedule when accessTokensCache.Contains(message.Text):
                        chatWithInlineKeyBoard[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCorrectAccessTokenToEdit,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        break;
                    case BotCommandType.GetPassword when accessTokensCache.Contains(message.Text):
                        var newAccessToken = GenerateAccessToken();
                        await botClient.SendTextMessageAsync(
                            chatId, $"{BotReplica.OnCorrectAccessTokenToGetPassword}{newAccessToken}");
                        accessTokensCache.Add(newAccessToken);
                        chatPreviousCommand.Remove(chatId);
                        break;
                    case BotCommandType.RecallPassword when accessTokensCache.Contains(message.Text):
                        if (accessTokensCache.Count == 1)
                            await botClient.SendTextMessageAsync(
                                chatId, BotReplica.OnCorrectAccessTokenToRemovePasswordIfNoPasswords);
                        else
                            await botClient.SendTextMessageAsync(
                                chatId, BotReplica.OnCorrectAccessTokenToRemovePassword,
                                replyMarkup: accessTokensCache.GetInlineAccessTokensKeyboard());
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
                chatPreviousCommand.Remove(chatId);
                await botClient.SendTextMessageAsync(chatId, BotReplica.HelpMessage);
            }
        }

        private async void BotOnCallbackQuery(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            var chatId = callbackQuery.Message.Chat.Id;

            switch (chatPreviousCommand[chatId])
            {
                case BotCommandType.EditSchedule:
                    await botClient.DeleteMessageAsync(chatId, chatWithInlineKeyBoard[chatId]);
                    chatWithInlineKeyBoard.Remove(chatId);

                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, BotReplica.OnInlinePickDayToEdit);
                    chatEditDay[callbackQuery.Message.Chat.Id] = workDaysMap[callbackQuery.Data];
                    chatPreviousCommand.Remove(chatId);
                    break;
                case BotCommandType.CustomDay:
                    await botClient.SendTextMessageAsync(chatId,
                                                         schedule.GetDaySchedule(workDaysMap[callbackQuery.Data]));
                    break;
                case BotCommandType.RecallPassword:
                    await botClient.DeleteMessageAsync(chatId, chatWithInlineKeyBoard[chatId]);

                    accessTokensCache.Remove(callbackQuery.Data);


                    if (accessTokensCache.Count == 1)
                        await botClient.SendTextMessageAsync(
                            chatId, $"Последний access-token '{callbackQuery.Data}' отозван.");
                    else
                        await botClient.SendTextMessageAsync(chatId, $"Access-token '{callbackQuery.Data}' отозван.",
                                                             replyMarkup: accessTokensCache
                                                                 .GetInlineAccessTokensKeyboard());
                    break;
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs) =>
            Console.WriteLine("Received error: {0} — {1}",
                              receiveErrorEventArgs.ApiRequestException.ErrorCode,
                              receiveErrorEventArgs.ApiRequestException.Message);

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
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