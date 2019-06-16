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

        public bool TrySetDaySchedule(WorkDay day, string daySchedule)
        {
            if (dayScheduleTemplate.Matches(daySchedule)
                                   .FirstOrDefault(match => match.Length == daySchedule.Length) is null)
                return false;

            daysSchedule[day] = daySchedule;

            return true;
        }

        public string GetDaySchedule(WorkDay day) =>
            daysSchedule[day] is null
                ? $"-----{day.GetAttribute<ChatRepresentation>().Representation}:{BotReplica.OnEmptyDaySchedule}"
                : $"-----{day.GetAttribute<ChatRepresentation>().Representation}:\n{daysSchedule[day]}";

        public string GetFullSchedule() =>
            daysSchedule.Values.Any(schedule => !string.IsNullOrEmpty(schedule))
                ? string.Join('\n', daysSchedule.Keys.Select(day => $"{GetDaySchedule(day)}\n"))
                : BotReplica.OnEmptyFullSchedule;
    }
}