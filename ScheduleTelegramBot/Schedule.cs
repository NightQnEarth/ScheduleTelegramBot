using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScheduleTelegramBot
{
    public class Schedule
    {
        private readonly Dictionary<WorkDay, string> daysSchedule =
            typeof(WorkDay).GetEnumValues()
                           .Cast<WorkDay>()
                           .ToDictionary(day => day, day => default(string));

        private readonly Regex dayScheduleTemplate = new Regex(@"(?:\d{1,2}:\d{2}\s+\([IV]{1,3}\),\s+.*(\n|$)){1,6}");

        public bool TrySetDaySchedule(WorkDay day, string daySchedule) =>
            dayScheduleTemplate.Match(daySchedule, 0, daySchedule.Length).Success &&
            (daysSchedule[day] = daySchedule) == daySchedule;

        public string GetDaySchedule(WorkDay day) =>
            daysSchedule[day] is null
                ? $"-----{Bot.RepresentByWorkDay[day]}: {BotPhrases.OnEmptyDaySchedule}"
                : $"-----{Bot.RepresentByWorkDay[day]}:\n{daysSchedule[day]}";

        public string GetFullSchedule() =>
            daysSchedule.Values.Any(schedule => !string.IsNullOrEmpty(schedule))
                ? string.Join('\n', daysSchedule.Keys.Select(day => $"{GetDaySchedule(day)}\n"))
                : BotPhrases.OnEmptyFullSchedule;

        public void ClearDaySchedule(WorkDay day) => daysSchedule[day] = null;
    }
}