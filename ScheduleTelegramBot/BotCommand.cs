using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ScheduleTelegramBot
{
    public class BotCommand
    {
        public readonly BotCommandType CommandType;
        public readonly string ChatRepresentation;
        public readonly string Description;

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public BotCommand(BotCommandType commandType)
        {
            CommandType = commandType;
            var attributes = commandType.GetType().GetField(commandType.ToString()).GetCustomAttributes();
            (ChatRepresentation, Description) = (attributes.OfType<ChatRepresentation>().First().Representation,
                                                 attributes.OfType<CommandDescription>().First().Description);
        }

        public override bool Equals(object obj) => obj is BotCommand botCommand && botCommand.Equals(this);

        // TODO
        // ReSharper disable once MemberCanBePrivate.Global
        public bool Equals(BotCommand other) => CommandType == other.CommandType;

        public override int GetHashCode() => (int)CommandType;
    }
}