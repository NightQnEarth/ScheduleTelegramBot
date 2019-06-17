using System.Text;

namespace ScheduleTelegramBot
{
    public static class BotPhrases
    {
        public const string OnEditScheduleRequest = "Send your edit-access-token to get access to schedule editing.";
        public const string OnAdministrationRequest = "Send api-access-token for bot to confirm that you are admin.";
        public const string OnIncorrectEditToken = "Unknown edit-access-token, try again.";
        public const string OnCorrectEditTokenToEdit = "Accepted, select a day that you wanna change:";
        public const string OnCorrectEditTokenToClearDay = "Accepted, select a day that you wanna clear:";
        public const string OnCorrectApiTokenToGetEditToken = "Accepted, your new edit-access-token:\n{0}";
        public const string OnCorrectApiTokenToRemoveEditToken = "Ok, select a token that you wanna revoke:";
        public const string OnSuccessfullyDayScheduleEdit = "{0} schedule successfully edited.";
        public const string OnUnsuccessfullyDayScheduleEdit = "Incorrect schedule format, try again.";
        public const string OnEmptyDaySchedule = "free day.";
        public const string OnEmptyFullSchedule = "Schedule hasn't been set yet.";
        public const string OnTodayRequestInSunday = "Day off, no lessons.";
        public const string OnSpecificDayCommand = "Ok, select a day that you wanna look at:";
        public const string OnRevokeLastEditToken = "Success, last edit-access-token '{0}' has been revoke.";
        public const string OnRevokeNotLastEditToken = "Success, edit-access-token '{0}' has been revoke.";
        public const string OnInlineSelectDayToClear = "{0} has been cleared.";

        public const string OnCorrectApiTokenToRemoveEditTokenIfNoTokens =
            "Accepted, but there aren't registered edit-access-tokens.";

        public static readonly string OnInlineSelectDayToEdit = string.Join(
            '\n',
            "{0} selected. Send schedule for the day in following format:\n",
            "9:00 (I), Web and Html, 512, Solodushkin S.I.",
            "10:40 (II), Combinatorial algorithms, 622, Asanov M.O.",
            "12:50 (III), Physical education",
            "16:10 (V), OOP(even week, second subgroup), 526");

        public static readonly string HelpMessage;

        static BotPhrases()
        {
            var stringBuilder = new StringBuilder();
            foreach (var (represent, botCommand) in Bot.BotCommandByRepresent)
                stringBuilder.Append($"{represent} - {botCommand.GetAttribute<CommandDescription>()}\n");

            HelpMessage = stringBuilder.ToString();
        }
    }
}