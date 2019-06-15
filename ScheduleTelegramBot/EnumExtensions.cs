using System;
using System.Linq;
using System.Reflection;

namespace ScheduleTelegramBot
{
    public static class EnumExtensions
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value) =>
            value.GetType()
                 .GetField(value.ToString())
                 .GetCustomAttributes()
                 .OfType<TAttribute>()
                 .First();
    }
}