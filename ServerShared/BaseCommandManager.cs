using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared
{
    public abstract class BaseCommandManager<T> where T : BaseCommand
    {
        class PermissionDefault
        {
            public readonly string Permission;
            public readonly string DefaultRank;

            public PermissionDefault(string permission, string defaultRank)
            {
                Permission = permission;
                DefaultRank = defaultRank;
            }
        }

        class PropertyAttribute
        {
            public readonly PropertyInfo PropertyInfo;
            public readonly CommandArgumentAttribute Attribute;

            public PropertyAttribute(PropertyInfo propertyInfo, CommandArgumentAttribute attribute)
            {
                PropertyInfo = propertyInfo;
                Attribute = attribute;
            }
        }

        protected readonly GameServer Server;
        protected readonly CommandParser Parser;
        protected NetPlayer CurrentCaller { get; private set; }
        private readonly List<PermissionDefault> permissionDefaults = new List<PermissionDefault>();
        private readonly Dictionary<Command, string> commandPermissions = new Dictionary<Command, string>();

        // State of the message currently being handled.
        private string currentInput;
        private CommandStatus? currentFailure;
        private string currentError;

        public BaseCommandManager(GameServer server, string prefix, params Assembly[] searchAssemblies)
        {
            Server = server;
            Parser = CommandParser.CreateNew(prefix);
            Parser.OnError(HandleParseError);
            Parser.CommandNotFound += _ => SetFailure(CommandStatus.NotFound);
            Parser.CommandRejected += (command, _) =>
            {
                if (commandPermissions.TryGetValue(command, out string permission) && !CurrentCallerHasPermission(permission))
                    SetFailure(CommandStatus.NoPermission);
            };
            LoadCommands(searchAssemblies.Concat(new[] {Assembly.GetExecutingAssembly()}));
        }

        private void HandleParseError(object sender, string error)
        {
            SetFailure(CommandStatus.Failed);

            if (currentError == null)
                currentError = error;

            OnParseError(sender, error);
        }

        // The first failure reason wins (e.g. "no permission" stays even though an error message follows it).
        private void SetFailure(CommandStatus status)
        {
            if (currentFailure == null)
                currentFailure = status;
        }

        /// <summary>The message shown when the caller doesn't have permission to run what they typed.</summary>
        private string NoPermissionMessage => $"You do not have permission to run the command '{currentInput}'.";

        public CommandResult HandleMessage(NetPlayer caller, string message)
        {
            CurrentCaller = caller;
            currentInput = message?.Trim();
            currentFailure = null;
            currentError = null;

            bool isCommand = Parser.Parse(message, caller);
            var result = new CommandResult(isCommand ? currentFailure ?? CommandStatus.Ran : CommandStatus.NotACommand, currentInput, currentError);

            CurrentCaller = null;
            currentInput = null;
            return result;
        }

        private void LoadCommands(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
            Type commandBaseType = typeof(T);
            bool permissionsChanged = false;

            foreach (Type type in types)
            {
                var attribute = type.GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault() as CommandAttribute;
                var permissionAttribute = type.GetCustomAttributes(typeof(CommandPermissionAttribute), true).FirstOrDefault() as CommandPermissionAttribute;
                var requireCallerAttribute = type.GetCustomAttributes(typeof(RequireCallerAttribute), true).FirstOrDefault() as RequireCallerAttribute;
                bool consoleOnly = type.GetCustomAttributes(typeof(ConsoleOnlyAttribute), true).Any();

                if (attribute == null)
                    continue;

                if (!commandBaseType.IsAssignableFrom(type))
                    continue;

                // Players can never run console-only commands, so don't even register them for chat.
                if (consoleOnly && this is PlayerCommandManager)
                    continue;

                var permissions = type.GetCustomAttributes(typeof(RegisterPermissionAttribute), true)
                    .Cast<RegisterPermissionAttribute>()
                    .Select(extra => new PermissionDefault(extra.Permission, extra.DefaultRank))
                    .ToList();

                if (permissionAttribute != null)
                    permissions.Insert(0, new PermissionDefault(permissionAttribute.Permission, permissionAttribute.DefaultRank));

                foreach (var permission in permissions)
                {
                    if (permissionDefaults.All(p => !p.Permission.Equals(permission.Permission, StringComparison.OrdinalIgnoreCase)))
                        permissionDefaults.Add(permission);

                    if (Server?.Config?.Permissions != null)
                        permissionsChanged |= Server.Config.Permissions.RegisterDefaultPermission(permission.Permission, permission.DefaultRank);
                }

                var command = CommandFromAttribute(type, attribute, permissionAttribute, requireCallerAttribute != null);
                command.ConsoleOnly = consoleOnly;
                Parser.AddCommand(command);
            }

            if (permissionsChanged && Server?.Config?.Permissions != null && !string.IsNullOrEmpty(Server.Config.Directory))
                Server.Config.Permissions.Save(Server.Config.Directory);
        }

        private bool RegisterCollectedPermissionDefaults()
        {
            if (Server?.Config?.Permissions == null)
                return false;

            bool changed = false;
            foreach (var permissionDefault in permissionDefaults)
                changed |= Server.Config.Permissions.RegisterDefaultPermission(permissionDefault.Permission, permissionDefault.DefaultRank);

            return changed;
        }

        /// <summary>
        /// Returns true if the current caller has the specified permission node.
        /// The server console (no caller) can always run every command.
        /// </summary>
        public bool CurrentCallerHasPermission(string permission)
        {
            if (CurrentCaller == null)
                return true;

            if (Server?.Config?.Permissions == null)
                return false;

            return Server.Config.Permissions.HasPermission(CurrentCaller.RankName, permission);
        }

        /// <summary>
        /// Gates a command behind a permission node. Access is decided by the caller's rank in permissions.json
        /// (including inherited ranks). Rank weight is not used for permissions.
        /// </summary>
        private void ApplyPermission(Command command, string permission)
        {
            commandPermissions[command] = permission;

            var existingPredicate = command.CanExecute;
            command.SetExecutePredicate(cmd =>
            {
                if (!CurrentCallerHasPermission(permission))
                    return NoPermissionMessage;

                return existingPredicate?.Invoke(cmd) ?? string.Empty;
            });
        }

        public bool CanCurrentCallerRunCommand(Command command)
        {
            if (command == null)
                return false;

            var canExecute = command.CanExecute?.Invoke(command);
            return string.IsNullOrEmpty(canExecute);
        }

        private Command CommandFromAttribute(Type type, CommandAttribute attribute, CommandPermissionAttribute permissionAttribute, bool requireCaller)
        {
            var command = new Command(Parser, attribute.FriendlyName, attribute.CommandNames.First(), attribute.Description);
            command.RequireCaller = requireCaller;

            if (attribute.CommandNames.Length > 1)
                command.AddAlias(attribute.CommandNames.Skip(1).ToArray());

            if (permissionAttribute != null)
                ApplyPermission(command, permissionAttribute.Permission);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var argumentAttribute = property.GetCustomAttributes(typeof(CommandArgumentAttribute), true).FirstOrDefault() as CommandArgumentAttribute;

                if (argumentAttribute == null)
                    continue;

                if (!property.CanWrite)
                {
                    Logger.LogError($"Argument property not writable: {type.Name}.{property.Name}");
                    continue;
                }

                command.AddArgument(ArgumentFromAttribute(type, property, argumentAttribute));
            }

            command.SetAction((arguments, data, rawArgs) =>
            {
                var netPlayer = (NetPlayer) data;
                var instance = (BaseCommand) Activator.CreateInstance(type, true);
                instance.Server = Server;
                instance.Parser = Parser;
                instance.Context = (this is PlayerCommandManager) ? CommandContext.Chat : CommandContext.Console;
                instance.Caller = netPlayer;
                instance.Permission = permissionAttribute?.Permission;
                instance.PermissionDefaultRank = permissionAttribute?.DefaultRank ?? DefaultRanks.Member;
                instance.Input = currentInput;
                instance.PermissionDenied = () => SetFailure(CommandStatus.NoPermission);
                
                if (!PrepareInstance(instance, arguments, out string error))
                {
                    OnParseError(this, error);
                    return;
                }

                instance.Handle(rawArgs);
            });

            return command;
        }

        private Argument ArgumentFromAttribute(Type commandType, PropertyInfo argumentProperty, CommandArgumentAttribute attribute)
        {
            var argument = new Argument(attribute.Name, attribute.Optional);

            if (attribute.Optional)
                argument.SetDefault(attribute.DefaultValue);

            return argument;
        }

        private bool PrepareInstance(BaseCommand instance, Argument[] arguments, out string error)
        {
            List<PropertyAttribute> propertyAttributes = new List<PropertyAttribute>();

            foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttributes(typeof(CommandArgumentAttribute), true).FirstOrDefault() as CommandArgumentAttribute;

                if (attribute == null)
                    continue;

                propertyAttributes.Add(new PropertyAttribute(property, attribute));
            }

            foreach (Argument argument in arguments)
            {
                foreach (var propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute.Attribute.Name.ToLower() != argument.Name)
                    {
                        continue;
                    }

                    Type targetType = propertyAttribute.PropertyInfo.PropertyType;

                    try
                    {
                        object convertedValue;

                        if (targetType.IsEnum)
                        {
                            convertedValue = Enum.Parse(targetType, argument.Value, true);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(argument.Value, targetType, CultureInfo.InvariantCulture);
                        }

                        propertyAttribute.PropertyInfo.SetValue(instance, convertedValue, null);
                    }
                    catch
                    {
                        error = $"Argument is not valid: {argument.Name}.";
                        return false;
                    }

                    break;
                }
            }

            error = null;
            return true;
        }

        protected abstract void OnParseError(object sender, string error);

        public void RegisterCommandsFromAssembly(Assembly assembly)
        {
            LoadCommands(new[] { assembly });
        }

        /// <summary>
        /// Registers a manually built command behind a permission node. The default rank gets the permission
        /// in permissions.json the first time it's registered.
        /// </summary>
        public Command RegisterCommand(Command command, string permission, string defaultRank = DefaultRanks.Member)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(permission)) throw new ArgumentNullException(nameof(permission));

            ApplyPermission(command, permission);

            if (permissionDefaults.All(p => !p.Permission.Equals(permission, StringComparison.OrdinalIgnoreCase)))
                permissionDefaults.Add(new PermissionDefault(permission, defaultRank));

            if (Server?.Config?.Permissions != null)
            {
                if (Server.Config.Permissions.RegisterDefaultPermission(permission, defaultRank) && !string.IsNullOrEmpty(Server.Config.Directory))
                    Server.Config.Permissions.Save(Server.Config.Directory);
            }

            Parser.AddCommand(command);
            return command;
        }

        public void SyncPermissionDefaults()
        {
            if (!RegisterCollectedPermissionDefaults())
                return;

            if (!string.IsNullOrEmpty(Server?.Config?.Directory))
                Server.Config.Permissions.Save(Server.Config.Directory);
        }
    }
}
