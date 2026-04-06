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
            public readonly AccessLevel AccessLevel;

            public PermissionDefault(string permission, AccessLevel accessLevel)
            {
                Permission = permission;
                AccessLevel = accessLevel;
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
        private bool parseErrorOccurred;
        
        public BaseCommandManager(GameServer server, string prefix, params Assembly[] searchAssemblies)
        {
            Server = server;
            Parser = CommandParser.CreateNew(prefix);
            Parser.OnError(HandleParseError);
            LoadCommands(searchAssemblies.Concat(new[] {Assembly.GetExecutingAssembly()}));
        }

        private void HandleParseError(object sender, string error)
        {
            parseErrorOccurred = true;
            OnParseError(sender, error);
        }
        
        public string HandleMessage(NetPlayer caller, string message)
        {
            CurrentCaller = caller;
            parseErrorOccurred = false;
            int parserAccessLevel = caller?.RankWeight ?? Server?.Config?.Permissions?.GetRankWeight("Console") ?? (int)AccessLevel.Console;
            bool result = Parser.Parse(message, caller, parserAccessLevel);
            CurrentCaller = null;
            if (!result) return null;
            if (parseErrorOccurred) return string.Empty;
            return message;
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

                if (attribute == null)
                    continue;

                if (!commandBaseType.IsAssignableFrom(type))
                    continue;

                AccessLevel defaultAccessLevel = permissionAttribute?.DefaultAccessLevel
                    ?? AccessLevel.Player;

                if (permissionAttribute != null
                    && permissionDefaults.All(p => !p.Permission.Equals(permissionAttribute.Permission, StringComparison.OrdinalIgnoreCase)))
                {
                    permissionDefaults.Add(new PermissionDefault(permissionAttribute.Permission, permissionAttribute.DefaultAccessLevel));
                }

                if (permissionAttribute != null && Server?.Config?.Permissions != null)
                    permissionsChanged |= Server.Config.Permissions.RegisterDefaultPermission(permissionAttribute.Permission, permissionAttribute.DefaultAccessLevel);

                Parser.AddCommand(CommandFromAttribute(type, attribute, permissionAttribute, requireCallerAttribute != null));
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
                changed |= Server.Config.Permissions.RegisterDefaultPermission(permissionDefault.Permission, permissionDefault.AccessLevel);

            return changed;
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
                command.AccessLevel = (int)permissionAttribute.DefaultAccessLevel;

            if (permissionAttribute != null)
            {
                command.SetExecutePredicate(_ =>
                {
                    if (Server?.Config?.Permissions == null)
                        return "No access to command.";

                    string rankName = CurrentCaller?.RankName ?? "Console";
                    return Server.Config.Permissions.HasPermission(rankName, permissionAttribute.Permission)
                        ? string.Empty
                        : "No access to command.";
                });
            }

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
                instance.PermissionDefaultAccessLevel = permissionAttribute?.DefaultAccessLevel
                    ?? AccessLevel.Player;
                
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

        public Command RegisterCommand(Command command, string permission, AccessLevel defaultAccessLevel = AccessLevel.Player)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (string.IsNullOrWhiteSpace(permission)) throw new ArgumentNullException(nameof(permission));

            command.AccessLevel = (int)defaultAccessLevel;

            if (permissionDefaults.All(p => !p.Permission.Equals(permission, StringComparison.OrdinalIgnoreCase)))
                permissionDefaults.Add(new PermissionDefault(permission, defaultAccessLevel));

            if (Server?.Config?.Permissions != null)
            {
                if (Server.Config.Permissions.RegisterDefaultPermission(permission, defaultAccessLevel) && !string.IsNullOrEmpty(Server.Config.Directory))
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
