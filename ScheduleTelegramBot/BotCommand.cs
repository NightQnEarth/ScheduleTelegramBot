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

        [ChatRepresentation("/specific_day")]
        [CommandDescription("Specific day schedule.")]
        SpecificDay,

        [ChatRepresentation("/edit_schedule")]
        [CommandDescription("Edit schedule.")]
        EditSchedule,

        [ChatRepresentation("/clear_day_schedule")]
        [CommandDescription("Delete schedule of custom day.")]
        ClearDaySchedule,

        [ChatRepresentation("/get_access")]
        [CommandDescription("Get edit-access-token to edit schedule.")]
        GetAccess,

        [ChatRepresentation("/revoke_access")]
        [CommandDescription("Revoke edit-access-token to edit schedule.")]
        RevokeAccess
    }
}