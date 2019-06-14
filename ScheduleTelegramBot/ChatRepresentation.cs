using System;

namespace ScheduleTelegramBot
{
    public class ChatRepresentation : Attribute
    {
        public readonly string Representation;

        public ChatRepresentation(string representation) => Representation = representation;
    }
}