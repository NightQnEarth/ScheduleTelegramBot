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

        private readonly Regex dayScheduleTemplate =
            new Regex(@"(?:[IV]+\s*-\s+\d{1,2}:\d{1,2},\s+(?:[ \w]+,)+.*(\n|$))+"); // TODO: (\n|$) or enough $?

        public bool TrySetDaySchedule(WorkDay day, string daySchedule)
        {
            if (dayScheduleTemplate.Matches(daySchedule)
                                   .FirstOrDefault(match => match.Length == daySchedule.Length) is null)
                return false;

            daysSchedule[day] = daySchedule;

            return true;
        }

        public string GetDaySchedule(WorkDay day) => daysSchedule[day] ?? BotReplica.OnEmptyDaySchedule;

        public string GetFullSchedule()
        {
            var fullSchedule = string.Concat(daysSchedule.Values);
            return string.IsNullOrEmpty(fullSchedule) ? BotReplica.OnEmptyDaySchedule : fullSchedule;
        }
    }
}