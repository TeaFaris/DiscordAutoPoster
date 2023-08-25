namespace DiscordAutoPoster.Configuration
{
	public class BotConfiguration
	{
		public string Token { get; init; } = null!;
		public ulong[] Admins { get; init; } = null!;
		public ulong GuildId { get; init; }
		public ulong AllowedRoleId { get; set; }
		public List<string> Servers { get; init; } = null!;
		public Dictionary<ulong, List<ulong>> RoleToChannel { get; init; } = null!;
		public uint PostDelayInMinutes { get; set; }
	}
}