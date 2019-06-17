using System;

namespace ScheduleTelegramBot
{
    public class ChatRepresentation : Attribute
    {
        private readonly string representation;

        public ChatRepresentation(string representation) => this.representation = representation;

        public override string ToString() => representation;
    }
}