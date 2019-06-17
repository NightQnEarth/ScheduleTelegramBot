using System;

namespace ScheduleTelegramBot
{
    public class CommandDescription : Attribute
    {
        private readonly string description;

        public CommandDescription(string description) => this.description = description;

        public override string ToString() => description;
    }
}