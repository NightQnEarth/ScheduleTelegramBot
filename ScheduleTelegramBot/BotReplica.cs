using System.Text;

namespace ScheduleTelegramBot
{
    public static class BotReplica
    {
        public const string OnEditScheduleRequest =
            "Отправьте ваш access-token для получени доступа к редактированию расписания.";

        public const string OnAdministrationRequest =
            "Отправьте API-access-token бота, чтобы подтвердить, что вы являетесь администратором.";

        public const string OnIncorrectAccessToken = "Access-token не опознан. Попробуйте снова.";
        public const string OnCorrectAccessTokenToEdit = "Ok, pick the day that you wanna change:";
        public const string OnCorrectAccessTokenToGetPassword = "Ok, your new access-token:\n";
        public const string OnCorrectAccessTokenToRemovePassword = "Ok, pick the token you want to remove.";
        public const string OnCorrectAccessTokenToRemovePasswordIfNoPasswords = "Bot has not access-tokens.";
        public const string OnSuccessfullyDayScheduleEdit = "Расписание успешно изменено.";
        public const string OnUnsuccessfullyDayScheduleEdit = "Некорректный формат расписания. Попробуйте снова.";
        public const string OnEmptyDaySchedule = "В этот день занятий нет.";
        public const string OnTodayRequestInSunday = "Сегодня выходной, занятий нет.";
        public const string OnCustomDayCommand = "Ok, pick the day that you see:";

        public static readonly string OnInlinePickDayToEdit = string.Join(
            '\n',
            "Ok, отправьте расписание выбранного дня в следующем формате:\n",
            "I   - 9:00,  Название предмета, номер аудитории, имя преподавателя",
            "II  - 10:40, Название предмета, номер аудитории, имя преподавателя",
            "III - 12:50, Название предмета, номер аудитории, имя преподавателя",
            "IV  - 14:30, Название предмета, номер аудитории, имя преподавателя",
            "V   - 16:10, Название предмета, номер аудитории, имя преподавателя",
            "VI  - 17:50, Название предмета, номер аудитории, имя преподавателя");

        public static readonly string HelpMessage;

        static BotReplica()
        {
            var stringBuilder = new StringBuilder();
            foreach (var botCommand in Bot.BotCommands)
                stringBuilder.Append($"{botCommand.ChatRepresentation} - {botCommand.Description}\n");

            HelpMessage = stringBuilder.ToString();
        }
    }
}