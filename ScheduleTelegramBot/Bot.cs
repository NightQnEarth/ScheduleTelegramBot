using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot
{
    public class Bot
    {
        public static readonly Dictionary<string, BotCommand> BotCommandByRepresent;
        public static readonly Dictionary<WorkDay, string> RepresentByWorkDay;
        private static readonly Dictionary<string, WorkDay> workDayByRepresent;
        private static readonly InlineKeyboardMarkup inlineWorkDaysKeyboard;
        private readonly AccessTokensCache accessTokensCache;
        private readonly TelegramBotClient botClient;
        private readonly Schedule schedule;
        private readonly Dictionary<long, BotCommand> previousCommandByChatId;
        private readonly Dictionary<long, WorkDay> selectedToEditDayByChatId;
        private readonly Dictionary<long, int> keyboardMessageIdByChatId;

        static Bot()
        {
            BotCommandByRepresent = typeof(BotCommand).GetEnumValues().Cast<BotCommand>().ToDictionary(
                command => command
                           .GetAttribute<ChatRepresentation>().ToString(),
                command => command);
            workDayByRepresent = typeof(WorkDay).GetEnumValues().Cast<WorkDay>().ToDictionary(
                day => day.GetAttribute<ChatRepresentation>().ToString(),
                day => day);
            RepresentByWorkDay = workDayByRepresent.ToDictionary(pair => pair.Value, pair => pair.Key);
            inlineWorkDaysKeyboard = new InlineKeyboardMarkup(workDayByRepresent.Values.Select(day => new[]
            {
                InlineKeyboardButton.WithCallbackData(RepresentByWorkDay[day])
            }));
        }

        public Bot(AccessTokensCache accessTokensCache)
        {
            this.accessTokensCache = accessTokensCache;
            botClient = new TelegramBotClient(accessTokensCache.ApiAccessToken);
            schedule = new Schedule();
            previousCommandByChatId = new Dictionary<long, BotCommand>();
            selectedToEditDayByChatId = new Dictionary<long, WorkDay>();
            keyboardMessageIdByChatId = new Dictionary<long, int>();
        }

        public void StartReceiving(TimeSpan workTime)
        {
            botClient.OnMessage += BotOnMessageAsync;
            botClient.OnCallbackQuery += BotOnCallbackQueryAsync;
            botClient.OnReceiveError += BotOnReceiveErrorAsync;

            var timer = new Stopwatch();
            var workTimeIsInfinity = workTime == Timeout.InfiniteTimeSpan;

            if (!workTimeIsInfinity) timer.Start();

            while (true)
            {
                try
                {
                    botClient.StartReceiving();

                    Thread.Sleep(workTimeIsInfinity ? workTime : workTime - timer.Elapsed);

                    botClient.StopReceiving();
                }
                catch (ApiRequestException exception)
                {
                    Console.WriteLine(exception);
                }

                if (!workTimeIsInfinity && timer.Elapsed >= workTime) break;
            }
        }

        private async void BotOnMessageAsync(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text) return;

            var chatId = message.Chat.Id;
            var firstWordOfReceivedMessage = message.Text.Split().First();

            if (keyboardMessageIdByChatId.ContainsKey(chatId))
            {
                await botClient.DeleteMessageAsync(chatId, keyboardMessageIdByChatId[chatId]);
                keyboardMessageIdByChatId.Remove(chatId);
                previousCommandByChatId.Remove(chatId);
            }

            if (BotCommandByRepresent.ContainsKey(firstWordOfReceivedMessage))
            {
                selectedToEditDayByChatId.Remove(chatId);

                var receivedCommandType = BotCommandByRepresent[firstWordOfReceivedMessage];

                switch (receivedCommandType)
                {
                    case BotCommand.Today:
                        var today = message.Date.DayOfWeek;
                        await botClient.SendTextMessageAsync(chatId, today == DayOfWeek.Sunday
                                                                 ? BotReplica.OnTodayRequestInSunday
                                                                 : schedule.GetDaySchedule((WorkDay)today));

                        previousCommandByChatId.Remove(chatId);
                        break;
                    case BotCommand.Full:
                        await botClient.SendTextMessageAsync(chatId, schedule.GetFullSchedule());
                        previousCommandByChatId.Remove(chatId);
                        break;
                    case BotCommand.CustomDay:
                        keyboardMessageIdByChatId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCustomDayCommand,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        previousCommandByChatId[chatId] = receivedCommandType;
                        break;
                    case BotCommand.EditSchedule:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnEditScheduleRequest);
                        previousCommandByChatId[chatId] = receivedCommandType;
                        break;
                    case BotCommand.ClearDaySchedule:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnEditScheduleRequest);
                        previousCommandByChatId[chatId] = receivedCommandType;
                        break;
                    case BotCommand.GetAccess:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        previousCommandByChatId[chatId] = receivedCommandType;
                        break;
                    case BotCommand.RecallAccess:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnAdministrationRequest);
                        previousCommandByChatId[chatId] = receivedCommandType;
                        break;
                }
            }
            else if (previousCommandByChatId.ContainsKey(chatId) &&
                     previousCommandByChatId[chatId] != BotCommand.CustomDay)
                switch (previousCommandByChatId[chatId])
                {
                    case BotCommand.EditSchedule when accessTokensCache.IsValidToken(message.Text):
                        keyboardMessageIdByChatId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCorrectEditTokenToEdit,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        break;
                    case BotCommand.ClearDaySchedule when accessTokensCache.IsValidToken(message.Text):
                        keyboardMessageIdByChatId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, BotReplica.OnCorrectEditTokenToClearDay,
                            replyMarkup: inlineWorkDaysKeyboard)).MessageId;
                        break;
                    case BotCommand.GetAccess when accessTokensCache.IsApiAccessToken(message.Text):
                        var newAccessToken = AccessTokensCache.GenerateAccessToken();
                        await botClient.SendTextMessageAsync(
                            chatId, string.Format(BotReplica.OnCorrectApiTokenToGetEditToken, newAccessToken));
                        accessTokensCache.Add(newAccessToken);
                        previousCommandByChatId.Remove(chatId);
                        break;
                    case BotCommand.RecallAccess when accessTokensCache.IsApiAccessToken(message.Text):
                        if (accessTokensCache.Count == 0)
                        {
                            await botClient.SendTextMessageAsync(
                                chatId, BotReplica.OnCorrectApiTokenToRemoveEditTokenIfNoTokens);
                            previousCommandByChatId.Remove(chatId);
                        }
                        else
                            keyboardMessageIdByChatId[chatId] = (await botClient.SendTextMessageAsync(
                                chatId, BotReplica.OnCorrectApiTokenToRemoveEditToken,
                                replyMarkup: accessTokensCache.GetInlineAccessTokensKeyboard())).MessageId;

                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, BotReplica.OnIncorrectEditToken);
                        break;
                }
            else if (selectedToEditDayByChatId.ContainsKey(chatId))
            {
                if (schedule.TrySetDaySchedule(selectedToEditDayByChatId[chatId], message.Text))
                {
                    keyboardMessageIdByChatId[chatId] = (await botClient.SendTextMessageAsync(
                        chatId, string.Format(BotReplica.OnSuccessfullyDayScheduleEdit,
                                              RepresentByWorkDay[selectedToEditDayByChatId[chatId]]),
                        replyMarkup: inlineWorkDaysKeyboard)).MessageId;

                    previousCommandByChatId[chatId] = BotCommand.EditSchedule;
                    selectedToEditDayByChatId.Remove(chatId);
                }
                else
                    await botClient.SendTextMessageAsync(chatId, BotReplica.OnUnsuccessfullyDayScheduleEdit);
            }
            else
            {
                previousCommandByChatId.Remove(chatId);
                await botClient.SendTextMessageAsync(chatId, BotReplica.HelpMessage);
            }
        }

        private async void BotOnCallbackQueryAsync(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            var chatId = callbackQuery.Message.Chat.Id;

            switch (previousCommandByChatId[chatId])
            {
                case BotCommand.EditSchedule:
                    await botClient.DeleteMessageAsync(chatId, keyboardMessageIdByChatId[chatId]);
                    keyboardMessageIdByChatId.Remove(chatId);

                    await botClient.SendTextMessageAsync(
                        chatId, string.Format(BotReplica.OnInlinePickDayToEdit, callbackQuery.Data));
                    selectedToEditDayByChatId[callbackQuery.Message.Chat.Id] = workDayByRepresent[callbackQuery.Data];
                    previousCommandByChatId.Remove(chatId);
                    break;
                case BotCommand.ClearDaySchedule:
                    await botClient.DeleteMessageAsync(chatId, keyboardMessageIdByChatId[chatId]);
                    keyboardMessageIdByChatId.Remove(chatId);

                    schedule.ClearDaySchedule(workDayByRepresent[callbackQuery.Data]);

                    await botClient.SendTextMessageAsync(
                        chatId, string.Format(BotReplica.OnInlinePickDayToClear, callbackQuery.Data));
                    previousCommandByChatId.Remove(chatId);
                    break;
                case BotCommand.CustomDay:
                    await botClient.SendTextMessageAsync(
                        chatId, schedule.GetDaySchedule(workDayByRepresent[callbackQuery.Data]));
                    break;
                case BotCommand.RecallAccess:
                    await botClient.DeleteMessageAsync(chatId, keyboardMessageIdByChatId[chatId]);

                    accessTokensCache.Remove(callbackQuery.Data);

                    if (accessTokensCache.Count == 0)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId, string.Format(BotReplica.OnRecallLastEditToken, callbackQuery.Data));

                        keyboardMessageIdByChatId.Remove(chatId);
                        previousCommandByChatId.Remove(chatId);
                    }
                    else
                        keyboardMessageIdByChatId[chatId] = (await botClient.SendTextMessageAsync(
                            chatId, string.Format(BotReplica.OnRecallNotLastEditToken, callbackQuery.Data),
                            replyMarkup: accessTokensCache
                                .GetInlineAccessTokensKeyboard())).MessageId;

                    break;
            }
        }

        private static async void BotOnReceiveErrorAsync(object sender, ReceiveErrorEventArgs receiveErrorEventArgs) =>
            await Task.Run(() => Console.WriteLine("Received error: {0} — {1}",
                                                   receiveErrorEventArgs.ApiRequestException.ErrorCode,
                                                   receiveErrorEventArgs.ApiRequestException.Message));
    }
}