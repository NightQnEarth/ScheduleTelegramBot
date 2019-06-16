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
    public class Bot
    {
        public static readonly HashSet<BotCommand> BotCommands;
        private static readonly Dictionary<string, BotCommandType> representCommandTypes;
        private static readonly Dictionary<string, WorkDay> representWorkDays;
        private static readonly InlineKeyboardMarkup inlineWorkDaysKeyboard;
        private readonly AccessTokensCache accessTokensCache;
        private readonly TelegramBotClient botClient;
        private readonly Schedule schedule;
        private readonly Dictionary<long, BotCommandType> chatIdPreviousCommand;
        private readonly Dictionary<long, WorkDay> chatIdPickedToEditDay;
        private readonly Dictionary<long, int> chatIdKeyboardMessageId;

        static Bot()
        {
            BotCommands = typeof(BotCommandType).GetEnumValues().Cast<BotCommandType>()
                                                .Select(type => new BotCommand(type)).ToHashSet();
            representCommandTypes = BotCommands.ToDictionary(command => command.ChatRepresentation,
                                                             command => command.CommandType);
            representWorkDays = typeof(WorkDay).GetEnumValues().Cast<WorkDay>()
                                               .ToDictionary(
                                                   day => day.GetAttribute<ChatRepresentation>().Representation,
                                                   day => day);
            inlineWorkDaysKeyboard = new InlineKeyboardMarkup(representWorkDays.Values.Select(day => new[]
            {
                InlineKeyboardButton.WithCallbackData(day.GetAttribute<ChatRepresentation>().Representation)
            }));
        }

        public Bot(AccessTokensCache accessTokensCache)
        {
            this.accessTokensCache = accessTokensCache;
            botClient = new TelegramBotClient(accessTokensCache.ApiAccessToken);
            schedule = new Schedule();
            chatIdPreviousCommand = new Dictionary<long, BotCommandType>();
            chatIdPickedToEditDay = new Dictionary<long, WorkDay>();
            chatIdKeyboardMessageId = new Dictionary<long, int>();
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

            if (chatIdKeyboardMessageId.ContainsKey(chatId))
            {
                await botClient.DeleteMessageAsync(chatId, chatIdKeyboardMessageId[chatId]);
                chatIdKeyboardMessageId.Remove(chatId);
                chatIdPreviousCommand.Remove(chatId);
            }

            if (representCommandTypes.ContainsKey(firstWordOfReceivedMessage))
            {
                chatIdPickedToEditDay.Remove(chatId);

                var receivedCommandType = representCommandTypes[firstWordOfReceivedMessage];

                switch (receivedCommandType)
                {
                    case BotCommandType.Today:
                        var today = message.Date.DayOfWeek;
                        await botClient.SendTextMessageAsync(chatId, today == DayOfWeek.Sunday
                                                                 ? BotReplica.OnTodayRequestInSunday
                                                                 : schedule.GetDaySchedule((WorkDay)today));
                        chatIdPreviousCommand.Remove(chatId);
                        break;
                    case BotCommandType.Full:
                        await botClient.SendTextMessageAsync(chatId, schedule.GetFullSchedule());
                        chatIdPreviousCommand.Remove(chatId);
                        break;
                    case BotCommandType.CustomDay:
                        chatIdKeyboardMessageId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCustomDayCommand,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        chatIdPreviousCommand[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.EditSchedule:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnEditScheduleRequest);
                        chatIdPreviousCommand[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.GetAccess:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        chatIdPreviousCommand[chatId] = receivedCommandType;
                        break;
                    case BotCommandType.RecallAccess:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        chatIdPreviousCommand[chatId] = receivedCommandType;
                        break;
                }
            }
            else if (chatIdPreviousCommand.ContainsKey(chatId) &&
                     chatIdPreviousCommand[chatId] != BotCommandType.CustomDay)
                switch (chatIdPreviousCommand[chatId])
                {
                    case BotCommandType.EditSchedule when accessTokensCache.IsValidToken(message.Text):
                        chatIdKeyboardMessageId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCorrectEditTokenToEdit,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        break;
                    case BotCommandType.GetAccess when accessTokensCache.IsApiAccessToken(message.Text):
                        var newAccessToken = GenerateAccessToken();
                        await botClient.SendTextMessageAsync(
                            chatId, string.Format(BotReplica.OnCorrectApiTokenToGetEditToken, newAccessToken));
                        accessTokensCache.Add(newAccessToken);
                        chatIdPreviousCommand.Remove(chatId);
                        break;
                    case BotCommandType.RecallAccess when accessTokensCache.IsApiAccessToken(message.Text):
                        if (accessTokensCache.Count == 0)
                            await botClient.SendTextMessageAsync(
                                chatId, BotReplica.OnCorrectApiTokenToRemoveEditTokenIfNoTokens);
                        else
                            chatIdKeyboardMessageId[chatId] = (await botClient.SendTextMessageAsync(
                                chatId, BotReplica.OnCorrectApiTokenToRemoveEditToken,
                                replyMarkup: accessTokensCache.GetInlineAccessTokensKeyboard())).MessageId;
                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnIncorrectEditToken);
                        break;
                }
            else if (chatIdPickedToEditDay.ContainsKey(chatId))
            {
                if (schedule.TrySetDaySchedule(chatIdPickedToEditDay[chatId], message.Text))
                {
                    chatIdKeyboardMessageId[chatId] = (await botClient.SendTextMessageAsync(
                        chatId,
                        string.Format(BotReplica.OnSuccessfullyDayScheduleEdit,
                                      chatIdPickedToEditDay[chatId].GetAttribute<ChatRepresentation>().Representation),
                        replyMarkup: inlineWorkDaysKeyboard)).MessageId;

                    chatIdPreviousCommand[chatId] = BotCommandType.EditSchedule;
                    chatIdPickedToEditDay.Remove(chatId);
                }
                else
                    await botClient.SendTextMessageAsync(chatId, BotReplica.OnUnsuccessfullyDayScheduleEdit);
            }
            else
            {
                chatIdPreviousCommand.Remove(chatId);
                await botClient.SendTextMessageAsync(chatId, BotReplica.HelpMessage);
            }
        }

        private async void BotOnCallbackQuery(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            var chatId = callbackQuery.Message.Chat.Id;

            switch (chatIdPreviousCommand[chatId])
            {
                case BotCommandType.EditSchedule:
                    await botClient.DeleteMessageAsync(chatId, chatIdKeyboardMessageId[chatId]);
                    chatIdKeyboardMessageId.Remove(chatId);

                    await botClient.SendTextMessageAsync(
                        chatId, string.Format(BotReplica.OnInlinePickDayToEdit, callbackQuery.Data));
                    chatIdPickedToEditDay[callbackQuery.Message.Chat.Id] = representWorkDays[callbackQuery.Data];
                    chatIdPreviousCommand.Remove(chatId);
                    break;
                case BotCommandType.CustomDay:
                    await botClient.SendTextMessageAsync(
                        chatId, schedule.GetDaySchedule(representWorkDays[callbackQuery.Data]));
                    break;
                case BotCommandType.RecallAccess:
                    await botClient.DeleteMessageAsync(chatId, chatIdKeyboardMessageId[chatId]);

                    accessTokensCache.Remove(callbackQuery.Data);

                    if (accessTokensCache.Count == 0)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId, string.Format(BotReplica.OnRecallLastEditToken, callbackQuery.Data));
                        chatIdKeyboardMessageId.Remove(chatId);
                    }
                    else
                        chatIdKeyboardMessageId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, string.Format(BotReplica.OnRecallNotLastEditToken, callbackQuery.Data),
                            replyMarkup: accessTokensCache
                                .GetInlineAccessTokensKeyboard())).MessageId;

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
            const string symbolsPool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%&";

            var resultPassword = new StringBuilder();
            Random random = new Random();
            while (0 < length--) resultPassword.Append(symbolsPool[random.Next(symbolsPool.Length)]);

            return resultPassword.ToString();
        }
    }
}