namespace ScheduleTelegramBot
{
    public enum BotCommandType
    {
        [ChatRepresentation("/today")]
        [CommandDescription("Today schedule.")]
        Today,

        [ChatRepresentation("/full")]
        [CommandDescription("Full schedule.")]
        Full,

        [ChatRepresentation("/custom_day")]
        [CommandDescription("Custom day schedule.")]
        CustomDay,

        [ChatRepresentation("/edit_schedule")]
        [CommandDescription("Edit schedule.")]
        EditSchedule,

        [ChatRepresentation("/get_access")]
        [CommandDescription("Get edit-access-token to edit schedule.")]
        GetAccess,

        [ChatRepresentation("/recall_access")]
        [CommandDescription("Recall edit-access-token to edit schedule.")]
        RecallAccess
    }
}