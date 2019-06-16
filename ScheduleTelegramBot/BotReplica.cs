using System.Text;

namespace ScheduleTelegramBot
{
    public static class BotReplica
    {
        public const string OnEditScheduleRequest = "Send your edit-access-token to get access to schedule edit.";
        public const string OnAdministrationRequest = "Send api-access-token for bot to confirm that you are admin.";
        public const string OnIncorrectEditToken = "Unknown edit-access-token, try again.";
        public const string OnCorrectEditTokenToEdit = "Accept, pick the day that you wanna change:";
        public const string OnCorrectApiTokenToGetEditToken = "Accept, your new edit-access-token:\n{0}";
        public const string OnCorrectApiTokenToRemoveEditToken = "Ok, pick token that you wanna recall.";
        public const string OnCorrectApiTokenToRemoveEditTokenIfNoTokens = "Accept, but bot hasn't edit-access-tokens.";
        public const string OnSuccessfullyDayScheduleEdit = "{0} schedule successfull edited.";
        public const string OnUnsuccessfullyDayScheduleEdit = "Incorrect schedule format, try again.";
        public const string OnEmptyDaySchedule = "free lessons day.";
        public const string OnEmptyFullSchedule = "Schedule didn't set yet.";
        public const string OnTodayRequestInSunday = "Day off, no lessons.";
        public const string OnCustomDayCommand = "Ok, pick day that you wanna see:";
        public const string OnRecallLastEditToken = "Success, the last edit-access-token '{0}' was recall.";
        public const string OnRecallNotLastEditToken = "Success, edit-access-token '{0}' was recall.";

        public static readonly string OnInlinePickDayToEdit = string.Join(
            '\n',
            "{0} picked. Send day schedule in specified format:\n",
            "9:00 (I), Web and Html, 512, Solodushkin S.I.",
            "10:40 (II), Combinatorial algorithms, 622, Asanov M.O.",
            "12:50 (III), Physical education",
            "16:10 (V), OOP(even week, second subgroup), 526");

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