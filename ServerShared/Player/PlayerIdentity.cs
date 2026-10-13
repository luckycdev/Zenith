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

    /// <summary>A player identity (steam id, or IP + name) with the permission rank it has been given.</summary>
    public class PlayerRankIdentity : PlayerIdentity
    {
        public string RankName;

        public string GetRankName()
        {
            return string.IsNullOrWhiteSpace(RankName) ? DefaultRanks.Member : RankName;
        }

        public PlayerRankIdentity(ulong steamId, string rankName) : base(steamId)
        {
            RankName = rankName;
        }

        public PlayerRankIdentity(uint ip, string name, string rankName) : base(ip, name)
        {
            RankName = rankName;
        }

        [JsonConstructor]
        private PlayerRankIdentity() { }
    }
}
