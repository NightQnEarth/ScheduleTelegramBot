using System;

namespace ScheduleTelegramBot
{
    public class CommandDescription : Attribute
    {
        public readonly string Description;

        public CommandDescription(string description) => Description = description;
    }
}