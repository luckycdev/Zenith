using ServerShared;
using ServerShared.Logging;
using ServerShared.Player;
using System;
using System.Linq;

namespace Pyratron.Frameworks.Commands.Parser
{
    public abstract class BaseCommand
    {
        public CommandContext Context { get; set; }
        public GameServer Server { get; set; }
        public CommandParser Parser { get; set; }
        public NetPlayer Caller { get; set; }
        public string Permission { get; set; }
        public AccessLevel PermissionDefaultAccessLevel { get; set; } = AccessLevel.Player;

        public abstract void Handle(string[] args);

        /// <summary>
        /// Checks whether the current caller has access to the command's declared permission.
        /// </summary>
        /// <param name="denyMessage">Optional message to send when access is denied.</param>
        /// <returns>True if the caller has access; otherwise false.</returns>
        protected bool HasAccess(string denyMessage = null)
        {
            if (!string.IsNullOrWhiteSpace(Permission))
                return HasAccess(Permission, denyMessage);

            return HasAccess(PermissionDefaultAccessLevel, denyMessage);
        }

        /// <summary>
        /// Checks whether the current caller has access to the specified permission.
        /// </summary>
        /// <param name="permission">The permission string to check.</param>
        /// <param name="denyMessage">Optional message to send when access is denied.</param>
        /// <returns>True if the caller has access; otherwise false.</returns>
        protected bool HasAccess(string permission, string denyMessage = null)
        {
            if (Server?.Config?.Permissions == null)
                return false;

            var rankName = Caller?.RankName ?? "Console";
            bool allowed = Server.Config.Permissions.HasPermission(rankName, permission);

            if (!allowed)
                SendMessage(string.IsNullOrEmpty(denyMessage) ? "No access to command." : denyMessage, LogMessageType.Error);

            return allowed;
        }

        /// <summary>
        /// Checks whether the current caller has access to a required access level.
        /// </summary>
        /// <param name="requiredAccessLevel">The minimum access level required.</param>
        /// <param name="denyMessage">Optional message to send when access is denied.</param>
        /// <returns>True if the caller has access; otherwise false.</returns>
        protected bool HasAccess(AccessLevel requiredAccessLevel, string denyMessage = null)
        {
            var currentAccessLevel = Caller?.AccessLevel ?? AccessLevel.Console;
            bool allowed = currentAccessLevel >= requiredAccessLevel;

            if (!allowed)
                SendMessage(string.IsNullOrEmpty(denyMessage) ? "No access to command." : denyMessage, LogMessageType.Error);

            return allowed;
        }

        /// <summary>Sends a message to the command caller.</summary>
        protected void SendMessage(object msg, LogMessageType type)
        {
            switch (Context)
            {
                case CommandContext.Chat:
                    SendMessageChat(msg, type);
                    break;
                case CommandContext.Console:
                    SendMessageConsole(msg, type);
                    break;
            }
        }

        private void SendMessageChat(object msg, LogMessageType type)
        {
            UnityEngine.Color color;

            switch (type)
            {
                case LogMessageType.Debug:
                case LogMessageType.Info:
                    color = SharedConstants.ColorGreen;
                    break;
                case LogMessageType.Warning:
                case LogMessageType.Error:
                case LogMessageType.Exception:
                    color = SharedConstants.ColorRed;
                    break;
                default:
                    color = UnityEngine.Color.white;
                    break;
            }

            Caller.SendChatMessage(msg.ToString(), color);
        }

        private void SendMessageConsole(object msg, LogMessageType type)
        {
            if (Caller != null)
                Caller.SendConsoleMessage(msg.ToString(), type);
            else
                Logger.Log(msg, type);
        }
    }

    public enum CommandContext
    {
        Chat,
        Console
    }
}
