namespace ServerShared
{
    public enum CommandStatus
    {
        /// <summary>The message wasn't a command (e.g. a chat message without the command prefix).</summary>
        NotACommand,
        /// <summary>The command ran.</summary>
        Ran,
        /// <summary>The command (or one of its subcommands) needs a permission the caller doesn't have.</summary>
        NoPermission,
        /// <summary>No command with that name exists.</summary>
        NotFound,
        /// <summary>The command exists but couldn't run, e.g. because of invalid arguments.</summary>
        Failed
    }

    /// <summary>The outcome of handling a command message.</summary>
    public class CommandResult
    {
        public CommandStatus Status { get; }

        /// <summary>The message as the caller typed it (trimmed).</summary>
        public string Input { get; }

        /// <summary>The first error shown to the caller, if any.</summary>
        public string Error { get; }

        public CommandResult(CommandStatus status, string input, string error = null)
        {
            Status = status;
            Input = input;
            Error = error;
        }
    }
}
