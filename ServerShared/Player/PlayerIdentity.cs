using System;
using System.Net;
using Newtonsoft.Json;

namespace ServerShared.Player
{
    public enum IdentityType
    {
        SteamId,
        Ip
    }

    public class PlayerIdentity
    {
        public IdentityType Type;
        public ulong SteamId;
        public uint Ip;
        public string Name;

        public PlayerIdentity(ulong steamId)
        {
            Type = IdentityType.SteamId;
            SteamId = steamId;
        }

        public PlayerIdentity(uint ip)
        {
            Type = IdentityType.Ip;
            Ip = ip;
        }

        public PlayerIdentity(uint ip, string name)
        {
            Type = IdentityType.Ip;
            Ip = ip;
            Name = name;
        }

        [JsonConstructor]
        protected PlayerIdentity() { }

        public bool Matches(NetPlayer player)
        {
            switch (Type)
            {
                case IdentityType.Ip:
                    if (player.Peer.RemoteEndPoint.Address.ToUint32() != Ip)
                        return false;

                    return !string.IsNullOrWhiteSpace(Name)
                        && player.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);
                case IdentityType.SteamId:
                    return player.SteamId == SteamId;
            }

            throw new NotImplementedException("Unimplemented IdentityType");
        }

        public override string ToString()
        {
            switch (Type)
            {
                case IdentityType.Ip:
                    return string.IsNullOrWhiteSpace(Name)
                        ? $"IP: {new IPAddress(Ip)}"
                        : $"IP: {new IPAddress(Ip)} (Name: {Name})";
                case IdentityType.SteamId:
                    return $"SteamID64: {SteamId}";
            }

            throw new NotImplementedException($"IdentityType not implemented: {Type}");
        }
    }

    public class PlayerAccessLevelIdentity : PlayerIdentity
    {
        public AccessLevel AccessLevel;
        public string RankName;

        public string GetRankName()
        {
            if (!string.IsNullOrWhiteSpace(RankName))
                return RankName;

            return AccessLevel == AccessLevel.Player ? "Default" : AccessLevel.ToString();
        }

        public PlayerAccessLevelIdentity(ulong steamId, AccessLevel accessLevel) : base(steamId)
        {
            AccessLevel = accessLevel;
            RankName = accessLevel == AccessLevel.Player ? "Default" : accessLevel.ToString();
        }

        public PlayerAccessLevelIdentity(uint ip, AccessLevel accessLevel) : base(ip)
        {
            AccessLevel = accessLevel;
            RankName = accessLevel == AccessLevel.Player ? "Default" : accessLevel.ToString();
        }

        public PlayerAccessLevelIdentity(uint ip, string name, AccessLevel accessLevel) : base(ip, name)
        {
            AccessLevel = accessLevel;
            RankName = accessLevel == AccessLevel.Player ? "Default" : accessLevel.ToString();
        }

        public PlayerAccessLevelIdentity(ulong steamId, string rankName, AccessLevel accessLevel) : base(steamId)
        {
            RankName = rankName;
            AccessLevel = accessLevel;
        }

        public PlayerAccessLevelIdentity(uint ip, string name, string rankName, AccessLevel accessLevel) : base(ip, name)
        {
            RankName = rankName;
            AccessLevel = accessLevel;
        }

        [JsonConstructor]
        private PlayerAccessLevelIdentity() { }
    }
}
