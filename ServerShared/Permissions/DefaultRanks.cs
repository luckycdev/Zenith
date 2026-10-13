using System;
using System.Collections.Generic;

namespace ServerShared
{
    /// <summary>
    /// The ranks Zenith ships with. Server owners can rename, remove or add ranks in permissions.json (or with /rank);
    /// these names are only used for the default permissions.json and as default ranks for permission nodes.
    /// The server console is not a rank: it can always run every command.
    /// </summary>
    public static class DefaultRanks
    {
        public const string Member = "Member";
        public const string Moderator = "Moderator";
        public const string Admin = "Admin";

        public const int MemberWeight = 10;
        public const int ModeratorWeight = 20;
        public const int AdminWeight = 50;

        /// <summary>Valid range for rank weights. Weight is only used for hierarchy (e.g. who can kick/ban/rank who), not for permissions.</summary>
        public const int MinWeight = 1;
        public const int MaxWeight = 9999;

        /// <summary>Returns the default weight of a built-in rank, or null if the name isn't a built-in rank.</summary>
        public static int? GetDefaultWeight(string rankName)
        {
            if (Member.Equals(rankName, StringComparison.OrdinalIgnoreCase)) return MemberWeight;
            if (Moderator.Equals(rankName, StringComparison.OrdinalIgnoreCase)) return ModeratorWeight;
            if (Admin.Equals(rankName, StringComparison.OrdinalIgnoreCase)) return AdminWeight;
            return null;
        }

        /// <summary>Returns the default inherited ranks of a built-in rank, or null if the name isn't a built-in rank.</summary>
        public static List<string> GetDefaultInherits(string rankName)
        {
            if (Member.Equals(rankName, StringComparison.OrdinalIgnoreCase)) return new List<string>();
            if (Moderator.Equals(rankName, StringComparison.OrdinalIgnoreCase)) return new List<string> { Member };
            if (Admin.Equals(rankName, StringComparison.OrdinalIgnoreCase)) return new List<string> { Moderator };
            return null;
        }
    }
}
