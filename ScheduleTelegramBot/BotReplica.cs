using System.Text;

namespace ScheduleTelegramBot
{
    public static class BotReplica
    {
        static BotReplica()
        {
            var stringBuilder = new StringBuilder();
            foreach (var botCommand in Program.BotCommands)
                stringBuilder.Append($"{botCommand.ChatRepresentation} - {botCommand.Description}\n");

            HelpMessage = stringBuilder.ToString();
        }

        public const string OnEditScheduleRequest =
            "Отправьте ваш access-token для получени доступа к редактированию расписания.";

        public const string OnAdministrationRequest =
            "Отправьте API-access-token бота, чтобы подтвердить, что вы являетесь администратором.";

        public const string OnIncorrectAccessToken = "Access-token не опознан. Попробуйте снова.";

        public static readonly string HelpMessage;
    }
}