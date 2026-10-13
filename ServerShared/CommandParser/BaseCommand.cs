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
        public string PermissionDefaultRank { get; set; } = DefaultRanks.Member;

        /// <summary>The full command as the caller typed it.</summary>
        public string Input { get; set; }

        /// <summary>Called when a permission check in this command fails, so the command manager can report it.</summary>
        public Action PermissionDenied { get; set; }

        public abstract void Handle(string[] args);

        /// <summary>
        /// Checks whether the current caller has access to the command's declared permission.
        /// Commands without a permission can be run by everyone.
        /// </summary>
        /// <param name="denyMessage">Optional message to send when access is denied.</param>
        /// <returns>True if the caller has access; otherwise false.</returns>
        protected bool HasAccess(string denyMessage = null)
        {
            if (string.IsNullOrWhiteSpace(Permission))
                return true;

            return HasAccess(Permission, denyMessage);
        }

        /// <summary>
        /// Checks whether the current caller has access to the specified permission.
        /// </summary>
        /// <param name="permission">The permission string to check.</param>
        /// <param name="denyMessage">Optional message to send when access is denied.</param>
        /// <returns>True if the caller has access; otherwise false.</returns>
        protected bool HasAccess(string permission, string denyMessage = null)
        {
            // The server console can always run every command.
            if (Caller == null)
                return true;

            bool allowed = Server?.Config?.Permissions != null
                && Server.Config.Permissions.HasPermission(Caller.RankName, permission);

            if (!allowed)
            {
                PermissionDenied?.Invoke();
                SendMessage(string.IsNullOrEmpty(denyMessage) ? $"You do not have permission to run the command '{Input}'." : denyMessage, LogMessageType.Error);
            }

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
