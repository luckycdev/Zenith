using System;
using ServerShared;

namespace Pyratron.Frameworks.Commands.Parser
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public readonly string FriendlyName;
        public readonly string[] CommandNames;
        public readonly string Description;

        public CommandAttribute(string friendlyName, string commandName, string description) : this(friendlyName, new[] {commandName}, description)
        {
        }

        public CommandAttribute(string friendlyName, string[] commandNames, string description)
        {
            FriendlyName = friendlyName;
            CommandNames = commandNames;
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CommandArgumentAttribute : Attribute
    {
        public readonly string Name;
        public readonly bool Optional;
        public readonly string DefaultValue;

        public CommandArgumentAttribute(string name, bool optional = false, string defaultValue = null)
        {
            Name = name;
            Optional = optional;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Declares the permission node for a command, and the rank that gets it by default
    /// (added to that rank in permissions.json the first time the command is loaded). Use the <c>DefaultRanks</c> names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandPermissionAttribute : Attribute
    {
        public readonly string Permission;
        public readonly string DefaultRank;

        public CommandPermissionAttribute(string permission, string defaultRank = DefaultRanks.Member)
        {
            Permission = permission;
            DefaultRank = defaultRank;
        }
    }

    /// <summary>
    /// Specifies that the command requires that a player called the command. This would not be the case if the command was called from the server console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireCallerAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that the command can only be run from the server console. Players can never run it, regardless of their permissions,
    /// so it doesn't need a permission node.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConsoleOnlyAttribute : Attribute
    {
    }

    /// <summary>
    /// Registers an extra permission node that the command checks itself (for example one per subcommand, checked with
    /// <c>HasAccess("node")</c>), and the rank that gets it by default. Can be used multiple times.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RegisterPermissionAttribute : Attribute
    {
        public readonly string Permission;
        public readonly string DefaultRank;

        public RegisterPermissionAttribute(string permission, string defaultRank = DefaultRanks.Member)
        {
            Permission = permission;
            DefaultRank = defaultRank;
        }
    }
}
