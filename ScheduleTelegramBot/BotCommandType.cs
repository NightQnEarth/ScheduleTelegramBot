namespace ScheduleTelegramBot
{
    public enum BotCommandType
    {
        [ChatRepresentation("/today")]
        [CommandDescription("Расписание на сегодня.")]
        Today,

        [ChatRepresentation("/all")]
        [CommandDescription("Полное расписание.")]
        All,

        [ChatRepresentation("/custom_day")]
        [CommandDescription("Расписание в конкретный день недели.")]
        CustomDay,

        [ChatRepresentation("/edit_schedule")]
        [CommandDescription("Изменить расписание.")]
        EditSchedule,

        [ChatRepresentation("/get_password")]
        [CommandDescription("Получить доступ к редактированию расписания.")]
        GetPassword,

        [ChatRepresentation("/recall_password")]
        [CommandDescription("Отозвать доступ к редактированию расписания.")]
        RecallPassword
    }
}