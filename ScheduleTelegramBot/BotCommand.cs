namespace ScheduleTelegramBot
{
    public enum BotCommand
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

        [ChatRepresentation("/clear_day_schedule")]
        [CommandDescription("Delete schedule of custom day.")]
        ClearDaySchedule,

        [ChatRepresentation("/get_access")]
        [CommandDescription("Get edit-access-token to edit schedule.")]
        GetAccess,

        [ChatRepresentation("/recall_access")]
        [CommandDescription("Recall edit-access-token to edit schedule.")]
        RecallAccess
    }
}