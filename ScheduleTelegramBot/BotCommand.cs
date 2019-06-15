using System.Diagnostics.CodeAnalysis;

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
            (ChatRepresentation, Description) = (commandType.GetAttribute<ChatRepresentation>().Representation,
                                                 commandType.GetAttribute<CommandDescription>().Description);
        }

        public override bool Equals(object obj) => obj is BotCommand botCommand && botCommand.Equals(this);

        // TODO: access modifier.
        // ReSharper disable once MemberCanBePrivate.Global
        public bool Equals(BotCommand other) => CommandType == other.CommandType;

        public override int GetHashCode() => (int)CommandType;

        public override string ToString() => ChatRepresentation;
    }
}